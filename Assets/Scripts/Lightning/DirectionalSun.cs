using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalSun : MonoBehaviour
{
    [SerializeField] private Transform player;     // Player to point light towards

    private Transform sun;                          // Sun to point light from
    private PillPlayerController currentPlayer;

    /// <summary>
    /// Initializes the script to make directional light point from the sun object
    /// </summary>
    public void Initialize()
    {
        sun = Universe.sunPosition;
        
        GetComponent<Light>().colorTemperature = sun.GetComponent<Sun>().temperature;
        currentPlayer = player.GetComponent<PillPlayerController>();
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
        if (currentPlayer.attractor != null) {
            currentPlayer.attractor.atmosphereHandler.UpdateAtmosphereAmbient();
        }
    }

    // Updates the direction of the light
    private void UpdateDirection()
    {
        if (sun == null) return;
        Vector3 sunToPlayerVector = Vector3.Normalize(player.transform.position - sun.position);
        transform.rotation = Quaternion.LookRotation(sunToPlayerVector);
    }
}
