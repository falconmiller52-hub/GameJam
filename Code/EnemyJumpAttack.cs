using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Прыжковая атака для врагов типа "Голем".
/// 
/// ИСПРАВЛЕНО:
/// - После стана приземления принудительно возвращает Animator в Idle
/// - Сбрасывает ВСЕ триггеры перед каждой фазой
/// - RecoveryPhase ставит IsAirborne=false и триггерит Idle переход
/// </summary>
public class EnemyJumpAttack : MonoBehaviour
{
    [Header("=== ДИСТАНЦИЯ АТАКИ ===")]
    public float minJumpDistance = 2f;
    public float maxJumpDistance = 7f;

    [Header("=== ПАРАМЕТРЫ ПРЫЖКА ===")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.6f;
    public float jumpCooldown = 4f;
    public float landingStunDuration = 1.0f;

    [Header("=== ТЕЛЕГРАФ ===")]
    public float telegraphDuration = 0.8f;
    public Color telegraphColor = new Color(1f, 0.8f, 0.3f, 1f);
    public bool shakeOnTelegraph = true;
    public float shakeIntensity = 0.1f;

    [Header("=== УРОН ПО ОБЛАСТИ ===")]
    public float damageRadius = 1.5f;
    public int damageAmount = 2;
    public float knockbackForce = 8f;

    [Header("=== ИНДИКАТОР ОБЛАСТИ ===")]
    public GameObject areaIndicatorPrefab;
    public Color indicatorWarningColor = new Color(1f, 1f, 0f, 0.5f);
    public Color indicatorDangerColor = new Color(1f, 0f, 0f, 0.7f);
    public float indicatorBlinkTime = 0.3f;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    public Color jumpColor = new Color(0.8f, 0.4f, 0.1f, 1f);
    public GameObject landingEffectPrefab;
    public bool showJumpShadow = true;
    public GameObject shadowPrefab;

    [Header("=== АНИМАЦИЯ ===")]
    public Animator animator;
    public string telegraphTrigger = "JumpTelegraph";
    public string jumpTrigger = "Jump";
    public string landTrigger = "Land";
    public string isAirborneBool = "IsAirborne";

    [Header("=== АУДИО ===")]
    public AudioClip telegraphSound;
    [Range(0f, 1f)] public float telegraphVolume = 0.6f;
    public AudioClip jumpSound;
    [Range(0f, 1f)] public float jumpVolume = 0.7f;
    public AudioClip landSound;
    [Range(0f, 1f)] public float landVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool showDebugGizmos = true;
    public bool debugLogs = false;

    private Transform playerTarget;
    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private bool isJumping = false;
    private bool isTelegraphing = false;
    private bool canJump = true;
    private float lastJumpTime = -999f;
    private Color originalColor;
    private Vector3 originalPosition;
    private Vector2 targetPosition;
    private GameObject currentIndicator;
    private GameObject currentShadow;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (spriteRenderer != null) originalColor = spriteRenderer.color;
        FindPlayer();
    }

    void Update()
    {
        if (playerTarget == null) { FindPlayer(); return; }
        if (isJumping || isTelegraphing) return;
        if (enemyHealth != null && enemyHealth.IsDead) return;
        if (!canJump) return;

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        if (dist >= minJumpDistance && dist <= maxJumpDistance && Time.time >= lastJumpTime + jumpCooldown)
        {
            StartCoroutine(JumpAttackSequence());
        }
    }

