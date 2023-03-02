using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalSun : MonoBehaviour
{
    [SerializeField] private Transform player;     // Player to point light towards
    [SerializeField] private Gradient skyGradient;  // Sky color gradient

    private Transform sun;                          // Sun to point light from
    private PillPlayerController playerScriptToGetPlanet;

    /// <summary>
    /// Initializes the script to make directional light point from the sun object
    /// </summary>
    /// <param name="sun">The sunubject to point light from</param>
    public void Initialize(Transform sun) {
        this.sun = sun;
        playerScriptToGetPlanet = player.GetComponent<PillPlayerController>();
    }

    // Update is called once per frame
    void Update() {
        UpdateDirection();
        UpdateAmbientLight();
    }

    private void UpdateAmbientLight() {
        if (playerScriptToGetPlanet != null) {
            Vector3 planetPosition = playerScriptToGetPlanet.attractor.transform.position;
            float sunPlanetDistance = Vector3.Distance(sun.transform.position, planetPosition);
            float planetRadius = playerScriptToGetPlanet.attractor.radius;

            float maxCutOfDistace = Vector3.Distance(Vector3.zero, new Vector3(sunPlanetDistance, planetRadius));
            float minDistance = sunPlanetDistance - planetRadius;

            // Will be between 0 and 1 with 1 being when the player is near sun (max light) and 0 being 90 degrees to the side of the planet (lowest ambient light) 
            float lightAmount  = Mathf.InverseLerp(maxCutOfDistace, minDistance, Vector3.Distance(sun.position, player.position));

            RenderSettings.ambientLight = skyGradient.Evaluate(lightAmount);
        }
    }

    // Updates the direction of the light
    private void UpdateDirection() {
        if (this.sun != null) {
            Vector3 sunToPlayerVector = Vector3.Normalize(player.transform.position - sun.position);
            transform.rotation = Quaternion.LookRotation(sunToPlayerVector);
        }
    }
}
