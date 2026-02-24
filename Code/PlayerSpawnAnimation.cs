using UnityEngine;
using System.Collections;

public class PlayerSpawnAnimation : MonoBehaviour
{
    [Header("=== ANIMATION TYPE ===")]
    public SpawnAnimationType animationType = SpawnAnimationType.FadeIn;

    [Header("=== SETTINGS ===")]
    public float spawnDuration = 1.5f;
    public float delayBeforeSpawn = 0.5f;
    public bool disableControlsDuringSpawn = true;

    [Header("=== FADE IN ===")] public float startAlpha = 0f;
    [Header("=== DROP IN ===")] public float dropHeight = 5f; public GameObject landingEffectPrefab;
    [Header("=== SCALE IN ===")] public float startScale = 0f;
    [Header("=== TELEPORT ===")] public GameObject teleportEffectPrefab; public Color teleportColor = Color.cyan;

    [Header("=== AUDIO ===")]
    public AudioClip spawnSound;
    [Range(0f, 1f)] public float spawnVolume = 0.7f;

    [Header("=== REFERENCES ===")]
    public Animator playerAnimator;
    public string spawnTrigger = "Spawn";

    [Header("=== WEAPON ===")]
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

        if (weaponPivot == null) { Transform wp = transform.Find("WeaponPivot"); if (wp != null) weaponPivot = wp.gameObject; }
        if (weaponPivot != null) weaponPivot.SetActive(false);
        if (disableControlsDuringSpawn) SetControls(false);
        PrepareForSpawn();
    }

    void Start() { StartCoroutine(SpawnSequence()); }

    void PrepareForSpawn()
    {
        switch (animationType)
        {
            case SpawnAnimationType.FadeIn: if (spriteRenderer != null) { Color c = spriteRenderer.color; c.a = startAlpha; spriteRenderer.color = c; } break;
            case SpawnAnimationType.DropIn: transform.position = targetPosition + Vector3.up * dropHeight; break;
            case SpawnAnimationType.ScaleIn: transform.localScale = Vector3.one * startScale; break;
            case SpawnAnimationType.Teleport: if (spriteRenderer != null) spriteRenderer.enabled = false; break;
        }
    }

    IEnumerator SpawnSequence()
    {
        yield return new WaitForSeconds(delayBeforeSpawn);
        if (spawnSound != null) audioSource.PlayOneShot(spawnSound, spawnVolume);
        if (playerAnimator != null && !string.IsNullOrEmpty(spawnTrigger)) playerAnimator.SetTrigger(spawnTrigger);

        switch (animationType)
        {
            case SpawnAnimationType.FadeIn: yield return FadeIn(); break;
            case SpawnAnimationType.DropIn: yield return DropIn(); break;
            case SpawnAnimationType.ScaleIn: yield return ScaleIn(); break;
            case SpawnAnimationType.Teleport: yield return Teleport(); break;
        }

        if (weaponPivot != null) weaponPivot.SetActive(true);
        if (disableControlsDuringSpawn) SetControls(true);
        
        // 🔥 ИСПРАВЛЕНИЕ: Принудительно возвращаем аниматор в нормальное состояние
        // Без этого после спавна анимация ходьбы может не проигрываться,
        // потому что аниматор застревает в состоянии Spawn
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(spawnTrigger);
            playerAnimator.SetFloat("Speed", 0f);
            // Пробуем Play Idle — если такое состояние есть, оно активируется
            playerAnimator.Play("Idle", 0, 0f);
        }
        
        isSpawning = false;
    }

    IEnumerator FadeIn()
    { float e = 0f; while (e < spawnDuration) { if (spriteRenderer != null) { Color c = spriteRenderer.color; c.a = Mathf.Lerp(startAlpha, originalColor.a, EaseOutCubic(e / spawnDuration)); spriteRenderer.color = c; } e += Time.deltaTime; yield return null; } if (spriteRenderer != null) spriteRenderer.color = originalColor; }

    IEnumerator DropIn()
    { Vector3 sp = transform.position; if (rb != null) { rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; } float e = 0f; while (e < spawnDuration) { transform.position = Vector3.Lerp(sp, targetPosition, EaseOutBounce(e / spawnDuration)); e += Time.deltaTime; yield return null; } transform.position = targetPosition; if (landingEffectPrefab != null) Destroy(Instantiate(landingEffectPrefab, targetPosition, Quaternion.identity), 2f); }

    IEnumerator ScaleIn()
    { float e = 0f; while (e < spawnDuration) { transform.localScale = originalScale * Mathf.Lerp(startScale, 1f, EaseOutBack(e / spawnDuration)); e += Time.deltaTime; yield return null; } transform.localScale = originalScale; }

    IEnumerator Teleport()
    { if (teleportEffectPrefab != null) Destroy(Instantiate(teleportEffectPrefab, targetPosition, Quaternion.identity), 2f); yield return new WaitForSeconds(spawnDuration * 0.3f); if (spriteRenderer != null) { spriteRenderer.enabled = true; spriteRenderer.color = teleportColor; } float e = 0f; float d = spawnDuration * 0.7f; while (e < d) { if (spriteRenderer != null) spriteRenderer.color = Color.Lerp(teleportColor, originalColor, e / d); e += Time.deltaTime; yield return null; } if (spriteRenderer != null) spriteRenderer.color = originalColor; }

    void SetControls(bool on) { if (playerMovement != null) playerMovement.enabled = on; if (playerAttack != null) playerAttack.enabled = on; }
    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseOutBack(float t) { float c1 = 1.70158f; float c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }
    float EaseOutBounce(float t) { float n1 = 7.5625f; float d1 = 2.75f; if (t < 1f / d1) return n1 * t * t; else if (t < 2f / d1) return n1 * (t -= 1.5f / d1) * t + 0.75f; else if (t < 2.5f / d1) return n1 * (t -= 2.25f / d1) * t + 0.9375f; else return n1 * (t -= 2.625f / d1) * t + 0.984375f; }
    public bool IsSpawning() => isSpawning;
}

public enum SpawnAnimationType { FadeIn, DropIn, ScaleIn, Teleport }
