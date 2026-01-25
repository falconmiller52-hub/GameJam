using UnityEngine; // Эта строка обязательна!

public class EnemyAI : MonoBehaviour
{
    public Transform playerTarget;
    public float speed = 2f;
    private Rigidbody2D rb;

    private float flipDelay = 0.1f;
    private float spawnTime; // Время рождения
    private bool hasFlipped = false;

void Start()
{
    rb = GetComponent<Rigidbody2D>();
    
    // !!! ВОТ ЭТА СТРОКА КРИТИЧЕСКИ ВАЖНА !!!
    spawnTime = Time.time; 
    // Без неё задержка не работает!

    if (playerTarget == null)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;
    }
}

    void FixedUpdate()
    {
        if (playerTarget == null) return;

        // Двигаем врага к игроку
        Vector2 newPos = Vector2.MoveTowards(rb.position, playerTarget.position, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        // Ждем 0.1 сек после спавна перед тем как разрешить поворот спрайта
        if (!hasFlipped && Time.time > spawnTime + flipDelay)
        {
            hasFlipped = true;
        }

if (hasFlipped)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Поменяли знак < на >. Теперь логика зеркальная.
                // Попробуйте этот вариант, если враг смотрит "жопой" к игроку.
                sr.flipX = playerTarget.position.x > transform.position.x;
            }
        }
    }
}
