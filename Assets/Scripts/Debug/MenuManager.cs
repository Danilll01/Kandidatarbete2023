using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuManager : MonoBehaviour
{
    private float refresh = 0.5f;
    private float timer = 0.5f;
    private float timelapse = 0;
    private int fps;

    [SerializeField] private GameObject debugContainer;
    [SerializeField] private GameObject pausContainer;
    [SerializeField] private TextMeshProUGUI seedText;
    [SerializeField] private PillPlayerController playerController;


    void Awake()
    {
        DisplayDebug.Initalize(debugContainer);
        debugContainer.SetActive(false);
        pausContainer.SetActive(false);
        seedText.text = "Seed: " + Universe.seed;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isActive = debugContainer.activeSelf;
            debugContainer.SetActive(!isActive);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PausGame();
        }

        if (debugContainer.activeSelf)
        {
            timelapse = Time.unscaledDeltaTime;
            if (timer <= 0)
            {
                fps = (int)(1.0f / timelapse);
                DisplayDebug.AddOrSetDebugVariable("FPS", fps, 0);
                DisplayDebug.UpdateDebugs();
            }

            timer = timer <= 0 ? refresh : timer -= timelapse;
        }
    }

    private void PausGame()
    {
        bool isActive = pausContainer.activeSelf;
        if (!isActive)
        {
            playerController.paused = true;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            Time.timeScale = 0;
        }
        else
        {
            playerController.paused = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1;
        }
        pausContainer.SetActive(!isActive);
    }

    /// <summary>
    /// Loads the start menu
    /// </summary>
    public void BackToStart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Start Menu");
    }

    /// <summary>
    /// Resumes the game
    /// </summary>
    public void ResumeGame()
    {
        PausGame();
    }

}
