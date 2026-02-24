using UnityEngine; // Эта строка обязательна!

public class EnemyAI : MonoBehaviour
{
    public Transform playerTarget;
    public float speed = 2f;
    private Rigidbody2D rb;
    private SpriteRenderer sr; // 🔥 Кэшируем вместо вызова GetComponent каждый кадр

    private float flipDelay = 0.1f;
    private float spawnTime;
    private bool hasFlipped = false;

void Start()
{
    rb = GetComponent<Rigidbody2D>();
    sr = GetComponent<SpriteRenderer>(); // 🔥 Один раз при старте
    
    spawnTime = Time.time; 

    if (playerTarget == null)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTarget = player.transform;
    }
}

    void FixedUpdate()
    {
        if (playerTarget == null) return;

        Vector2 newPos = Vector2.MoveTowards(rb.position, playerTarget.position, speed * Time.fixedDeltaTime);
        rb.MovePosition(newPos);

        if (!hasFlipped && Time.time > spawnTime + flipDelay)
        {
            hasFlipped = true;
        }

if (hasFlipped)
        {
            if (sr != null)
            {
                sr.flipX = playerTarget.position.x > transform.position.x;
            }
        }
    }
}
