using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pauses the game until the terrain around the spawn point is fully generated, and covers the screen
/// when the Load Menu's loading screen isn't already doing that (when Main is started directly in the Editor)
/// </summary>
public class WorldLoadingScreen : MonoBehaviour
{
    // Start the game anyway if generation takes longer than this
    private const float timeout = 90f;

    private ChunksHandler spawnTerrain;
    private TextMeshProUGUI progressText;
    private Image progressBar;
    private float startTime;

    /// <summary>
    /// How much of the terrain around the spawn point is generated, between 0 and 1
    /// </summary>
    public static float Progress { get; private set; }

    public static void Show(ChunksHandler spawnTerrain)
    {
        WorldLoadingScreen screen = new GameObject("World Loading Screen").AddComponent<WorldLoadingScreen>();
        screen.spawnTerrain = spawnTerrain;
    }

    private void Awake()
    {
        Universe.worldReady = false;
        Progress = 0;
        Time.timeScale = 0;
        startTime = Time.unscaledTime;

        // The Load Menu's loading screen shows the progress when the game is started from the menus
        if (!LoadManager.IsShowing) CreateUI();
    }

    private void Update()
    {
        bool ready = spawnTerrain == null || (spawnTerrain.HighGridReady && spawnTerrain.OutstandingNearbyChunks == 0);
        Progress = spawnTerrain == null ? 1 : spawnTerrain.HighGridProgress;

        if (progressText != null)
        {
            progressText.text = "Generating planet... " + Mathf.RoundToInt(Progress * 100) + "%";
            progressBar.rectTransform.anchorMax = new Vector2(Progress, 1);
        }

        if (ready || Time.unscaledTime - startTime > timeout)
        {
            if (!ready) Debug.LogWarning("Terrain around the spawn point was not finished in time, starting anyway");
            Progress = 1;
            Time.timeScale = 1;
            Universe.worldReady = true;
            Destroy(gameObject);
        }
    }

    private void CreateUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        Image background = CreateRect<Image>("Background", transform, Vector2.zero, Vector2.one);
        background.color = Color.black;

        progressText = CreateRect<TextMeshProUGUI>("Text", transform, new Vector2(0.2f, 0.45f), new Vector2(0.8f, 0.55f));
        progressText.alignment = TextAlignmentOptions.Center;
        progressText.fontSize = 36;

        Image barBackground = CreateRect<Image>("Bar", transform, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.42f));
        barBackground.color = new Color(1, 1, 1, 0.15f);
        progressBar = CreateRect<Image>("Fill", barBackground.transform, Vector2.zero, new Vector2(0, 1));
    }

    private static T CreateRect<T>(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax) where T : Component
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return child.AddComponent<T>();
    }
}
