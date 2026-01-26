using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ← ДОБАВИЛИ для CanvasGroup!
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    [Header("UI Settings")]
    public GameObject deathFadePanel; // Ссылка на панель затемнения

    [Header("Audio")]
    public AudioClip deathSound; // Звук смерти игрока

    [Header("Death Scene")]
    public string deathSceneName = "DeathDialogue"; // ← ГИБКОЕ ИМЯ!

    [Header("Timing")]
    public float waitBeforeFade = 2f;      // Ждать анимацию смерти
    public float fadeDuration = 1f;        // Плавное затемнение

    private bool isDead = false;
    private Animator anim;
    private Rigidbody2D rb;
    private CanvasGroup fadeGroup;         // ← CanvasGroup для плавности!

    void Start()
    {
        currentHealth = maxHealth;
        
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        // Настраиваем панель затемнения
        if (deathFadePanel != null)
        {
            fadeGroup = deathFadePanel.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = deathFadePanel.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;  // Прозрачная на старте
            fadeGroup.gameObject.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        // Можно добавить звук получения урона (хрюканье) здесь
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        // Отключаем управление движением
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        // Отключаем физику (остановка)
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Отключаем атаку
        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        // Проигрываем анимацию смерти (если есть)
        if (anim != null) anim.SetTrigger("Die");

        // ЗВУК СМЕРТИ
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 1.0f);
        }

        // Запускаем последовательность Game Over
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 1. Ждем анимацию смерти (твоя логика)
        yield return new WaitForSeconds(waitBeforeFade);

        // 2. ПЛАВНОЕ затемнение (улучшение!)
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
            // Fallback: просто ждем
            yield return new WaitForSeconds(fadeDuration);
        }

        // 3. Загружаем ТВОЮ сцену смерти (гибко!)
        SceneManager.LoadScene(deathSceneName);
    }
}
