using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeText;
    [SerializeField] private PillPlayerController playerController;

    private AudioSource musicAudioSource;


    void Awake()
    {
        #if DEBUG || UNITY_EDITOR
        DisplayDebug.Initalize(debugContainer);
        debugContainer.SetActive(false);
        #endif
        
        pausContainer.SetActive(false);
        seedText.text = "SEED " + Universe.seed;
        AudioListener.volume = PlayerPrefs.HasKey("volume") ? PlayerPrefs.GetFloat("volume") : 0.5f;
        volumeText.text = Mathf.Round(AudioListener.volume * 100) + "%";
        volumeSlider.maxValue = 1;
        volumeSlider.minValue = 0;
        volumeSlider.value = AudioListener.volume;

        // Check in case we started from the main menu
        GameObject musicObject = GameObject.Find("Music");
        if (musicObject != null)
        {
            musicAudioSource = musicObject.GetComponent<AudioSource>();
            StartCoroutine(FadeOutMusic(2f));
        }
        

    }

    /// <summary>
    /// Updates the audio volume
    /// </summary>
    /// <param name="slider">The slider that changes the volume</param>
    public void UpdateVolume(Slider slider)
    {
        PlayerPrefs.SetFloat("volume", slider.value);
        AudioListener.volume = slider.value;
        float newVolume = Mathf.Round(slider.value * 100);
        volumeText.text = newVolume + "%";
    }

    void Update()
    {
        #if DEBUG || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            bool isActive = debugContainer.activeSelf;
            debugContainer.SetActive(!isActive);
        }
        #endif

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PausGame();
        }

        #if DEBUG || UNITY_EDITOR
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
        #endif
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

    private IEnumerator FadeOutMusic(float fadeDuration)
    {
        float currentVolume = musicAudioSource.volume;
        for (float timePassed = 0f; timePassed < fadeDuration; timePassed += Time.deltaTime)
        {
            musicAudioSource.volume = Mathf.Lerp(currentVolume, 0.01f, timePassed / fadeDuration);

            yield return null;
        }
    }

}
