using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int health = 5;

    [Header("Audio")]
    public AudioClip deathSound; // ← ЗВУК СМЕРТИ ВРАГА (перетащите сюда .wav файл)

    private Animator anim;
    private Collider2D col;
    private bool isDead = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;
        Debug.Log(gameObject.name + " получил " + damage + " урона. Осталось HP: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        // 1. Отключаем коллайдер
        if (col != null) col.enabled = false;

        // 2. Отключаем AI (ИСПРАВЛЕНО!)
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        // 3. Останавливаем физику
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 4. АНИМАЦИЯ СМЕРТИ
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        // 5. ЗВУК СМЕРТИ ← ДОБАВЛЕНО!
        if (deathSound != null)
        {
AudioSource.PlayClipAtPoint(deathSound ?? Resources.Load<AudioClip>("Default"), transform.position, 2.0f);;
        }

        // 6. Уничтожаем объект
        StartCoroutine(DestroyAfterAnim());
// 5. ЗВУК СМЕРТИ (2D FIX)
if (deathSound != null)
{
    Debug.Log("🎵 Воспроизводим звук смерти (2D): " + deathSound.name);
    
    // Создаем пустой объект
    GameObject soundObj = new GameObject("TempAudio");
    soundObj.transform.position = transform.position;
    
    // Добавляем AudioSource
    AudioSource src = soundObj.AddComponent<AudioSource>();
    src.clip = deathSound;
    src.volume = 1.0f;
    src.spatialBlend = 0f; // <--- ВАЖНО! 0 = 2D Звук (слышно везде)
    src.Play();
    
    // Уничтожаем объект после окончания звука
    Destroy(soundObj, deathSound.length);
}

    }

    IEnumerator DestroyAfterAnim()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
    
}
