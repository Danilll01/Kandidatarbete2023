using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class SpaceShipCameraMover : MonoBehaviour
{

    [SerializeField] private Transform realCameraPosition;
    [SerializeField] private Vector3[] cameraPositions;
    [SerializeField] private CinemachineFreeLook freeLookCamera;
    private int currentCameraIndex = 0;
    
    // Update is called once per frame
    void Update()
    {
        // Sets free cam to its starting position
        if (!Input.GetButton("ShipFreeLook"))
        {
            freeLookCamera.m_YAxis.Value = 0.7f;
            freeLookCamera.m_XAxis.Value = 0;
        }
        
        // Enable / disable free cam
        freeLookCamera.enabled = Input.GetButton("ShipFreeLook");

        if (!Input.GetKeyDown(KeyCode.V)) { return; }

        // If the player is boarded, change camera position to next in position list
        if (Universe.player.boarded)
        {
            currentCameraIndex = ((cameraPositions.Length - 1) == currentCameraIndex) ? 0 : currentCameraIndex += 1;
            realCameraPosition.localPosition = cameraPositions[currentCameraIndex];
        }
    }
}
