using UnityEngine;
using System.Collections;

public class PlayerSpawnAnimation : MonoBehaviour
{
    [Header("=== ТИП АНИМАЦИИ ===")]
    public SpawnAnimationType animationType = SpawnAnimationType.FadeIn;

    [Header("=== НАСТРОЙКИ ===")]
    public float spawnDuration = 1.5f;
    public float delayBeforeSpawn = 0.5f;
    public bool disableControlsDuringSpawn = true;

    [Header("=== FADE IN ===")]
    public float startAlpha = 0f;

    [Header("=== DROP IN ===")]
    public float dropHeight = 5f;
    public GameObject landingEffectPrefab;

    [Header("=== SCALE IN ===")]
    public float startScale = 0f;

    [Header("=== TELEPORT ===")]
    public GameObject teleportEffectPrefab;
    public Color teleportColor = Color.cyan;

    [Header("=== АУДИО ===")]
    public AudioClip spawnSound;
    [Range(0f, 1f)] public float spawnVolume = 0.7f;

    [Header("=== ССЫЛКИ ===")]
    public Animator playerAnimator;
    public string spawnTrigger = "Spawn";

    [Header("=== ОРУЖИЕ ===")]
    [Tooltip("WeaponPivot — родитель оружия (скрывается при спавне)")]
    public GameObject weaponPivot;

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
        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        targetPosition = transform.position;
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        originalScale = transform.localScale;

        // 🔥 Автопоиск WeaponPivot
        if (weaponPivot == null)
        {
            Transform wp = transform.Find("WeaponPivot");
            if (wp != null) weaponPivot = wp.gameObject;
        }

        // 🔥 Прячем оружие
        if (weaponPivot != null)
        {
            weaponPivot.SetActive(false);
            Debug.Log("[SpawnAnim] Оружие скрыто на время спавна");
        }

        if (disableControlsDuringSpawn) SetControlsEnabled(false);
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
                { Color c = spriteRenderer.color; c.a = startAlpha; spriteRenderer.color = c; }
                break;
            case SpawnAnimationType.DropIn:
                transform.position = targetPosition + Vector3.up * dropHeight;
                break;
            case SpawnAnimationType.ScaleIn:
                transform.localScale = Vector3.one * startScale;
                break;
            case SpawnAnimationType.Teleport:
                if (spriteRenderer != null) spriteRenderer.enabled = false;
                break;
        }
    }

    IEnumerator SpawnSequence()
    {
        yield return new WaitForSeconds(delayBeforeSpawn);

        if (spawnSound != null) audioSource.PlayOneShot(spawnSound, spawnVolume);
        if (playerAnimator != null && !string.IsNullOrEmpty(spawnTrigger))
            playerAnimator.SetTrigger(spawnTrigger);

        switch (animationType)
        {
            case SpawnAnimationType.FadeIn: yield return StartCoroutine(FadeInAnimation()); break;
            case SpawnAnimationType.DropIn: yield return StartCoroutine(DropInAnimation()); break;
            case SpawnAnimationType.ScaleIn: yield return StartCoroutine(ScaleInAnimation()); break;
            case SpawnAnimationType.Teleport: yield return StartCoroutine(TeleportAnimation()); break;
        }

        // 🔥 Показываем оружие после анимации
        if (weaponPivot != null)
        {
            weaponPivot.SetActive(true);
            Debug.Log("[SpawnAnim] Оружие показано!");
        }

        if (disableControlsDuringSpawn) SetControlsEnabled(true);
        isSpawning = false;
    }

    IEnumerator FadeInAnimation()
    {
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            float t = EaseOutCubic(elapsed / spawnDuration);
            if (spriteRenderer != null)
            { Color c = spriteRenderer.color; c.a = Mathf.Lerp(startAlpha, originalColor.a, t); spriteRenderer.color = c; }
            elapsed += Time.deltaTime; yield return null;
        }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    IEnumerator DropInAnimation()
    {
        Vector3 startPos = transform.position;
        if (rb != null) { rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; }
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            transform.position = Vector3.Lerp(startPos, targetPosition, EaseOutBounce(elapsed / spawnDuration));
            elapsed += Time.deltaTime; yield return null;
        }
        transform.position = targetPosition;
        if (landingEffectPrefab != null) { GameObject e = Instantiate(landingEffectPrefab, targetPosition, Quaternion.identity); Destroy(e, 2f); }
        if (rb != null) rb.gravityScale = 0f;
    }

    IEnumerator ScaleInAnimation()
    {
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            transform.localScale = originalScale * Mathf.Lerp(startScale, 1f, EaseOutBack(elapsed / spawnDuration));
            elapsed += Time.deltaTime; yield return null;
        }
        transform.localScale = originalScale;
    }

    IEnumerator TeleportAnimation()
    {
        if (teleportEffectPrefab != null) { GameObject e = Instantiate(teleportEffectPrefab, targetPosition, Quaternion.identity); Destroy(e, 2f); }
        yield return new WaitForSeconds(spawnDuration * 0.3f);
        if (spriteRenderer != null) { spriteRenderer.enabled = true; spriteRenderer.color = teleportColor; }
        float elapsed = 0f; float dur = spawnDuration * 0.7f;
        while (elapsed < dur)
        { if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(teleportColor, originalColor, elapsed / dur); elapsed += Time.deltaTime; yield return null; }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    void SetControlsEnabled(bool enabled)
    {
        if (playerMovement != null) playerMovement.enabled = enabled;
        if (playerAttack != null) playerAttack.enabled = enabled;
    }

    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseOutBack(float t) { float c1 = 1.70158f; float c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }
    float EaseOutBounce(float t) { float n1 = 7.5625f; float d1 = 2.75f; if (t < 1f / d1) return n1 * t * t; else if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f; else if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f; else return n1 * (t -= 2.625f / d1) * t + 0.984375f; }

    public bool IsSpawning() => isSpawning;
}

public enum SpawnAnimationType { FadeIn, DropIn, ScaleIn, Teleport }
