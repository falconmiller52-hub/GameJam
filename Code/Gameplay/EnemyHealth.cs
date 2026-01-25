using UnityEngine;
using System.Collections; // Нужно для корутины

public class EnemyHealth : MonoBehaviour
{
    public int health = 5;
    private Animator anim;
    private Collider2D col; // Чтобы отключить столкновения у трупа
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
        // Можно добавить анимацию получения урона (Hit), если есть
        
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;

        // 1. Отключаем физику, чтобы сквозь труп можно было ходить
        if (col != null) col.enabled = false;
        
        // 2. Останавливаем движение (если есть скрипт AI)
        var ai = GetComponent<EnemyAI>(); // Или как он у вас называется
        if (ai != null) ai.enabled = false;
        
        // Чтобы труп не толкался, можно убрать Rigidbody velocity
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 3. Запускаем анимацию
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        // 4. Ждем конца анимации и удаляем
        StartCoroutine(DestroyAfterAnim());
    }

    IEnumerator DestroyAfterAnim()
    {
        // Ждем длительность анимации смерти (например, 0.5 - 1 сек)
        // Можно получить точную длину из клипа, но проще хардкодом для начала
        yield return new WaitForSeconds(1f); 
        
        Destroy(gameObject);
    }
}
