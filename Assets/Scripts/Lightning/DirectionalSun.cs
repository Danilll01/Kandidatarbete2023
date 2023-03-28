using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalSun : MonoBehaviour
{
    [SerializeField] private Transform player;     // Player to point light towards
    [SerializeField] private Gradient skyGradient;  // Sky color gradient

    private Transform sun;                          // Sun to point light from
    private Planet currentPlanet;
    private AtmosphereHandler atmosphere;
    private float planetRadius;

    /// <summary>
    /// Initializes the script to make directional light point from the sun object
    /// </summary>
    /// <param name="sun">The sun object to point light from</param>
    public void Initialize()
    {
        this.sun = Universe.sunPosition;
        currentPlanet = player.GetComponent<PillPlayerController>().attractor;
        atmosphere = currentPlanet.atmosphereHandler;
        planetRadius = currentPlanet.radius;
    }

    // Update is called once per frame
    void Update() {
        UpdateDirection();
        UpdateAmbientLight();
    }

    private void UpdateAmbientLight() {

        // This basically works by calculating the distance to the planet light edge and the shortest distance to the sun.
        // Then whatever the player distance falls in between these values that is whats is looked up in the gradient and set as the color
        // Therefore when the player is on the back half of the planet the ambient light will be low and the more near the sun the lighter the light will be
        if (currentPlanet != null) {
            atmosphere.UpdateAtmosphereAmbient(skyGradient);
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
