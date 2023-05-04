using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Serialization;
using Image = UnityEngine.UI.Image;
using Random = UnityEngine.Random;

public class StartManager : MonoBehaviour
{
    [Header("Game Options")]
    [SerializeField] private TMP_InputField seedInput;
    [SerializeField] private TextMeshProUGUI nrOfPlanetsText;
    
    [Header("Audio")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TextMeshProUGUI volumeText;
    [SerializeField] private AudioClip[] buttonHoverSounds;
    [SerializeField] private AudioClip[] sliderClickSounds;
    [SerializeField] private AudioClip[] writingClickSounds;
    
    [Header("Fade-out")]
    [SerializeField] private Image fadeOutImage;
    [SerializeField] private float fadeOutTimer = 0.4f;

    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.HasKey("volume") ? PlayerPrefs.GetFloat("volume") : 0.5f;
        volumeText.text = Mathf.Round(AudioListener.volume * 100) + "%";
        volumeSlider.maxValue = 1;
        volumeSlider.minValue = 0;
        volumeSlider.value = AudioListener.volume;
    }

    /// <summary>
    /// Update the text for the planet slider corresponding to value of slider
    /// </summary>
    /// <param name="slider">The slider that holds what value</param>
    public void UpdatePlanetInputValue(Slider slider)
    {
        if (slider.value == 0)
        {
            nrOfPlanetsText.text = "X";
        }
        else
        {
            nrOfPlanetsText.text = slider.value.ToString();
        }

        musicAudioSource.PlayOneShot(sliderClickSounds[Random.Range(0, sliderClickSounds.Length)]);
    }

    /// <summary>
    /// Function to start the game
    /// </summary>
    public void StartGame()
    {
        int tryParseSeed = 0;
        int tryParsePlanets = 0;

        // These will try and parse the text inputs to ints, will return 0 if it cant
        int.TryParse(seedInput.text, out tryParseSeed);
        int.TryParse(nrOfPlanetsText.text, out tryParsePlanets);

        // If the seedInput is empty or if it can't be cast to int, randomize it
        if (string.IsNullOrEmpty(seedInput.text) || tryParseSeed == 0)
        {
            seedInput.text = Random.Range(0, 1000000).ToString();
        }

        // If there was no planets input on the slider, randomize it
        if (tryParsePlanets == 0)
        {
            int[] nrOfPlanetsArray = new int[] { 1, 2, 3, 3, 3, 4, 4, 4, 5, 5, 5 };
            int randomValue = Random.Range(0, nrOfPlanetsArray.Length);
            int nrOfPlanets = nrOfPlanetsArray[randomValue];
            Universe.nrOfPlanets = nrOfPlanets;
        }
        else
        {
            Universe.nrOfPlanets = int.Parse(nrOfPlanetsText.text);
        }

        Universe.seed = int.Parse(seedInput.text);

        // Set the seed and load the game
        Universe.InitializeRandomWithSeed();
        StartCoroutine(FadeOutMusic(fadeOutTimer));
    }
    
    private IEnumerator FadeOutMusic(float fadeDuration)
    {
        float currentVolume = musicAudioSource.volume;
        for (float timePassed = 0f; timePassed < fadeDuration; timePassed += Time.deltaTime)
        {
            musicAudioSource.volume = Mathf.Lerp(currentVolume, 0.01f, timePassed / fadeDuration);
            float newAlpha = Mathf.Lerp(0f, 255f, timePassed / fadeDuration);
            fadeOutImage.color = new Color(fadeOutImage.color.r, fadeOutImage.color.g, fadeOutImage.color.b, newAlpha / 255f);
            
            yield return null;
        }
        
        SceneManager.LoadScene("Load Menu");
    }

    /// <summary>
    /// Updates the volume in game
    /// </summary>
    /// <param name="slider">The slider value from the slider</param>
    public void UpdateVolume(Slider slider)
    {
        PlayerPrefs.SetFloat("volume", slider.value);
        AudioListener.volume = slider.value;
        float newVolume = Mathf.Round(slider.value * 100);
        volumeText.text = newVolume + "%";
    }

    /// <summary>
    /// Plays a random button hover sound
    /// </summary>
    public void PlayButtonHoverSound()
    {
        musicAudioSource.PlayOneShot(buttonHoverSounds[Random.Range(0, buttonHoverSounds.Length)]);
    }
    
    /// <summary>
    /// Plays a random writing sound
    /// </summary>
    public void PlayWritingSound()
    {
        musicAudioSource.PlayOneShot(writingClickSounds[Random.Range(0, writingClickSounds.Length)]);
    }
    
    /// <summary>
    /// Exits the game
    /// </summary>
    public void ExitGame()
    {
        Application.Quit();
    }
}
