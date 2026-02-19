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
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }
    
    // То же самое для OnCollisionStay, если ты решишь его использовать
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (myHealth != null && myHealth.IsDead) return;
        // логика периодического урона...
    }
}
