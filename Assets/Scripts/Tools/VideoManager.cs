using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VideoManager : MonoBehaviour
{

    [SerializeField] private GameObject helpScreen;
    [SerializeField] private GameObject[] userInterface;
    private bool uiDisabled = false;

    [SerializeField] private GameObject playerShip;
    [SerializeField] private GameObject playerModel;
    [SerializeField] private SkinnedMeshRenderer playerHeadModel;
    
    [SerializeField] private GameObject freeFlyCamera;
    [SerializeField] private Camera[] normalCams;
    private Camera tmpCam;
    
    [SerializeField] private FreeFlyCamera flyScript;
    [SerializeField] private PillPlayerController pillScript;
    [SerializeField] private SpaceShipController shipScript;
    [SerializeField] private SolarSystemTransform solarSystemScripts;

    [SerializeField] private AudioSource[] audioSources;

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
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            playerHeadModel.shadowCastingMode = playerHeadModel.shadowCastingMode == ShadowCastingMode.ShadowsOnly ?  ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
        } 
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            audioSources[0].enabled = !audioSources[0].enabled;
        } 
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            audioSources[1].enabled = !audioSources[1].enabled;
        } 
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            audioSources[2].enabled = !audioSources[2].enabled;
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
                pillScript.canMove = true;
                shipScript.canMove = true;
            }
            else
            {
                freeFlyCamera.SetActive(true);
                tmpCam = normalCams[0].enabled ? normalCams[0] : normalCams[1];
                freeFlyCamera.transform.position = tmpCam.transform.position;
                flyScript.canMove = true;
                pillScript.canMove = false;
                shipScript.canMove = false;
            }
        } 
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            if (freeFlyCamera.activeSelf)
            {
                flyScript.canMove = !flyScript.canMove;
                pillScript.canMove = !pillScript.canMove;
                shipScript.canMove = !shipScript.canMove;
            }
            
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            solarSystemScripts.stopSolarSytemMovement = !solarSystemScripts.stopSolarSytemMovement;
        }
    }
}
