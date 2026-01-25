using UnityEngine;
using System.Collections.Generic; // Нужно для списка

public class SwordDamage : MonoBehaviour
{
    public int damageAmount = 3;
    public float knockbackForce = 5f; // Сила отбрасывания

    // Список врагов, которых мы уже ударили за ЭТОТ замах.
    // Чтобы один взмах не наносил урон 60 раз в секунду одному и тому же врагу.
    private List<GameObject> hitEnemies = new List<GameObject>();

    // Вызывается автоматически, когда коллайдер включается (в начале удара)
    void OnEnable()
    {
        hitEnemies.Clear();
    }

    // Если мы выключаем коллайдер анимацией, список тоже лучше чистить
    void OnDisable() 
    {
        hitEnemies.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        
        // 2. Игнорируем триггеры
        if (other.isTrigger) return;
        // Проверяем, не били ли мы уже этого врага в этом замахе
        if (hitEnemies.Contains(other.gameObject)) return;

        // Ищем скрипт здоровья врага (у вас он может называться EnemyHealth или просто EnemyAI)
        // Допустим, у врага есть скрипт EnemyHealth.
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        
        if (enemy != null)
        {
            // Наносим урон
            enemy.TakeDamage(damageAmount);
            
            // Запоминаем, что этого парня мы уже ударили
            hitEnemies.Add(other.gameObject);

            // ОТБРАСЫВАНИЕ (Knockback)
            Rigidbody2D enemyRB = other.GetComponent<Rigidbody2D>();
            if (enemyRB != null)
            {
                // Вектор от меча к врагу
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                
                // Прикладываем импульс
                // ForceMode2D.Impulse - мгновенный толчок
                enemyRB.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }
}
