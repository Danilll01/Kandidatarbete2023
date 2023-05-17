using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoManager : MonoBehaviour
{

    [SerializeField] private GameObject helpScreen;
    [SerializeField] private GameObject[] userInterface;
    private bool uiDisabled = false;

    [SerializeField] private GameObject playerShip;
    [SerializeField] private GameObject playerModel;
    
    [SerializeField] private GameObject freeFlyCamera;
    [SerializeField] private Camera[] normalCams;
    private Camera tmpCam;

    private void LateUpdate()
    {
        if (uiDisabled)
        {
            foreach (GameObject ui in userInterface)
            {
                ui.SetActive(false);
            }
        }
        else
        {
            userInterface[2].SetActive(true);
            userInterface[3].SetActive(true);
        }
        
    }

    

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            uiDisabled = !uiDisabled;
        }    
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            playerShip.SetActive(!playerShip.activeSelf);
        }  
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            playerModel.SetActive(!playerModel.activeSelf);
        }  
        if (Input.GetKeyDown(KeyCode.H))
        {
            helpScreen.SetActive(!helpScreen.activeSelf);
        } 
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (freeFlyCamera.activeSelf)
            {
                freeFlyCamera.SetActive(false);
                tmpCam.enabled = true;
            }
            else
            {
                freeFlyCamera.SetActive(true);
                tmpCam = normalCams[0].enabled ? normalCams[0] : normalCams[1];
                freeFlyCamera.transform.position = tmpCam.transform.position;
            }
        } 
    }
}