    void FindPlayer()
    {
        if (enemyAI != null && enemyAI.playerTarget != null)
        { playerTarget = enemyAI.playerTarget; return; }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    // ==================== ГЛАВНАЯ ПОСЛЕДОВАТЕЛЬНОСТЬ ====================

    IEnumerator JumpAttackSequence()
    {
        yield return StartCoroutine(TelegraphPhase());

        if (enemyHealth != null && enemyHealth.IsDead)
        { CleanupIndicators(); ResetState(); yield break; }

        yield return StartCoroutine(JumpPhase());
        yield return StartCoroutine(LandingPhase());
        yield return StartCoroutine(RecoveryPhase());
    }

    // ==================== ТЕЛЕГРАФ ====================

    IEnumerator TelegraphPhase()
    {
        isTelegraphing = true;

        if (enemyAI != null) enemyAI.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        targetPosition = playerTarget.position;
        originalPosition = transform.position;

        SpawnAreaIndicator(targetPosition);

        // 🔥 Сбрасываем все триггеры перед телеграфом
        ResetAllTriggers();
        if (animator != null) animator.SetTrigger(telegraphTrigger);

        if (telegraphSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(telegraphSound, telegraphVolume);
        }

        float elapsed = 0f;
        Vector3 originalLocalPos = transform.localPosition;

        while (elapsed < telegraphDuration)
        {
            if (spriteRenderer != null)
            {
                float t = Mathf.PingPong(elapsed * 8f, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, telegraphColor, t);
            }

            if (shakeOnTelegraph)
            {
                float sx = Random.Range(-shakeIntensity, shakeIntensity);
                float sy = Random.Range(-shakeIntensity, shakeIntensity);
                transform.localPosition = originalLocalPos + new Vector3(sx, sy, 0);
            }

            if (currentIndicator != null)
            {
                SpriteRenderer isr = currentIndicator.GetComponent<SpriteRenderer>();
                if (isr != null)
                    isr.color = Color.Lerp(indicatorWarningColor, indicatorDangerColor, elapsed / telegraphDuration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalLocalPos;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        isTelegraphing = false;
    }

    // ==================== ПРЫЖОК ====================

    IEnumerator JumpPhase()
    {
        isJumping = true;
        lastJumpTime = Time.time;

        // 🔥 Сбрасываем триггеры и ставим новый
        ResetAllTriggers();
        if (animator != null)
        {
            animator.SetTrigger(jumpTrigger);
            animator.SetBool(isAirborneBool, true);
        }

        if (jumpSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(jumpSound, jumpVolume);
        }

        if (spriteRenderer != null) spriteRenderer.color = jumpColor;
        if (showJumpShadow) SpawnShadow(targetPosition);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            float heightOffset = 4f * jumpHeight * t * (1f - t);
            currentPos.y += heightOffset;
            transform.position = currentPos;

            if (currentShadow != null)
            {
                float shadowScale = 1f - (heightOffset / jumpHeight) * 0.5f;
                currentShadow.transform.localScale = Vector3.one * shadowScale * damageRadius;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        if (col != null) col.enabled = true;
    }

    // ==================== ПРИЗЕМЛЕНИЕ ====================

    IEnumerator LandingPhase()
    {
        Vector3 landingPosition = transform.position;

        // 🔥 Сбрасываем триггеры
        ResetAllTriggers();
        if (animator != null)
        {
            animator.SetBool(isAirborneBool, false);
            animator.SetTrigger(landTrigger);
        }

        transform.position = landingPosition;

        if (landSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(landSound, landVolume);
        }

        if (spriteRenderer != null) spriteRenderer.color = originalColor;

        if (landingEffectPrefab != null)
        {
            GameObject eff = Instantiate(landingEffectPrefab, transform.position, Quaternion.identity);
            Destroy(eff, 2f);
        }

        DealAreaDamage();

        if (currentIndicator != null) StartCoroutine(BlinkAndDestroyIndicator());
        if (currentShadow != null) { Destroy(currentShadow); currentShadow = null; }

        // Стан
        if (rb != null) rb.linearVelocity = Vector2.zero;

        float stunElapsed = 0f;
        while (stunElapsed < landingStunDuration)
        {
            transform.position = landingPosition;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            stunElapsed += Time.deltaTime;
            yield return null;
        }

        isJumping = false;
    }

    // ==================== ВОССТАНОВЛЕНИЕ ====================

    IEnumerator RecoveryPhase()
    {
        canJump = false;

        // 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: возвращаем аниматор в Idle
        ResetAllTriggers();
        if (animator != null)
        {
            animator.SetBool(isAirborneBool, false);
            // Принудительно проигрываем Idle
            animator.Play("Idle", 0, 0f);
        }

        // Включаем AI
        if (enemyAI != null && (enemyHealth == null || !enemyHealth.IsDead))
        {
            enemyAI.enabled = true;
        }

        yield return new WaitForSeconds(jumpCooldown);
        canJump = true;
    }

    // ==================== УТИЛИТЫ ====================

    /// <summary>
    /// 🔥 Сбрасывает ВСЕ триггеры чтобы Animator не застревал
    /// </summary>
    void ResetAllTriggers()
    {
        if (animator == null) return;
        animator.ResetTrigger(telegraphTrigger);
        animator.ResetTrigger(jumpTrigger);
        animator.ResetTrigger(landTrigger);
    }

    void DealAreaDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.CompareTag("Player"))
            {
                PlayerHealth ph = hit.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    // 📊 АНАЛИТИКА: урон от прыгающего врага
                    if (GameAnalyticsManager.Instance != null)
                        GameAnalyticsManager.Instance.SetLastDamageSource("jumper_aoe");
                    ph.TakeDamage(damageAmount);
                }
                Rigidbody2D prb = hit.GetComponent<Rigidbody2D>();
                if (prb != null && knockbackForce > 0)
                {
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    prb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    void SpawnAreaIndicator(Vector2 position)
    {
        if (currentIndicator != null) Destroy(currentIndicator);
        if (areaIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(areaIndicatorPrefab, position, Quaternion.identity);
            currentIndicator.transform.localScale = Vector3.one * damageRadius * 2f;
        }
        else
        {
            currentIndicator = CreateSimpleCircle(position, damageRadius, indicatorWarningColor);
        }
    }

    GameObject CreateSimpleCircle(Vector2 position, float radius, Color color)
    {
        GameObject circle = new GameObject("AreaIndicator");
        circle.transform.position = new Vector3(position.x, position.y, 0);

        SpriteRenderer sr = circle.AddComponent<SpriteRenderer>();
        sr.sortingOrder = -1;

        Texture2D tex = new Texture2D(64, 64);
        Color[] cols = new Color[4096];
        Vector2 center = new Vector2(32, 32);
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                cols[y * 64 + x] = d < 30 ? new Color(color.r, color.g, color.b, color.a * (1f - d / 30f)) : Color.clear;
            }
        tex.SetPixels(cols); tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f);
        sr.color = color;
        circle.transform.localScale = Vector3.one * radius * 2f;
        return circle;
    }

    void SpawnShadow(Vector2 position)
    {
        if (currentShadow != null) Destroy(currentShadow);
        if (shadowPrefab != null)
            currentShadow = Instantiate(shadowPrefab, position, Quaternion.identity);
        else
            currentShadow = CreateSimpleCircle(position, damageRadius * 0.5f, new Color(0, 0, 0, 0.3f));
    }

    IEnumerator BlinkAndDestroyIndicator()
    {
        if (currentIndicator == null) yield break;
        SpriteRenderer sr = currentIndicator.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(currentIndicator); currentIndicator = null; yield break; }

        float elapsed = 0f;
        while (elapsed < indicatorBlinkTime)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }
        Destroy(currentIndicator);
        currentIndicator = null;
    }

    void CleanupIndicators()
    {
        if (currentIndicator != null) { Destroy(currentIndicator); currentIndicator = null; }
        if (currentShadow != null) { Destroy(currentShadow); currentShadow = null; }
    }

    void ResetState()
    {
        isJumping = false;
        isTelegraphing = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        ResetAllTriggers();
        if (animator != null) animator.SetBool(isAirborneBool, false);
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    public void InterruptJump()
    {
        if (isJumping || isTelegraphing)
        {
            StopAllCoroutines();
            CleanupIndicators();
            ResetState();
        }
    }

    public bool IsJumping() => isJumping;
    public bool IsTelegraphing() => isTelegraphing;
    public bool IsBusy() => isJumping || isTelegraphing;

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, minJumpDistance);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, maxJumpDistance);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
}
