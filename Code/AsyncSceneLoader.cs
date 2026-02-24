using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Async scene loader with fade transition.
/// Place on a persistent object OR call the static method.
/// 
/// Usage (from any script):
///   AsyncSceneLoader.LoadSceneAsync("Level1");
///   AsyncSceneLoader.LoadSceneAsync("Level1", myFadeCanvasGroup, 1f);
/// </summary>
public class AsyncSceneLoader : MonoBehaviour
{
    private static AsyncSceneLoader _instance;

    [Header("=== DEFAULT FADE ===")]
    [Tooltip("Default fade panel (optional, can pass per-call)")]
    public CanvasGroup fadePanel;
    public float defaultFadeDuration = 0.5f;

    [Header("=== LOADING SCREEN (optional) ===")]
    [Tooltip("Loading screen UI object")]
    public GameObject loadingScreen;
    public Image progressBar;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    /// <summary>
    /// Load scene asynchronously. Can be called from anywhere.
    /// </summary>
    public static void LoadSceneAsync(string sceneName, CanvasGroup fade = null, float fadeDuration = 0.5f)
    {
        if (_instance != null)
        {
            _instance.StartCoroutine(_instance.LoadRoutine(sceneName, fade, fadeDuration));
        }
        else
        {
            // Fallback: create temporary loader
            GameObject loaderObj = new GameObject("TempAsyncLoader");
            AsyncSceneLoader loader = loaderObj.AddComponent<AsyncSceneLoader>();
            _instance = loader;
            loader.StartCoroutine(loader.LoadRoutine(sceneName, fade, fadeDuration));
        }
    }

    /// <summary>
    /// Load scene by build index asynchronously.
    /// </summary>
    public static void LoadSceneAsync(int buildIndex, CanvasGroup fade = null, float fadeDuration = 0.5f)
    {
        string sceneName = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        // Extract scene name from path
        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
        LoadSceneAsync(sceneName, fade, fadeDuration);
    }

    IEnumerator LoadRoutine(string sceneName, CanvasGroup fade, float fadeDuration)
    {
        // Use provided fade or default
        CanvasGroup activeFade = fade != null ? fade : fadePanel;

        // Fade out
        if (activeFade != null)
        {
            activeFade.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                activeFade.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            activeFade.alpha = 1f;
        }

        // Show loading screen
        if (loadingScreen != null) loadingScreen.SetActive(true);

        // Async load
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            if (progressBar != null)
                progressBar.fillAmount = op.progress / 0.9f;
            yield return null;
        }

        if (progressBar != null) progressBar.fillAmount = 1f;

        // Small delay for smooth transition
        yield return new WaitForSecondsRealtime(0.1f);

        // Ensure time scale is reset
        Time.timeScale = 1f;

        op.allowSceneActivation = true;
    }
}
