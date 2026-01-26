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

    private Animator anim;
    private Collider2D col;
    private SpriteRenderer sr;            
    private Rigidbody2D rb;               
    private bool isDead = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>(); 
        rb = GetComponent<Rigidbody2D>(); 
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;
        
        if (sr != null && gameObject.activeInHierarchy) StartCoroutine(FlashRed());

        // ВАЖНО: Даем пинку сработать, отключая мозг врага
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
            // Выключаем движение, чтобы физика толчка сработала
            ai.enabled = false;
            // Ждем немного (время полета)
            yield return new WaitForSeconds(0.15f);
            // Если не умер — включаем обратно
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

        if (col != null) col.enabled = false;

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        if (rb != null)
        {
            rb.gravityScale = 0f; 
            // rb.linearDamping = 5f; // Unity 6
             rb.linearDamping = 5f; // Unity 2022/2021
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

        StartCoroutine(DestroyAfterAnim());
    }

    IEnumerator DestroyAfterAnim()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
