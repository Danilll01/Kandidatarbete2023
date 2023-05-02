using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LoadManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private GameObject generatingPlanetText;
    [SerializeField] private Slider loadingSlider;

    void Start()
    {
        // Use a coroutine to load the Scene in the background
        StartCoroutine(LoadYourAsyncScene());
    }

    IEnumerator LoadYourAsyncScene()
    {
        
        // The Application loads the Scene in the background as the current Scene runs.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main");

        while (!asyncLoad.isDone)
        {

            float loadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            loadingSlider.value = loadProgress;
            float roundedProgress = Mathf.Round(loadProgress * 100f);
            loadingText.text = "Pre Loading... " + roundedProgress + "%";

            if (roundedProgress >= 99.8f)
            {
                generatingPlanetText.SetActive(true);
            }
            
            yield return null; 
        }

    }
}
