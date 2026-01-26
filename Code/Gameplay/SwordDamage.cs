using UnityEngine;
using System.Collections.Generic;

public class SwordDamage : MonoBehaviour
{
    public int damageAmount = 3;
    
    [Header("Physics")]
    public float knockbackForce = 10f; // Увеличьте это значение (было 5, стало 10)

    private List<GameObject> hitEnemies = new List<GameObject>();

    public void ResetAttack()
    {
        hitEnemies.Clear();
    }

    void OnEnable() { hitEnemies.Clear(); }
    void OnDisable() { hitEnemies.Clear(); }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.name.Contains("CameraBounds")) return;
        if (other.isTrigger) return;
        if (hitEnemies.Contains(other.gameObject)) return;

        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        
        if (enemyHealth != null)
        {
            // 1. Урон
            enemyHealth.TakeDamage(damageAmount);
            hitEnemies.Add(other.gameObject);
            
            // 2. ОТБРАСЫВАНИЕ (Knockback)
            Rigidbody2D enemyRB = other.GetComponent<Rigidbody2D>();
            if (enemyRB != null)
            {
                // Фишка: Сначала останавливаем врага (Reset velocity)
                enemyRB.linearVelocity = Vector2.zero;
                
                // Вычисляем направление от Игрока (или меча) к Врагу
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                
                // Прикладываем импульс
                enemyRB.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
                
                // Опционально: Можно временно отключить AI врага на 0.2 сек, 
                // чтобы он не сопротивлялся полету. Но это уже сложнее.
            }
        }
    }
}
