using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class LoadManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Slider loadingSlider;

    void Start()
    {
        // Use a coroutine to load the Scene in the background
        StartCoroutine(LoadYourAsyncScene());
    }

    IEnumerator LoadYourAsyncScene()
    {
        float progress = 0f;

        // The Application loads the Scene in the background as the current Scene runs.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Main");

        //Don't let the Scene activate until you allow it to
        asyncLoad.allowSceneActivation = false;

        while (progress <= 1f)
        {

            loadingSlider.value = progress;
            loadingText.text = "Loading... " + Mathf.Round(progress * 100f) + "%";
            progress += .01f;

            yield return new WaitForSeconds(.02f);
        }

        while (!asyncLoad.isDone && progress >= 1f)
        {
            asyncLoad.allowSceneActivation = true; //here the scene is definitely loaded.
            yield return null;

        }
    }
}
