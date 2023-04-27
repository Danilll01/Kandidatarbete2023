using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleBackgroundMusic : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private AudioClip[] audioclips;
    private AudioSource audioSource;
    private float duration = 3f;
    private const float FADED_OUT_VOLUME = 0.01f;

    public void Initialize()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioclips[0];
        audioSource.Play();
    }

    public enum BackgroundClips
    {
        Space,
        Planet
    }

    // Update is called once per frame
    public IEnumerator UpdateClipIndex(BackgroundClips backgroundClip)
    {

        var originalVolume = audioSource.volume;

        // I prefer using for loops over while to eliminate the danger of infinite loops
        // and the need for "external" variables
        // I personally also find this better to read and maintain
        for (var timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(originalVolume, FADED_OUT_VOLUME, timePassed / duration);

            yield return null;
        }

        // To be sure to end with clean values
        audioSource.volume = FADED_OUT_VOLUME;

        // If there is only one instance of `AudioManager` in your scene this is more efficient
        // in general you should fetch that AudioManager reference only ONCE and re-use it
        audioSource.clip = audioclips[(int)backgroundClip];
        //audioSource.clip = GameObject.FindWithTag("AudioManager").GetComponent<AudioManager>().clips[clip];

        yield return new WaitForSeconds(0.1f);

        // Actually start playing the new clip
        audioSource.Play();

        for (var timePassed = 0f; timePassed < duration; timePassed += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(FADED_OUT_VOLUME, originalVolume, timePassed / duration);

            yield return null;
        }

        audioSource.volume = 0.5f;
    }
}
