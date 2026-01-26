using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ← Для CanvasGroup!
using System.Collections; 

public class GameOverClick : MonoBehaviour
{
    [Header("Transition")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;
    public int menuSceneBuildIndex = 0;  // ← Build Index 0 (Главное меню!)

    private CanvasGroup buttonGroup;

    void Start()
    {
        buttonGroup = GetComponent<CanvasGroup>();
        if (buttonGroup == null) buttonGroup = gameObject.AddComponent<CanvasGroup>();
        
        if (fadePanel != null) fadePanel.alpha = 0f;
    }

    public void LoadMenu()
    {
        StartCoroutine(FadeToMenu());
    }

    IEnumerator FadeToMenu()
    {
        if (fadePanel != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }

        // Build Index 0 = Главное меню!
        SceneManager.LoadScene(menuSceneBuildIndex);
    }
}
