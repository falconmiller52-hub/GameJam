using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 10;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log($"Враг получил {damage} урона! Осталось: {health}");
        
        // Эффект мигания или крови можно добавить сюда

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Враг повержен!");
        Destroy(gameObject);
    }
}
