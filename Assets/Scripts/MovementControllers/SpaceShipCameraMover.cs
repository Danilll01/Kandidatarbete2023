using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceShipCameraMover : MonoBehaviour
{

    [SerializeField] private Transform realCameraPosition;
    [SerializeField] private Vector3[] cameraPositions;
    private int currentCameraIndex = 0;

    // Update is called once per frame
    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.V)) { return; }

        if (Universe.player.boarded)
        {
            Debug.Log(currentCameraIndex);
            Debug.Log(((cameraPositions.Length - 1) == currentCameraIndex));
            Debug.Log(cameraPositions.Length - 1);
            
            currentCameraIndex = ((cameraPositions.Length - 1) == currentCameraIndex) ? 0 : currentCameraIndex += 1;
            realCameraPosition.localPosition = cameraPositions[currentCameraIndex];
        }
    }
}
