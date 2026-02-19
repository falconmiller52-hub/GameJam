using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Regeneration")]
    public bool enableRegen = true;
    public int regenAmount = 1;
    public float regenRate = 1.0f;
    public float damageCooldown = 4.0f;

    [Header("=== НЕУЯЗВИМОСТЬ (I-Frames) ===")]
    [Tooltip("Длительность неуязвимости после урона (сек)")]
    public float invincibilityDuration = 1.5f;
    [Tooltip("Скорость мигания спрайта (раз в секунду)")]
    public float blinkRate = 10f;

    [Header("Death UI References")]
    public Image desaturationBG;
    public Image deathFade;
    public AudioSource musicSource;

    [Header("Death Settings")]
    public float deathPauseDuration = 3f;
    public bool lockCursorOnDeath = true;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathVolume = 1f;
    public string deathSceneName = "DeathDialogue";

    [Header("Visual Feedback")]
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public float flashDuration = 0.1f;

    private bool isDead = false;
    private bool isInvincible = false; // 🔥 Флаг неуязвимости
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRend;
    private Color originalColor;
    private AudioSource audioSource;
    private float lastDamageTime;
    private float regenTimer;
    private Coroutine invincibilityCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRend = GetComponent<SpriteRenderer>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRend != null) originalColor = spriteRend.color;

        if (desaturationBG != null)
        {
            desaturationBG.gameObject.SetActive(false);
            ForceResetCanvasGroup(desaturationBG.gameObject);
        }
        if (deathFade != null)
        {
            deathFade.gameObject.SetActive(false);
            ForceResetCanvasGroup(deathFade.gameObject);
        }

        if (musicSource == null) musicSource = FindObjectOfType<AudioSource>();
        lastDamageTime = -damageCooldown;
    }

    void ForceResetCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame && !isDead)
        {
            Debug.Log("TEST KILL BUTTON PRESSED");
            currentHealth = 0;
            Die();
        }

        if (isDead || !enableRegen) return;

        if (currentHealth < maxHealth)
        {
            if (Time.time >= lastDamageTime + damageCooldown)
            {
                regenTimer += Time.deltaTime;
                if (regenTimer >= regenRate)
                {
                    Heal(regenAmount);
                    regenTimer = 0f;
                }
            }
        }
    }

    public void Heal(int amount)
    {
        if (isDead || currentHealth >= maxHealth) return;
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (spriteRend != null) StartCoroutine(FlashColor(healColor));
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // 🔥 Неуязвимость — полностью игнорируем урон
        if (isInvincible) return;

        // Проверяем щит
        PlayerShield shield = GetComponent<PlayerShield>();
        if (shield != null && shield.HasShield())
        {
            damage = shield.TakeDamage(damage);
            if (damage <= 0)
            {
                // Щит поглотил весь урон — запускаем i-frames
                StartInvincibility();
                return;
            }
        }

        currentHealth -= damage;
        lastDamageTime = Time.time;
        regenTimer = 0f;

        if (currentHealth > 0)
        {
            // 🔥 Запускаем неуязвимость + мигание
            StartInvincibility();
        }

        if (currentHealth <= 0) Die();
    }

    // ==================== НЕУЯЗВИМОСТЬ ====================

    /// <summary>
    /// Запускает период неуязвимости с миганием спрайта
    /// </summary>
    void StartInvincibility()
    {
        if (invincibilityCoroutine != null)
            StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine());
    }

    IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invincibilityDuration)
        {
            // Мигание спрайта
            if (spriteRend != null)
            {
                visible = !visible;
                Color c = spriteRend.color;
                c.a = visible ? 1f : 0.2f;
                spriteRend.color = c;
            }

            // Ждём до следующего мигания
            float blinkInterval = 1f / blinkRate;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        // Гарантируем видимость после окончания
        if (spriteRend != null)
        {
            Color c = spriteRend.color;
            c.a = 1f;
            spriteRend.color = c;
        }

        isInvincible = false;
        invincibilityCoroutine = null;
    }

    /// <summary>
    /// Проверка — неуязвим ли игрок (для внешних скриптов)
    /// </summary>
    public bool IsInvincible => isInvincible;

    // ==================== ВИЗУАЛ ====================

    IEnumerator FlashColor(Color targetColor)
    {
        if (spriteRend == null) yield break;
        spriteRend.color = targetColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRend.color = originalColor;
    }

    // ==================== СМЕРТЬ ====================

    void Die()
    {
        Debug.Log("DIE() CALLED");
        isDead = true;

        // Останавливаем мигание
        if (invincibilityCoroutine != null)
        {
            StopCoroutine(invincibilityCoroutine);
            isInvincible = false;
            if (spriteRend != null)
            {
                Color c = spriteRend.color;
                c.a = 1f;
                spriteRend.color = c;
            }
        }

        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        if (anim != null) anim.SetTrigger("Die");

        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound, deathVolume);

        StartCoroutine(GungeonDeathEffect());
    }

    IEnumerator GungeonDeathEffect()
    {
        yield return new WaitForSecondsRealtime(0.8f);

        Time.timeScale = 0f;

        if (lockCursorOnDeath)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (musicSource != null) musicSource.Pause();

        if (desaturationBG != null)
        {
            desaturationBG.gameObject.SetActive(true);
            Color startColor = desaturationBG.color;
            startColor.a = 0;
            desaturationBG.color = startColor;

            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 2f;
                Color c = desaturationBG.color;
                c.a = Mathf.Lerp(0f, 0.8f, t);
                desaturationBG.color = c;
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(deathPauseDuration);

        if (deathFade != null)
        {
            deathFade.gameObject.SetActive(true);
            Color startColor = deathFade.color;
            startColor.a = 0;
            deathFade.color = startColor;

            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 1f;
                Color c = deathFade.color;
                c.a = t;
                deathFade.color = c;
                yield return null;
            }
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(deathSceneName);
    }
}
