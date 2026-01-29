using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem; // 🔥 ОБЯЗАТЕЛЬНО ДЛЯ NEW INPUT SYSTEM
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

    [Header("Death UI References")]
    // Назначь эти поля в Инспекторе (как у тебя на скрине)
    public Image desaturationBG;    // Твой DesaturationOverlay
    public Image deathFade;         // Твой OverlayPanel (или DeathFade)
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
    private Animator anim;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRend;
    private Color originalColor;
    private AudioSource audioSource;
    private float lastDamageTime;
    private float regenTimer;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRend = GetComponent<SpriteRenderer>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRend != null) originalColor = spriteRend.color;

        // 🔥 СБРОС UI ПРИ СТАРТЕ (Гарантия, что ничего не мешает)
        if (desaturationBG != null) 
        {
            desaturationBG.gameObject.SetActive(false); // Выключаем объект
            ForceResetCanvasGroup(desaturationBG.gameObject);
        }
        if (deathFade != null) 
        {
            deathFade.gameObject.SetActive(false);
            ForceResetCanvasGroup(deathFade.gameObject);
        }
        
        // Автопоиск музыки
        if (musicSource == null) musicSource = FindObjectOfType<AudioSource>();

        lastDamageTime = -damageCooldown;
    }

    // Хак для сброса CanvasGroup, который мешал отображению
    void ForceResetCanvasGroup(GameObject go)
    {
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f; // 🔥 ВАЖНО: Делаем группу видимой
    }

    void Update()
    {
        // 🔥 ТЕСТОВАЯ КНОПКА (New Input System)
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

        currentHealth -= damage;
        lastDamageTime = Time.time;
        regenTimer = 0f;

        if (currentHealth > 0 && spriteRend != null) StartCoroutine(FlashColor(damageColor));
        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashColor(Color targetColor)
    {
        spriteRend.color = targetColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRend.color = originalColor;
    }

    void Die()
    {
        Debug.Log("DIE() CALLED");
        isDead = true;

        // 1. Блокировка управления
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;
        
        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
        }

        // 2. Анимация
        if (anim != null) anim.SetTrigger("Die");

        // 3. Звук
        if (deathSound != null && audioSource != null)
            audioSource.PlayOneShot(deathSound, deathVolume);

        // 4. Запуск эффекта
        StartCoroutine(GungeonDeathEffect());
    }

    IEnumerator GungeonDeathEffect()
    {
        Debug.Log("STARTING DEATH EFFECT...");
        
        // Ждем анимацию смерти (0.8 сек реального времени)
        yield return new WaitForSecondsRealtime(0.8f);

        // 🔥 ПАУЗА ИГРЫ
        Time.timeScale = 0f;
        Debug.Log("GAME PAUSED (TimeScale = 0)");

        // 🔥 БЛОКИРОВКА КУРСОРА
        if (lockCursorOnDeath)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // 🔥 ПАУЗА МУЗЫКИ
        if (musicSource != null) musicSource.Pause();

        // 🔥 СЕРЫЙ ЭКРАН (Обесцвечивание)
        if (desaturationBG != null)
        {
            desaturationBG.gameObject.SetActive(true); // Включаем объект
            
            Color startColor = desaturationBG.color;
            startColor.a = 0;
            desaturationBG.color = startColor;

            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 2f; // Быстрое появление
                Color c = desaturationBG.color;
                c.a = Mathf.Lerp(0f, 0.8f, t); // До 0.8 прозрачности
                desaturationBG.color = c;
                yield return null;
            }
        }
        else
        {
            Debug.LogError("DesaturationBG Reference is MISSING!");
        }

        // Драматическая пауза (3 сек)
        Debug.Log("WAITING 3 SECONDS...");
        yield return new WaitForSecondsRealtime(deathPauseDuration);

        // 🔥 ЧЕРНОЕ ЗАТЕМНЕНИЕ
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

        Debug.Log("LOADING SCENE...");
        Time.timeScale = 1f; // Возвращаем время перед загрузкой, иначе новая сцена будет на паузе!
        SceneManager.LoadScene(deathSceneName);
    }
}
