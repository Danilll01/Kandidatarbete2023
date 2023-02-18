using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugManager : MonoBehaviour
{
    private float refresh = 0.5f;
    private float timer = 0.5f;
    private float timelapse = 0;
    private int fps;

    private GameObject debugContainer;


    void Awake()
    {
        debugContainer = GameObject.FindGameObjectWithTag("Debug");
        DisplayDebug.InitalizeDebugManager(debugContainer);
        debugContainer.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isActive = debugContainer.activeSelf;
            debugContainer.SetActive(!isActive);
        }

        if (debugContainer.activeSelf)
        {
            timelapse = Time.unscaledDeltaTime;
            if (timer <= 0)
            {
                GetFPS(timelapse);
                DisplayDebug.AddOrSetDebugVariable("FPS", fps, 0);
                DisplayDebug.UpdateDebugs();
            }

            timer = timer <= 0 ? refresh : timer -= timelapse;
        }
    }


    private void GetFPS(float time)
    {
        fps = (int)(1.0f / time);
    }


}
