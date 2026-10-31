using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LoadManager : MonoBehaviour
{
    // Share of the loading bar used by loading the scene, the rest is generating the terrain around the spawn point
    private const float sceneLoadShare = 0.3f;

    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private GameObject generatingPlanetText;
    [SerializeField] private Slider loadingSlider;

    /// <summary>
    /// True while this loading screen is shown, so the game scene doesn't show one of its own
    /// </summary>
    public static bool IsShowing { get; private set; }

    void Start()
    {
        // Use a coroutine to load the Scene in the background
        StartCoroutine(LoadYourAsyncScene());
    }

    IEnumerator LoadYourAsyncScene()
    {
        // Keep this loading screen through the scene change so it can also cover the terrain generation
        GameObject canvas = loadingSlider.GetComponentInParent<Canvas>().rootCanvas.gameObject;
        canvas.GetComponent<Canvas>().sortingOrder = short.MaxValue;
        DontDestroyOnLoad(canvas);
        DontDestroyOnLoad(gameObject);
        IsShowing = true;

        // The Application loads the Scene in the background as the current Scene runs.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main");

        while (!asyncLoad.isDone)
        {
            float loadProgress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            SetProgress(loadProgress * sceneLoadShare, "Pre Loading... ");
            yield return null;
        }

        generatingPlanetText.SetActive(true);
        while (!Universe.worldReady)
        {
            SetProgress(sceneLoadShare + WorldLoadingScreen.Progress * (1 - sceneLoadShare), "Generating planet... ");
            yield return null;
        }

        IsShowing = false;
        Destroy(canvas);
        Destroy(gameObject);
    }

    private void SetProgress(float progress, string label)
    {
        loadingSlider.value = progress;
        loadingText.text = label + Mathf.Round(progress * 100f) + "%";
    }

    private void OnDestroy()
    {
        IsShowing = false;
    }
}
