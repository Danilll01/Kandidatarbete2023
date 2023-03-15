using UnityEngine;

namespace ExtendedRandom
{
    /// <summary>
    /// Class for random with various random functions
    /// </summary>
    public class RandomX
    {
        readonly private System.Random random;

        public RandomX()
        {
            random = new System.Random();
        }

        public RandomX(int seed)
        {
            random = new System.Random(seed);
        }

        /// <summary>
        /// <para>Returns a random point inside or on a circle with radius 1.0</para>
        /// <i>Disclaimer: This function is not uniformly distributed</i> 
        /// </summary>
        public Vector2 InsideUnitCircle()
        {
            float angle = Value(0, 2) * Mathf.PI; // Generate angle between [0, 2*Pi)
            float radius = Value(); // Generate radius between [0, 1)

            // Convert to from polar coordinates to regular coordinates
            return new Vector2(
                x: radius * Mathf.Cos(angle),
                y: radius * Mathf.Sin(angle));
        }

        /// <summary>
        /// <para>Returns a uniformly distributed random point inside or on a circle with radius 1.0</para>
        /// <i>Note: This function is slower than <see cref="InsideUnitCircle()">InsideUnitCircle()</see></i>
        /// </summary>
        public Vector2 InsideUnitCircleUniform()
        {
            float angle = Value(0, 2) * Mathf.PI; // Generate angle between [0, 2*Pi)
            float radius = Mathf.Sqrt(Value()); // Generate radius between [0, 1)

            // Convert to from polar coordinates to regular coordinates
            return new Vector2(
                x: radius * Mathf.Cos(angle),
                y: radius * Mathf.Sin(angle));
        }

        /// <summary>
        /// Returns a uniformly random point on the edge of a circle with radius 1.0
        /// </summary>
        public Vector2 OnUnitCircle()
        {
            float angle = Value(0, 2) * Mathf.PI; // Generate angle between [0, 2*Pi)

            // Convert to from polar coordinates to regular coordinates
            return new Vector2(
                x: Mathf.Cos(angle),
                y: Mathf.Sin(angle));
        }

        /// <summary>
        /// <para>Returns a random point inside or on a sphere with radius 1.0</para>
        /// <i>Disclaimer: This function is not uniformly distributed</i> 
        /// </summary>
        public Vector3 InsideUnitSphere()
        {
            float phi = Value(0, 2) * Mathf.PI; // Generates angle between [0, 2*Pi)
            float theta = Value() * Mathf.PI; // Generates angle between [0, Pi)
            float radius = Value(); // Generates radius between [0, 1]

            // Convert from spherical coordinates to regular coordinates
            return convertSphericalToCartesianCoords(radius, theta, phi);
        }

        /// <summary>
        /// <para>Returns a uniformly distributed random point inside or on a sphere with radius 1.0</para>
        /// <i>Note: This function is slower than <see cref="InsideUnitSphere()">InsideUnitSphere()</see></i>
        /// </summary>
        public Vector3 InsideUnitSphereUniform()
        {
            float phi = Value(0, 2) * Mathf.PI; // Generates angle between [0, 2*Pi)
            float theta = Mathf.Acos(Value(0, 2) - 1); // Generates angle between [0, Pi)
            float radius = Mathf.Pow(Value(), 1.0f / 3); // Generates radius between [0, 1)

            // Convert from spherical coordinates to regular coordinates
            return convertSphericalToCartesianCoords(radius, theta, phi);
        }

        /// <summary>
        /// <para>Returns a random point on the surface of a sphere with radius 1.0</para>
        /// <i>Disclaimer: This function is not uniformly distributed</i> 
        /// </summary>
        public Vector3 OnUnitSphere()
        {
            float phi = Value(0, 2) * Mathf.PI; // Generates angle between [0, 2*Pi)
            float theta = Value() * Mathf.PI; // Generates angle between [0, Pi)

            // Convert from spherical coordinates to regular coordinates
            return convertSphericalToCartesianCoords(1, theta, phi);
        }

        /// <summary>
        /// <para>Returns a uniformly distributed random point on the surface of a sphere with radius 1.0</para>
        /// <i>Note: This function is slower than <see cref="OnUnitSphere()">OnUnitSphere()</see></i>
        /// </summary>
        public Vector3 OnUnitSphereUniform()
        {
            float phi = Value(0, 2) * Mathf.PI; // Generates angle between [0, 2*Pi)
            float theta = Mathf.Acos(1 - 2 * Value()); // Generates angle between [0, Pi)

            // Convert from spherical coordinates to regular coordinates
            return convertSphericalToCartesianCoords(1, theta, phi);
        }

        /// <summary>
        /// Returns a random rotation 
        /// </summary>
        public Quaternion Rotation()
        {
            float x = Value();
            float y = Value();
            float z = Value();
            float w = Value();

            return new Quaternion(x, y, z, w).normalized;
        }

        /// <summary>
        /// Returns a random float within [0, 1)
        /// </summary>
        public float Value()
        {
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Returns a random float within [0, a)
        /// </summary>
        public float Value(float a)
        {
            return Value(0, a);
        }

        /// <summary>
        /// Returns a random float within [a, b)
        /// </summary>
        public float Value(float a, float b)
        {
            if (a < b)
                return Value() * (b - a) + a;
            else
                return Value() * (a - b) + b;
        }

        /// <summary>
        /// Returns a non-negative random integer.
        /// </summary>
        public int Next()
        {
            return random.Next();
        }

        /// <summary>
        /// Returns a non-negative random integer that is less than the specified maximum.
        /// </summary>
        public int Next(int maxValue)
        {
            return random.Next(maxValue);
        }

        /// <summary>
        /// Returns a random integer that is within a specified range.
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }

        private Vector3 convertSphericalToCartesianCoords(float radius, float theta, float phi)
        {
            return new Vector3(
                x: radius * Mathf.Sin(theta) * Mathf.Cos(phi),
                y: radius * Mathf.Sin(theta) * Mathf.Sin(phi),
                z: radius * Mathf.Cos(theta));
        }
    }
}
