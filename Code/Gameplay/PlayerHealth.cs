using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;
    
    // Ссылка на панель затемнения в Canvas уровня (если есть)
    // Если нет, можно сделать просто резкий переход
    public GameObject deathFadePanel; 

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        // Отключаем управление, чтобы игрок не дергался
        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // 1. Пауза ("повиснуть на пару секунд")
        // Можно использовать Time.timeScale = 0, но тогда корутины остановятся.
        // Лучше просто подождать в реальном времени.
        yield return new WaitForSeconds(2f);

        // 2. Затемнение (если панель назначена)
        if (deathFadePanel != null)
        {
            deathFadePanel.SetActive(true);
            // Тут можно добавить плавное появление alpha от 0 до 1, но для простоты включим сразу
            yield return new WaitForSeconds(1f); 
        }

        // 3. Загрузка сцены диалога
        SceneManager.LoadScene("DeathDialogueScene");
    }
}
