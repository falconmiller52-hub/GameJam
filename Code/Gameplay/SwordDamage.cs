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
    // Игнорируем игрока и границы камеры
    if (other.CompareTag("Player") || other.name.Contains("CameraBounds")) return;
    
    // Игнорируем триггеры
    if (other.isTrigger) return;
    
    // Проверяем, не били ли уже этого врага
    if (hitEnemies.Contains(other.gameObject)) return;

    // Универсальный поиск компонента здоровья
    EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
    
    Debug.Log("Найден EnemyHealth на " + other.name + ": " + (enemyHealth != null));

    if (enemyHealth != null)
    {
        Debug.Log("Наносим " + damageAmount + " урона врагу " + other.name);
        
        // Наносим урон
        enemyHealth.TakeDamage(damageAmount);
        
        // Запоминаем для защиты от спама
        hitEnemies.Add(other.gameObject);
        
        // Knockback
        Rigidbody2D enemyRB = other.GetComponent<Rigidbody2D>();
        if (enemyRB != null)
        {
            Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
            enemyRB.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
        }
    }
    else
    {
        Debug.LogWarning("На объекте " + other.name + " НЕТ компонента EnemyHealth!");
    }
}

}
