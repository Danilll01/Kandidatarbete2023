using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Lets the web page close the game without crashing. A web build that has run a compute shader crashes with
/// "memory access out of bounds" when it quits, unless the scenes and the compute shaders are unloaded first.
/// The page calls SendMessage("WebQuit", "PrepareForQuit") and quits Unity when the game sends the ReadyToQuit event.
/// </summary>
public class WebQuit : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ReadyToQuit();
#else
    private static void ReadyToQuit() { }
#endif

    private bool preparing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;

        GameObject webQuit = new GameObject("WebQuit");
        webQuit.AddComponent<WebQuit>();
        DontDestroyOnLoad(webQuit);
    }

    /// <summary>
    /// Stops the game and unloads what makes quitting crash, then tells the page it can quit
    /// </summary>
    public void PrepareForQuit()
    {
        if (preparing) return;
        preparing = true;
        StartCoroutine(Prepare());
    }

    private IEnumerator Prepare()
    {
        // Quitting from the pause menu or the loading screen still crashed while time was stopped
        Time.timeScale = 1;

        // Swap every scene for an empty one, so nothing uses the compute shaders anymore
        Scene empty = SceneManager.CreateScene("Quitting");
        SceneManager.SetActiveScene(empty);
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene != empty) yield return SceneManager.UnloadSceneAsync(scene);
        }
        yield return Resources.UnloadUnusedAssets();

        // Unloading the scenes isn't enough on its own, the compute shaders also have to be unloaded before quitting
        foreach (ComputeShader shader in Resources.FindObjectsOfTypeAll<ComputeShader>())
            Resources.UnloadAsset(shader);

        // The GPU resources are released some frames later, and quitting right away still crashed
        yield return new WaitForSecondsRealtime(1);

        ReadyToQuit();
    }
}
