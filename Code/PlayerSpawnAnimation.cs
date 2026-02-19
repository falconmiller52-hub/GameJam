using UnityEngine;
using System.Collections;

/// <summary>
/// Анимация появления игрока в начале уровня.
/// Добавь на объект игрока.
/// </summary>
public class PlayerSpawnAnimation : MonoBehaviour
{
    [Header("=== ТИП АНИМАЦИИ ===")]
    public SpawnAnimationType animationType = SpawnAnimationType.FadeIn;

    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Длительность анимации появления")]
    public float spawnDuration = 1.5f;
    
    [Tooltip("Задержка перед появлением")]
    public float delayBeforeSpawn = 0.5f;
    
    [Tooltip("Блокировать управление во время анимации?")]
    public bool disableControlsDuringSpawn = true;

    [Header("=== FADE IN ===")]
    [Tooltip("Начальная прозрачность")]
    public float startAlpha = 0f;

    [Header("=== DROP IN ===")]
    [Tooltip("Высота падения")]
    public float dropHeight = 5f;
    
    [Tooltip("Эффект при приземлении")]
    public GameObject landingEffectPrefab;

    [Header("=== SCALE IN ===")]
    [Tooltip("Начальный масштаб")]
    public float startScale = 0f;

    [Header("=== TELEPORT ===")]
    [Tooltip("Эффект телепортации")]
    public GameObject teleportEffectPrefab;
    
    [Tooltip("Цвет телепортации")]
    public Color teleportColor = Color.cyan;

    [Header("=== АУДИО ===")]
    public AudioClip spawnSound;
    [Range(0f, 1f)]
    public float spawnVolume = 0.7f;

    [Header("=== ССЫЛКИ ===")]
    public Animator playerAnimator;
    public string spawnTrigger = "Spawn";

    // Приватные переменные
    private SpriteRenderer spriteRenderer;
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;
    private Rigidbody2D rb;
    private Vector3 targetPosition;
    private Color originalColor;
    private Vector3 originalScale;
    private AudioSource audioSource;
    private bool isSpawning = true;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody2D>();
        
        if (playerAnimator == null)
            playerAnimator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Сохраняем оригинальные значения
        targetPosition = transform.position;
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        originalScale = transform.localScale;

        // Блокируем управление
        if (disableControlsDuringSpawn)
        {
            SetControlsEnabled(false);
        }

        // Подготовка к анимации
        PrepareForSpawn();
    }

    void Start()
    {
        StartCoroutine(SpawnSequence());
    }

    void PrepareForSpawn()
    {
        switch (animationType)
        {
            case SpawnAnimationType.FadeIn:
                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    c.a = startAlpha;
                    spriteRenderer.color = c;
                }
                break;

            case SpawnAnimationType.DropIn:
                transform.position = targetPosition + Vector3.up * dropHeight;
                break;

            case SpawnAnimationType.ScaleIn:
                transform.localScale = Vector3.one * startScale;
                break;

            case SpawnAnimationType.Teleport:
                if (spriteRenderer != null)
                    spriteRenderer.enabled = false;
                break;
        }
    }

    IEnumerator SpawnSequence()
    {
        // Задержка перед появлением
        yield return new WaitForSeconds(delayBeforeSpawn);

        // Звук появления
        if (spawnSound != null)
        {
            audioSource.PlayOneShot(spawnSound, spawnVolume);
        }

        // Триггер анимации
        if (playerAnimator != null && !string.IsNullOrEmpty(spawnTrigger))
        {
            playerAnimator.SetTrigger(spawnTrigger);
        }

        // Выполняем анимацию
        switch (animationType)
        {
            case SpawnAnimationType.FadeIn:
                yield return StartCoroutine(FadeInAnimation());
                break;

            case SpawnAnimationType.DropIn:
                yield return StartCoroutine(DropInAnimation());
                break;

            case SpawnAnimationType.ScaleIn:
                yield return StartCoroutine(ScaleInAnimation());
                break;

            case SpawnAnimationType.Teleport:
                yield return StartCoroutine(TeleportAnimation());
                break;
        }

        // Включаем управление
        if (disableControlsDuringSpawn)
        {
            SetControlsEnabled(true);
        }

        isSpawning = false;
        Debug.Log("[PlayerSpawnAnimation] Анимация появления завершена!");
    }

    IEnumerator FadeInAnimation()
    {
        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            float t = elapsed / spawnDuration;
            t = EaseOutCubic(t); // Плавное замедление в конце

            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(startAlpha, originalColor.a, t);
                spriteRenderer.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Финал
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    IEnumerator DropInAnimation()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        // Отключаем гравитацию на время анимации
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        while (elapsed < spawnDuration)
        {
            float t = elapsed / spawnDuration;
            t = EaseOutBounce(t); // Эффект "прыжка" при приземлении

            transform.position = Vector3.Lerp(startPos, targetPosition, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;

        // Эффект приземления
        if (landingEffectPrefab != null)
        {
            GameObject effect = Instantiate(landingEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Возвращаем гравитацию (для 2D обычно 0)
        if (rb != null)
            rb.gravityScale = 0f;
    }

    IEnumerator ScaleInAnimation()
    {
        float elapsed = 0f;

        while (elapsed < spawnDuration)
        {
            float t = elapsed / spawnDuration;
            t = EaseOutBack(t); // Эффект "выпрыгивания"

            float scale = Mathf.Lerp(startScale, 1f, t);
            transform.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    IEnumerator TeleportAnimation()
    {
        // Эффект телепортации
        if (teleportEffectPrefab != null)
        {
            GameObject effect = Instantiate(teleportEffectPrefab, targetPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Вспышка цвета
        yield return new WaitForSeconds(spawnDuration * 0.3f);

        // Появляемся
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = teleportColor;
        }

        // Плавно возвращаем цвет
        float elapsed = 0f;
        float fadeDuration = spawnDuration * 0.7f;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(teleportColor, originalColor, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    void SetControlsEnabled(bool enabled)
    {
        if (playerMovement != null)
            playerMovement.enabled = enabled;
        
        if (playerAttack != null)
            playerAttack.enabled = enabled;
    }

    // ===== ФУНКЦИИ ПЛАВНОСТИ =====

    float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseOutBounce(float t)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        else if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        else if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        else
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }

    /// <summary>
    /// Проверяет, идёт ли сейчас анимация появления
    /// </summary>
    public bool IsSpawning() => isSpawning;
}

/// <summary>
/// Типы анимации появления
/// </summary>
public enum SpawnAnimationType
{
    FadeIn,     // Плавное появление (прозрачность)
    DropIn,     // Падение сверху
    ScaleIn,    // Увеличение из точки
    Teleport    // Телепортация с эффектом
}
