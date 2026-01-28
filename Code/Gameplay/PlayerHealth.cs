using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    [Header("UI Settings")]
    public GameObject deathFadePanel;

    [Header("Audio")]
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathVolume = 1f; // 🔥 НОВАЯ НАСТРОЙКА ГРОМКОСТИ (0.0 - 1.0)

    [Header("Death Scene")]
    public string deathSceneName = "DeathDialogue";

    [Header("Timing")]
    public float waitBeforeFade = 2f;
    public float fadeDuration = 1f;

    [Header("Damage FX")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;

    private bool isDead = false;
    private Animator anim;
    private Rigidbody2D rb;
    private CanvasGroup fadeGroup;
    private SpriteRenderer spriteRend; 
    private Color originalColor; 
    private AudioSource audioSource; // 🔥 Ссылка на источник звука

    void Start()
    {
        currentHealth = maxHealth;
        
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // Находим или добавляем AudioSource для качественного звука
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        spriteRend = GetComponent<SpriteRenderer>();
        if (spriteRend != null)
        {
            originalColor = spriteRend.color;
        }
        
        if (deathFadePanel != null)
        {
            fadeGroup = deathFadePanel.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = deathFadePanel.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;  
            fadeGroup.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        if (currentHealth > 0 && spriteRend != null)
        {
            StartCoroutine(DamageFlash());
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator DamageFlash()
    {
        spriteRend.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRend.color = originalColor;
    }

    void Die()
    {
        isDead = true;

        if (spriteRend != null) spriteRend.color = originalColor;

        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        if (anim != null) anim.SetTrigger("Die");

        // 🔥 ИСПОЛЬЗУЕМ PLAY ONE SHOT С ГРОМКОСТЬЮ
        if (deathSound != null && audioSource != null)
        {
            // Здесь мы передаем громкость из переменной deathVolume
            audioSource.PlayOneShot(deathSound, deathVolume);
        }

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(waitBeforeFade);

        if (deathFadePanel != null)
        {
            deathFadePanel.SetActive(true);
            
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadeGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        SceneManager.LoadScene(deathSceneName);
    }
}
