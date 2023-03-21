Shader "Atmosphere/Atmospheric Scattering" 
{
    Properties
    {
        _LightIntensity("Light Intensity", Float) = 30
        _LightColor("Light Color", Color) = (1,1,1)
        _LightDirection("Light Direction", Vector) = (0,0,1)
        _PlanetRadius("Planet Radius", Float) = 47
        _AtmosphereRadius("Atmosphere Radius", Float) = 50
        _Steps ("Steps", Int) = 20                                                  // Standard: 20
        _LightSteps ("Light Steps", Int) = 12                                       // Standard: 12
        _RayleighScattering("Rayleigh Scattering", Vector) = (0.08,0.2,0.51,0.64)   // Standard: (0.08,0.2,0.51,0.64)
        _MieScattering("Mie Scattering", Vector) = (0.01, 0.9, 0, 0.8)              // Standard: (0.01, 0.9, 0, 0.8)
        _ClipThreshold ("Clip Threshold", Range(0.0,1.0)) = 0.73                    // Standard: 0.73
    }
    SubShader
    {
        //Blend OneMinusDstColor One
        Blend SrcAlpha OneMinusSrcAlpha
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 100

        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #define PI 3.14159265

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 normal : TEXCOORD2;
                float3 viewDir : TEXCOORD1;
                float3 startPos : TEXCOORD3;
                float3 wPos : TEXCOORD4;
            };

            float _LightIntensity;
            float3 _LightColor;
            float3 _LightDirection;
            float _PlanetRadius;
            float3 _PlanetCenter;
            float _AtmosphereRadius;
            float _Steps;
            float _LightSteps;
            float4 _RayleighScattering;
            float4 _MieScattering;
            float _ClipThreshold;

            float sqrLength(float3 v) {
                return (v.x * v.x + v.y * v.y + v.z * v.z);
            }

            bool SphereIntersect(float3 ro, float3 rd, out float t0, out float t1, bool isPlanet) {
                float t = dot(_PlanetCenter - ro, rd);
                float3 pM = ro + rd * t;
                float height = sqrLength(pM - _PlanetCenter);
                if (height > _AtmosphereRadius * _AtmosphereRadius) return false;
                float x = sqrt(_AtmosphereRadius * _AtmosphereRadius - height);
                t0 = (t - x < 0) ? 0 : t - x;
                if (isPlanet && height < _PlanetRadius * _PlanetRadius && t > 0) {
                    float x = sqrt(_PlanetRadius * _PlanetRadius - height);
                    t1 = t - x;
                } else {
                    t1 = t + x;
                }
                return true;
            }

            bool LightMarch(float3 p1, float3 rd, float l, out float2 lightDepth) {
                float ds = l / _LightSteps;
                float time = 0;
                lightDepth = float2(0, 0);
                for (int i = 0; i < _LightSteps; i++)
                {
                    float3 p = p1 + rd * (time + ds * 0.5);
                    float height = length(p - _PlanetCenter) - _PlanetRadius;

                    if (height < 0) return false;

                    lightDepth.x += exp(-height / _RayleighScattering.w) * ds;
                    lightDepth.y += exp(-height / _MieScattering.w) * ds;

                    time += ds;
                }
                return true;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.startPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wPos = o.startPos;
                o.viewDir = normalize(o.startPos - _WorldSpaceCameraPos.xyz);
                v.normal *= -1;
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                _PlanetCenter = mul(unity_ObjectToWorld, float4(0, 0, 0, 1));
                if (_PlanetRadius > _AtmosphereRadius) _PlanetRadius = _AtmosphereRadius - 2;
                if (_AtmosphereRadius < 0) _AtmosphereRadius = 1;

                float3 rsRGB = float3(_RayleighScattering.xyz);
                float msRGB = _MieScattering.x;
                float rSH = _RayleighScattering.w;
                float mSH = _MieScattering.w;

                i.viewDir = normalize(i.startPos - _WorldSpaceCameraPos.xyz);
                i.startPos = _WorldSpaceCameraPos;

                float t0,t1;
                if (!SphereIntersect(i.startPos, i.viewDir, t0, t1, true)) discard;

                float mu = dot(i.viewDir, normalize(-_LightDirection));
                float g = _MieScattering.y;
                float phaseR = 3.0 / (16.0 * PI) * (1 + mu * mu);
                float phaseM = 3.0 / (8.0 * PI) * ((1.f - g * g) * (1.f + mu * mu)) / ((2.f + g * g) * pow(1.f + g * g - 2.f * g * mu, 1.5f));

                float3 sumR, sumM;
                float2 opticalDepth;

                float3 p1 = i.startPos + i.viewDir * t0;
                float l = t1 - t0;
                float ds = l / _Steps;
                float time = 0;
                for (int e = 0; e < _Steps; e++)
                {
                    float3 p = p1 + i.viewDir * (time + ds * 0.5);
                    float3 lrd = normalize(-_LightDirection);

                    float lt0, lt1;
                    SphereIntersect(p, lrd, lt0, lt1, false);
                    float2 opticalLightDepth;
                    float3 lp1 = p + lrd * lt0;
                    if (LightMarch(lp1, lrd, lt1 - lt0, opticalLightDepth)) {
                        float height = length(p - _PlanetCenter) - _PlanetRadius;

                        float hr = exp(-height / rSH) * ds;
                        float hm = exp(-height / mSH) * ds;

                        opticalDepth.x += hr;
                        opticalDepth.y += hm;

                        float3 tau = rsRGB * (opticalDepth.x + opticalLightDepth.x) + msRGB * 1.1 * (opticalDepth.y + opticalLightDepth.y);
                        float3 attenuation = float3 (exp(-tau.x), exp(-tau.y), exp(-tau.z));

                        sumR += attenuation * hr;
                        sumM += attenuation * hm;
                    }

                    time += ds;
                }

                float3 color = (sumR * rsRGB * phaseR + sumM * msRGB * phaseM) * _LightIntensity * _LightColor;

                float a = pow(saturate(sqrLength(_WorldSpaceCameraPos.xyz - i.wPos) - 0.4),2);
                float a1 = (color.x + color.y + color.z) / 3;
                if (a1 < _ClipThreshold)
                    a1 = lerp(0,a1, a1 / _ClipThreshold);

                return fixed4(color.xyz, min(a,a1));
            }
            ENDCG
        }
    }
}
