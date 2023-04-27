using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleBackgroundMusic : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private AudioClip[] audioclips;
    private AudioSource audioSource;

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
    public void UpdateClipIndex(BackgroundClips backgroundClip)
    {
        audioSource.clip = audioclips[(int)backgroundClip];
        audioSource.Play();
    }
}
