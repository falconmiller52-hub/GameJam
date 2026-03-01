using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damage = 1;
    private EnemyHealth myHealth; // Ссылка на свое здоровье

    void Start()
    {
        myHealth = GetComponent<EnemyHealth>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 🔥 ДОБАВЛЕНО: Если я мертв — я безобиден
        if (myHealth != null && myHealth.IsDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            // 📊 АНАЛИТИКА: запоминаем тип врага перед нанесением урона
            if (GameAnalyticsManager.Instance != null)
            {
                string enemyType = GetEnemyType();
                GameAnalyticsManager.Instance.SetLastDamageSource(enemyType);
            }

            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    /// <summary>
    /// Определяет тип врага по компонентам на объекте
    /// </summary>
    string GetEnemyType()
    {
        if (GetComponent<EnemyJumpAttack>() != null) return "jumper";
        if (GetComponent<EnemyDash>() != null) return "dasher";
        if (GetComponent<EnemyRangedAI>() != null) return "ranged";
        return "basic_melee";
    }
    
    // То же самое для OnCollisionStay, если ты решишь его использовать
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (myHealth != null && myHealth.IsDead) return;
        // логика периодического урона...
    }
}
