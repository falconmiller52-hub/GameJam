using UnityEngine;
using System.Collections;

/// <summary>
/// FIXED:
/// - FlyToMonster now always Destroy()s the enemy after timeout (prevents wave from getting stuck)
/// - EnemyDied() is cached to avoid repeated FindObjectOfType calls
/// - Added safety Destroy after max lifetime
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public int health = 5;
    public bool IsDead { get { return isDead; } }

    [Header("Audio")]
    public AudioClip deathSound;

    [Header("Visual Feedback")]
    public Color damageColor = Color.red;
    public float flashDuration = 0.1f;

    [Header("Monster Feeding")]
    public float flyToMonsterSpeed = 8f;
    public float flyDelay = 0.5f;

    [Header("Safety")]
    [Tooltip("Max time enemy can exist after death before forced destroy")]
    public float maxDeathLifetime = 5f;

    private Animator anim;
    private Collider2D col;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private bool isDead = false;
    private Transform _monsterTarget;
    private static WaveSpawner _cachedSpawner;

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
        if (monsterGO != null) { _monsterTarget = monsterGO.transform; return; }
        if (MonsterEater.Instance != null) _monsterTarget = MonsterEater.Instance.transform;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, false);
    }

    /// <summary>
    /// Расширенная версия — поддерживает отображение сниженного урона
    /// </summary>
    public void TakeDamage(int damage, bool isReducedDamage)
    {
        if (isDead) return;
        health -= damage;
        
        // 🔥 Всплывающий урон
        DamagePopup.Create(transform.position, damage, isReducedDamage);
        
        if (sr != null && gameObject.activeInHierarchy) StartCoroutine(FlashRed());
        StartCoroutine(ApplyKnockbackStun());
        if (health <= 0) Die();
    }

    IEnumerator ApplyKnockbackStun()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null) { ai.enabled = false; yield return new WaitForSeconds(0.15f); if (!isDead) ai.enabled = true; }
    }

    IEnumerator FlashRed()
    {
        if (sr == null) yield break;
        sr.color = damageColor;
        yield return new WaitForSeconds(flashDuration);
        if (sr != null) sr.color = Color.white;
    }

    void Die()
    {
        isDead = true;

        if (col != null) { col.enabled = false; StartCoroutine(ReenableColliderForMonster()); }

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        // 🔥 СНАЧАЛА прерываем все атаки (они могут сбрасывать триггеры!)
        // Disable jump attack if present
        var jumpAttack = GetComponent<EnemyJumpAttack>();
        if (jumpAttack != null) { jumpAttack.InterruptJump(); jumpAttack.enabled = false; }

        // 🔥 Disable dash attack if present
        var dashAttack = GetComponent<EnemyDash>();
        if (dashAttack != null) { dashAttack.InterruptDash(); dashAttack.enabled = false; }

        // 🔥 Disable ranged AI if present
        var rangedAI = GetComponent<EnemyRangedAI>();
        if (rangedAI != null) { rangedAI.InterruptAction(); rangedAI.enabled = false; }

        if (rb != null) { rb.gravityScale = 0f; rb.linearDamping = 5f; rb.linearVelocity = Vector2.zero; }
        if (sr != null) sr.color = Color.white;

        // 🔥 ПОТОМ ставим триггер Die — после того как все ResetTrigger уже отработали
        if (anim != null)
        {
            // Сбрасываем все возможные триггеры, чтобы Die точно сработал
            anim.ResetTrigger("Die");
            anim.SetTrigger("Die");
        }

        if (deathSound != null)
        {
            GameObject soundObj = new GameObject("TempAudio");
            soundObj.transform.position = transform.position;
            AudioSource src = soundObj.AddComponent<AudioSource>();
            src.clip = deathSound; src.volume = 1.0f; src.spatialBlend = 0f; src.Play();
            Destroy(soundObj, deathSound.length);
        }

        // Notify WaveSpawner
        if (_cachedSpawner == null) _cachedSpawner = FindObjectOfType<WaveSpawner>();
        if (_cachedSpawner != null) _cachedSpawner.EnemyDied();

        if (_monsterTarget != null)
            StartCoroutine(FlyToMonster());
        else
            StartCoroutine(DestroyAfterAnim());

        // 🔥 SAFETY: guaranteed destroy after maxDeathLifetime
        Destroy(gameObject, maxDeathLifetime);
    }

    IEnumerator ReenableColliderForMonster()
    {
        yield return new WaitForSeconds(0.1f);
        if (col != null) { col.isTrigger = true; col.enabled = true; }
    }

    IEnumerator FlyToMonster()
    {
        yield return new WaitForSeconds(flyDelay);
        if (rb == null || _monsterTarget == null) { Destroy(gameObject); yield break; }

        rb.gravityScale = 0f; rb.linearDamping = 2f; rb.angularDamping = 2f;

        float flyTime = 0f;
        while (flyTime < 3f)
        {
            if (rb == null || _monsterTarget == null) break;
            Vector2 dir = ((Vector2)_monsterTarget.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dir * flyToMonsterSpeed;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, Random.Range(-45f, 45f)), Time.deltaTime * 3f);
            flyTime += Time.deltaTime;
            yield return null;
        }

        // 🔥 FIXED: Always destroy after fly, even if MonsterEater didn't catch it
        if (gameObject != null) Destroy(gameObject);
    }

    IEnumerator DestroyAfterAnim()
    {
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
