using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Конечные титры — строки текста плавно появляются и исчезают по очереди.
/// После последнего блока игра закрывается автоматически.
///
/// КАК НАСТРОИТЬ:
/// 1. Создай новую сцену "Credits"
/// 2. Добавь её ПОСЛЕДНЕЙ в Build Settings (например, индекс 4)
///
/// 3. Создай в сцене:
///    - Main Camera (Background Color = чёрный, Clear Flags = Solid Color)
///    - Canvas (Screen Space — Overlay)
///      └── CreditText (TextMeshPro — UGUI, по центру экрана, белый, 
///                       выравнивание по центру, шрифт по вкусу)
///    - Пустой GameObject "CreditsManager" с этим скриптом
///
/// 4. В Inspector CreditsManager назначь:
///    - Credit Text → CreditText (TextMeshProUGUI)
///    - Credit Lines → массив строк титров (каждый элемент = один блок)
///
/// 5. ВАЖНО: В WaveSpawner.EndingSequence() — после затемнения
///    замени загрузку сцены на "Credits":
///    SceneManager.LoadScene("Credits");
///
/// Пример строк:
///   [0] "A Game by\nYour Studio Name"
///   [1] "Programming\nYour Name"
///   [2] "Art & Design\nYour Name"
///   [3] "Music\nArtist Name"
///   [4] "Special Thanks\nFriends & Family"
///   [5] "Thank you for playing"
///
/// Используй \n для переноса строки внутри одного блока.
/// </summary>
public class CreditsRoll : MonoBehaviour
{
    [Header("=== TEXT ===")]
    [Tooltip("UI текст для отображения титров")]
    public TextMeshProUGUI creditText;

    [Header("=== CREDIT LINES ===")]
    [Tooltip("Массив строк титров. Каждый элемент — один блок (появляется и исчезает)")]
    [TextArea(2, 5)]
    public string[] creditLines;

    [Header("=== TIMING ===")]
    [Tooltip("Задержка перед началом (тёмный экран)")]
    public float initialDelay = 1.5f;
    
    [Tooltip("Время появления текста")]
    public float fadeInDuration = 1.0f;
    
    [Tooltip("Время показа текста")]
    public float displayDuration = 2.5f;
    
    [Tooltip("Время исчезновения текста")]
    public float fadeOutDuration = 1.0f;
    
    [Tooltip("Пауза между блоками")]
    public float betweenBlockDelay = 0.8f;
    
    [Tooltip("Задержка перед закрытием игры после последнего блока")]
    public float endDelay = 2.0f;

    [Header("=== AUDIO (опционально) ===")]
    [Tooltip("Фоновая музыка титров")]
    public AudioClip creditsMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    
    [Tooltip("Время fade-in музыки")]
    public float musicFadeInDuration = 2f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Скрываем текст
        if (creditText != null)
        {
            creditText.text = "";
            SetTextAlpha(0f);
        }

        // Запускаем музыку
        if (creditsMusic != null)
        {
            audioSource.clip = creditsMusic;
            audioSource.volume = 0f;
            audioSource.loop = true;
            audioSource.Play();
            StartCoroutine(FadeInMusic());
        }

        // Убедимся что Time.timeScale нормальный
        Time.timeScale = 1f;

        // Курсор видим
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        StartCoroutine(CreditsSequence());
    }

    IEnumerator CreditsSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        if (creditLines == null || creditLines.Length == 0)
        {
            Debug.LogWarning("[CreditsRoll] Нет строк титров!");
            yield return new WaitForSeconds(endDelay);
            QuitGame();
            yield break;
        }

        // Показываем каждый блок по очереди
        for (int i = 0; i < creditLines.Length; i++)
        {
            yield return StartCoroutine(ShowCreditBlock(creditLines[i]));

            // Пауза между блоками (кроме последнего)
            if (i < creditLines.Length - 1)
                yield return new WaitForSeconds(betweenBlockDelay);
        }

        // Задержка после последнего блока
        yield return new WaitForSeconds(endDelay);

        // Fade out музыки
        if (creditsMusic != null && audioSource.isPlaying)
        {
            float elapsed = 0f;
            float startVol = audioSource.volume;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / 1.5f);
                yield return null;
            }
        }

        // Закрываем игру
        QuitGame();
    }

    IEnumerator ShowCreditBlock(string text)
    {
        if (creditText == null) yield break;

        creditText.text = text;

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(Mathf.Clamp01(elapsed / fadeInDuration));
            yield return null;
        }
        SetTextAlpha(1f);

        // Display
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            SetTextAlpha(1f - Mathf.Clamp01(elapsed / fadeOutDuration));
            yield return null;
        }
        SetTextAlpha(0f);

        creditText.text = "";
    }

    IEnumerator FadeInMusic()
    {
        float elapsed = 0f;
        while (elapsed < musicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / musicFadeInDuration);
            yield return null;
        }
        audioSource.volume = musicVolume;
    }

    void SetTextAlpha(float alpha)
    {
        if (creditText == null) return;
        Color c = creditText.color;
        c.a = alpha;
        creditText.color = c;
    }

    void QuitGame()
    {
        Debug.Log("[CreditsRoll] Титры завершены. Закрываем игру.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
