using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShipCameraMover : MonoBehaviour
{

    [SerializeField] private Transform realCameraPosition;
    [SerializeField] private Vector3[] cameraPositions;
    [SerializeField] private GameObject freeLookCamera;
    private int currentCameraIndex = 0;

    // Update is called once per frame
    void Update()
    {
        // 
        freeLookCamera.SetActive(Input.GetButton("ShipFreeLook"));

        if (!Input.GetKeyDown(KeyCode.V)) { return; }

        // If the player is boarded, change camera position to next in position list
        if (Universe.player.boarded)
        {
            currentCameraIndex = ((cameraPositions.Length - 1) == currentCameraIndex) ? 0 : currentCameraIndex += 1;
            realCameraPosition.localPosition = cameraPositions[currentCameraIndex];
        }
    }
}
