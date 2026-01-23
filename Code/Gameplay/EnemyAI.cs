using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 2f; // Скорость врага (медленнее игрока)
    public int damage = 1;   // Сила укуса

    private Transform playerTarget;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Враг сам ищет игрока по тегу, который мы поставили в Шаге 1
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
        else
        {
            Debug.LogError("ВРАГ: Не могу найти игрока! Вы забыли поставить тег 'Player'?");
        }
    }

    void FixedUpdate()
    {
        // Если игрока нет (убит) — стоим на месте
        if (playerTarget == null) return;

        // 1. Движение к игроку
        // MoveTowards плавно меняет позицию от текущей к цели
        Vector2 newPos = Vector2.MoveTowards(rb.position, playerTarget.position, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);
    }

    // Обработка столкновений
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, врезались ли мы именно в игрока
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("ВРАГ: Кусь!");
            
            // Получаем скрипт здоровья с объекта, в который врезались
            PlayerHealth healthScript = collision.gameObject.GetComponent<PlayerHealth>();
            
            if (healthScript != null)
            {
                healthScript.TakeDamage(damage);
            }
        }
    }
}
