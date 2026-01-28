using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int health = 5;
    
    // 🔥 ДОБАВЛЕНО: Публичное свойство, чтобы EnemyDamage мог проверить состояние
    public bool IsDead { get { return isDead; } }

    [Header("Audio")]
    public AudioClip deathSound; 

    [Header("Visual Feedback")]
    public Color damageColor = Color.red; 
    public float flashDuration = 0.1f;    

    [Header("Monster Feeding")]
    public float flyToMonsterSpeed = 8f;
    public float flyDelay = 0.5f;

    private Animator anim;
    private Collider2D col;
    private SpriteRenderer sr;             
    private Rigidbody2D rb;               
    private bool isDead = false;
    private Transform _monsterTarget;

    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>(); 
        rb = GetComponent<Rigidbody2D>(); 
        
        FindMonsterTarget();
    }

    void FindMonsterTarget()
    {
        GameObject monsterGO = GameObject.FindGameObjectWithTag("Monster");
        if (monsterGO != null)
        {
            _monsterTarget = monsterGO.transform;
            return;
        }
        
        if (MonsterEater.Instance != null)
        {
            _monsterTarget = MonsterEater.Instance.transform;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;
        
        if (sr != null && gameObject.activeInHierarchy) 
            StartCoroutine(FlashRed());

        StartCoroutine(ApplyKnockbackStun());

        if (health <= 0)
        {
            Die();
        }
    }

    IEnumerator ApplyKnockbackStun()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.enabled = false;
            yield return new WaitForSeconds(0.15f);
            if (!isDead) ai.enabled = true;
        }
    }

    IEnumerator FlashRed()
    {
        sr.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        sr.color = Color.white;
    }

    void Die()
    {
        isDead = true;

        // Отключаем коллайдер сразу, чтобы враг перестал толкаться МГНОВЕННО
        if (col != null) 
        {
            col.enabled = false; 
            StartCoroutine(ReenableColliderForMonster());
        }

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 5f;
            // Останавливаем врага, чтобы он не летел по инерции в игрока
            rb.linearVelocity = Vector2.zero; 
        }

        if (anim != null) anim.SetTrigger("Die");
        if (sr != null) sr.color = Color.white;

        if (deathSound != null)
        {
            // Создаем временный объект для звука, так как сам враг улетит
            GameObject soundObj = new GameObject("TempAudio");
            soundObj.transform.position = transform.position;
            AudioSource src = soundObj.AddComponent<AudioSource>();
            src.clip = deathSound;
            src.volume = 1.0f;
            src.spatialBlend = 0f;
            src.Play();
            Destroy(soundObj, deathSound.length);
        }

        if (_monsterTarget != null)
        {
            StartCoroutine(FlyToMonster());
        }
        else
        {
            StartCoroutine(DestroyAfterAnim());
        }
        
        var spawner = FindObjectOfType<WaveSpawner>();
        spawner?.EnemyDied();
    }

    // ✅ ГЛАВНОЕ ИСПРАВЛЕНИЕ ЗДЕСЬ
    IEnumerator ReenableColliderForMonster()
    {
        yield return new WaitForSeconds(0.1f);
        if (col != null) 
        {
            // 🔥 Делаем коллайдер ТРИГГЕРОМ!
            // Триггеры не имеют физических коллизий (сквозь них проходят),
            // но они ловятся событием OnTriggerEnter (у Монстра).
            col.isTrigger = true; 
            
            col.enabled = true;
        }
    }

    IEnumerator FlyToMonster()
    {
        yield return new WaitForSeconds(flyDelay);

        if (rb == null || _monsterTarget == null) yield break;

        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;

        float flyTime = 0f;
        
        while (flyTime < 3f) // Убрал проверку дистанции, пусть летит прямо в центр
        {
            if (rb == null || _monsterTarget == null) yield break;
            
            Vector2 monsterPos = (Vector2)_monsterTarget.position;
            Vector2 direction = (monsterPos - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * flyToMonsterSpeed;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.Euler(0, 0, Random.Range(-45f, 45f)), 
                Time.deltaTime * 3f);
            
            flyTime += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator DestroyAfterAnim()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
