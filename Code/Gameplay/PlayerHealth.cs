using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 10;
    public int currentHealth;

    void Start()
    {
        // При старте здоровье полное
        currentHealth = maxHealth;
        Debug.Log($"ИГРОК: Я родился! Мое здоровье: {currentHealth}");
    }

    // Этот метод будут вызывать враги
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"ИГРОК: Ай! Получен урон: {damage}. Осталось здоровья: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("ИГРОК: Я погиб... F");
        // Тут потом добавим рестарт уровня или экран смерти
        // Destroy(gameObject); // Пока не уничтожаем, чтобы консоль не пропала
    }
}
