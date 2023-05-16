using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoManager : MonoBehaviour
{

    [SerializeField] private GameObject[] userInterface;
    private bool uiDisabled = false;

    [SerializeField] private GameObject playerShip;
    [SerializeField] private GameObject playerModel;



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
    }
}
