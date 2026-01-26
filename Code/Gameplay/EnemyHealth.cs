using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int health = 5;

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
            Debug.Log($"Monster найден для {gameObject.name}");
            return;
        }
        
        if (MonsterEater.Instance != null)
        {
            _monsterTarget = MonsterEater.Instance.transform;
            Debug.Log("MonsterEater singleton найден");
        }
        else
        {
            Debug.LogWarning($"Monster не найден для {gameObject.name}");
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

        // ✅ ВРЕМЕННО отключаем коллайдер (урон не спамится)
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
        }

        if (anim != null) anim.SetTrigger("Die");
        if (sr != null) sr.color = Color.white;

        if (deathSound != null)
        {
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

    // ✅ НОВЫЙ: Включаем коллайдер для MonsterEater через 0.1 сек
    IEnumerator ReenableColliderForMonster()
    {
        yield return new WaitForSeconds(0.1f);
        if (col != null) 
        {
            col.enabled = true;
            Debug.Log("Коллайдер включен для MonsterEater");
        }
    }

    IEnumerator FlyToMonster()
    {
        yield return new WaitForSeconds(flyDelay);

        if (rb == null || _monsterTarget == null) yield break;

        rb.gravityScale = 0f;
        rb.linearDamping = 2f;
        rb.angularDamping = 2f;  // ✅ ИСПРАВЛЕНО: angularDrag, не angularDamping

        float flyTime = 0f;
        Vector2 monsterPos = (Vector2)_monsterTarget.position;
        
        while (flyTime < 3f && Vector2.Distance(transform.position, monsterPos) > 0.5f)
        {
            if (rb == null || _monsterTarget == null) yield break;
            
            Vector2 direction = (monsterPos - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * flyToMonsterSpeed;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.Euler(0, 0, Random.Range(-45f, 45f)), 
                Time.deltaTime * 3f);
            
            flyTime += Time.deltaTime;
            yield return null;
        }
        
        Debug.Log("Враг долетел до Монстра!");
    }

    IEnumerator DestroyAfterAnim()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
