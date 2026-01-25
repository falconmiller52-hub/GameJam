using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    [Header("UI Settings")]
    public GameObject deathFadePanel; // Ссылка на панель затемнения (если есть)

    [Header("Audio")]
    public AudioClip deathSound; // Звук смерти игрока

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
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
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Отключаем атаку
        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.enabled = false;

        // Проигрываем анимацию смерти (если есть)
        var anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        // ЗВУК СМЕРТИ
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, 1.0f);
        }

        // Запускаем последовательность Game Over
        StartCoroutine(DeathSequence());
    }

    // ВОТ ЭТОТ МЕТОД, КОТОРОГО НЕ ХВАТАЛО:
    IEnumerator DeathSequence()
    {
        // 1. Ждем пару секунд, пока проиграется анимация смерти
        yield return new WaitForSeconds(2f);

        // 2. Включаем затемнение (если оно есть)
        if (deathFadePanel != null)
        {
            deathFadePanel.SetActive(true);
            yield return new WaitForSeconds(1f); // Ждем пока игрок осознает тщетность бытия
        }

        // 3. Загружаем сцену смерти или меню
        // Убедитесь, что сцена с таким именем добавлена в File -> Build Settings
        SceneManager.LoadScene("DeathDialogueScene"); 
    }
}
