using System;
using System.Collections;
using UnityEngine;

public class HandleAudio : MonoBehaviour
{
    [SerializeField] private AudioClip[] backgroundMusicAudioClips;
    [SerializeField] private AudioClip[] soundEffectsAudioClips;
    [SerializeField] private const float backgroundMusicVolume = 0.04f;
    [SerializeField] private const float soundEffectsVolume = 0.1f;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource soundEffectsAudioSource;
    private float duration = 2f;
    private const float FADED_OUT_VOLUME = 0.01f;
    private bool stoppedSoundEffects;
    private bool gameIsPaused;
    private Coroutine fadeInCoroutine;
    private Coroutine fadeOutCoroutine;

    /// <summary>
    /// Initialize the audio components
    /// </summary>
    public void Initialize()
    {
        musicAudioSource.clip = backgroundMusicAudioClips[0];
        musicAudioSource.volume = backgroundMusicVolume;
        soundEffectsAudioSource.volume = soundEffectsVolume;
        musicAudioSource.Play();
        StartCoroutine(InitializeBackgroundMusic());
    }

    private void Update()
    {
        if (Time.timeScale == 0 && !gameIsPaused)
        {
            soundEffectsAudioSource.mute = true;
            gameIsPaused = true;
        }
        else if (Time.timeScale == 1 && gameIsPaused)
        {
            soundEffectsAudioSource.mute = false;
            gameIsPaused = false;
        }
    }

    /// <summary>
    /// Enums for the different types of background clips
    /// </summary>
    public enum BackgroundClips
    {
        Space,
        Planet
    }
    
    /// <summary>
    /// Enums for the different sound effects
    /// </summary>
    public enum SoundEffects
    {
        Thrust,
        TakeOff,
        Landing
    }

    /// <summary>
    /// Play a given sound effect
    /// </summary>
    /// <param name="soundEffect"></param>
    /// <param name="loop"></param>
    public void PlaySoundEffect(SoundEffects soundEffect, bool loop, bool instantly, float fadeInDuration = 1f, float volume = soundEffectsVolume)
    {
        AudioClip newClip = soundEffectsAudioClips[(int)soundEffect];
        soundEffectsAudioSource.volume = volume;
        if (newClip != soundEffectsAudioSource.clip)
        {
            StopCurrentCoroutines();
            soundEffectsAudioSource.clip = soundEffectsAudioClips[(int)soundEffect];
            soundEffectsAudioSource.loop = loop;
            if (instantly)
            {
                soundEffectsAudioSource.volume = volume;
                soundEffectsAudioSource.Play();
            }
            else
            {
                soundEffectsAudioSource.volume = 0;
                soundEffectsAudioSource.Play();
                fadeInCoroutine = StartCoroutine(FadeInSoundEffect(fadeInDuration, volume));
            }
            
        }
        else if (stoppedSoundEffects)
        {
            StopCurrentCoroutines();
            if (instantly)
            {
                soundEffectsAudioSource.volume = volume;
                soundEffectsAudioSource.Play();
            }
            else
            {
                soundEffectsAudioSource.volume = 0;
                soundEffectsAudioSource.Play();
                fadeInCoroutine = StartCoroutine(FadeInSoundEffect(fadeInDuration, volume));
            }
            stoppedSoundEffects = false;
        }
    }

    /// <summary>
    /// Turn off the currently played sound effect
    /// </summary>
    public void TurnOffCurrentSoundEffect(float fadeOutDuration = 0.5f)
    {
        if (!stoppedSoundEffects)
        {
            StopCurrentCoroutines();

            fadeOutCoroutine = StartCoroutine(FadeOutSoundEffect(fadeOutDuration));
        }
        stoppedSoundEffects = true;
    }

    private void StopCurrentCoroutines()
    {
        if (fadeInCoroutine != null && fadeOutCoroutine != null)
        {
            StopCoroutine(fadeOutCoroutine);
            StopCoroutine(fadeInCoroutine);
        }
    }

    private IEnumerator FadeOutSoundEffect(float fadeDuration)
    {
        float currentVolume = soundEffectsAudioSource.volume;
        for (var timePassed = 0f; timePassed < fadeDuration; timePassed += Time.deltaTime)
        {
            soundEffectsAudioSource.volume = Mathf.Lerp(currentVolume, FADED_OUT_VOLUME, timePassed / fadeDuration);

            yield return null;
        }
    }
    
    private IEnumerator FadeInSoundEffect(float fadeInDuration, float volume)
    {
        for (var timePassed = 0f; timePassed < fadeInDuration; timePassed += Time.deltaTime)
        {
            soundEffectsAudioSource.volume = Mathf.Lerp(FADED_OUT_VOLUME, volume, timePassed / fadeInDuration);

            yield return null;
        }
    }

    private IEnumerator InitializeBackgroundMusic()
    {
        for (var timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
        {
            musicAudioSource.volume = Mathf.Lerp(FADED_OUT_VOLUME, backgroundMusicVolume, timePassed / duration);

            yield return null;
        }
    }

    /// <summary>
    /// Change the background music to the given music clip
    /// </summary>
    /// <param name="backgroundClip"></param>
    /// <returns></returns>
    public IEnumerator UpdateMusicClipIndex(BackgroundClips backgroundClip, float volume = backgroundMusicVolume)
    {

        var originalVolume = musicAudioSource.volume;

        // I prefer using for loops over while to eliminate the danger of infinite loops
        // and the need for "external" variables
        // I personally also find this better to read and maintain
        for (var timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
        {
            musicAudioSource.volume = Mathf.Lerp(originalVolume, FADED_OUT_VOLUME, timePassed / duration);

            yield return null;
        }

        // To be sure to end with clean values
        musicAudioSource.volume = FADED_OUT_VOLUME;

        // If there is only one instance of `AudioManager` in your scene this is more efficient
        // in general you should fetch that AudioManager reference only ONCE and re-use it
        musicAudioSource.clip = backgroundMusicAudioClips[(int)backgroundClip];
        //audioSource.clip = GameObject.FindWithTag("AudioManager").GetComponent<AudioManager>().clips[clip];

        yield return new WaitForSeconds(0.1f);

        // Actually start playing the new clip
        musicAudioSource.Play();

        for (var timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
        {
            musicAudioSource.volume = Mathf.Lerp(FADED_OUT_VOLUME, volume, timePassed / duration);

            yield return null;
        }
        
        musicAudioSource.volume = volume;
    }
}