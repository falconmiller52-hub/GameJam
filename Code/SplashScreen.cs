using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Начальные титры — логотип студии.
/// Показывает лого, плавно проявляет его, держит, плавно убирает, грузит MainMenu.
///
/// КАК НАСТРОИТЬ:
/// 1. Создай новую сцену "SplashScreen" 
/// 2. Поставь её ПЕРВОЙ в Build Settings (индекс 0)
///    Build Settings должен выглядеть так:
///      0 — SplashScreen
///      1 — MainMenu
///      2 — Level1
///      3 — DeathDialogue
///      (+ Credits если добавишь)
///
/// 3. Создай в сцене:
///    - Main Camera (с чёрным Background Color)
///    - Canvas (Screen Space — Overlay)
///      └── LogoImage (UI Image, по центру, назначь спрайт логотипа)
///      └── FadePanel (UI Image, чёрный, растянут на весь экран, 
///                     добавь CanvasGroup компонент)
///    - Пустой GameObject "SplashManager" с этим скриптом
///
/// 4. В Inspector SplashManager назначь:
///    - Logo Image → LogoImage
///    - Fade Panel → CanvasGroup на FadePanel
///    - Next Scene Name → "MainMenu"
///    - (опционально) Studio Sound → звук при появлении лого
///
/// 5. При желании можно добавить несколько лого (massив logoImages)
/// </summary>
public class SplashScreen : MonoBehaviour
{
    [Header("=== LOGO ===")]
    [Tooltip("UI Image с логотипом студии")]
    public Image logoImage;
    
    [Tooltip("Дополнительные лого (опционально — покажутся по очереди)")]
    public Image[] additionalLogos;

    [Header("=== TIMING ===")]
    [Tooltip("Задержка перед началом (чёрный экран)")]
    public float initialDelay = 0.5f;
    
    [Tooltip("Время появления логотипа")]
    public float fadeInDuration = 1.0f;
    
    [Tooltip("Время показа логотипа")]
    public float displayDuration = 2.0f;
    
    [Tooltip("Время исчезновения логотипа")]
    public float fadeOutDuration = 0.8f;
    
    [Tooltip("Пауза между несколькими лого")]
    public float betweenLogoDelay = 0.5f;

    [Header("=== TRANSITION ===")]
    [Tooltip("Панель затемнения (CanvasGroup)")]
    public CanvasGroup fadePanel;
    
    [Tooltip("Название следующей сцены")]
    public string nextSceneName = "MainMenu";

    [Header("=== AUDIO ===")]
    [Tooltip("Звук при появлении лого")]
    public AudioClip studioSound;
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    [Header("=== SKIP ===")]
    [Tooltip("Можно ли пропустить кликом/кнопкой?")]
    public bool allowSkip = true;

    private AudioSource audioSource;
    private bool isSkipping = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Скрываем всё в начале
        if (logoImage != null)
            SetImageAlpha(logoImage, 0f);
        
        if (additionalLogos != null)
        {
            foreach (Image img in additionalLogos)
            {
                if (img != null) SetImageAlpha(img, 0f);
            }
        }

        // Фейд-панель полностью непрозрачна (чёрный экран)
        if (fadePanel != null)
            fadePanel.alpha = 0f;

        StartCoroutine(SplashSequence());
    }

    void Update()
    {
        if (allowSkip && !isSkipping)
        {
            // New Input System — проверяем мышь и клавиатуру
            bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            
            if (clicked || keyPressed)
            {
                isSkipping = true;
            }
        }
    }

    IEnumerator SplashSequence()
    {
        // Начальная задержка
        yield return new WaitForSeconds(initialDelay);

        // Показываем основной логотип
        if (logoImage != null)
        {
            yield return StartCoroutine(ShowLogo(logoImage));
        }

        // Показываем дополнительные лого (если есть)
        if (additionalLogos != null)
        {
            foreach (Image img in additionalLogos)
            {
                if (isSkipping) break;
                if (img == null) continue;
                
                yield return new WaitForSeconds(betweenLogoDelay);
                yield return StartCoroutine(ShowLogo(img));
            }
        }

        // 🔥 Начинаем загрузку следующей сцены ВО ВРЕМЯ затемнения
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;

        // Затемнение
        if (fadePanel != null)
        {
            float elapsed = 0f;
            float dur = isSkipping ? 0.3f : 0.5f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Clamp01(elapsed / dur);
                yield return null;
            }
            fadePanel.alpha = 1f;
        }

        // Ждём загрузки
        while (asyncLoad.progress < 0.9f)
            yield return null;

        asyncLoad.allowSceneActivation = true;
    }

    IEnumerator ShowLogo(Image logo)
    {
        // Звук
        if (studioSound != null)
            audioSource.PlayOneShot(studioSound, soundVolume);

        // Fade in
        float elapsed = 0f;
        float duration = isSkipping ? 0.2f : fadeInDuration;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetImageAlpha(logo, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        SetImageAlpha(logo, 1f);

        // Display
        if (!isSkipping)
        {
            float displayElapsed = 0f;
            while (displayElapsed < displayDuration && !isSkipping)
            {
                displayElapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Fade out
        elapsed = 0f;
        duration = isSkipping ? 0.2f : fadeOutDuration;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetImageAlpha(logo, 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        SetImageAlpha(logo, 0f);
    }

    void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}
