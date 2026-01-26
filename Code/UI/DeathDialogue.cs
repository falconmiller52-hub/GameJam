using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ← Для CanvasGroup!
using System.Collections;

public class DeathDialogueManager : MonoBehaviour
{
    public IntroDialogue dialogueScript;
    public GameObject fadePanel; // Черная панель

    [Header("Music")]
    public AudioSource musicSource;

    [Header("Transition")]
    public string nextSceneName = "GameOver"; // GameOver сцена
    public float fadeDuration = 1.5f;

    private CanvasGroup fadeGroup;

    void Start()
    {
        // Автозапуск музыки
        if (musicSource == null)
        {
            GameObject musicObj = GameObject.Find("MusicManager");
            if (musicObj != null) musicSource = musicObj.GetComponent<AudioSource>();
        }
        if (musicSource != null && !musicSource.isPlaying)
        {
            musicSource.Play();
        }

        // Настройка Fade
        if (fadePanel != null)
        {
            fadeGroup = fadePanel.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = fadePanel.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
        }

        dialogueScript.BeginDialogue();
        StartCoroutine(CheckForEnd());
    }

    IEnumerator CheckForEnd()
    {
        while (!dialogueScript.IsFinished)
        {
            yield return null;
        }

        // ПЛАВНОЕ затемнение!
        if (fadeGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }
        else if (fadePanel != null)
        {
            fadePanel.SetActive(true);
            yield return new WaitForSeconds(fadeDuration);
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
