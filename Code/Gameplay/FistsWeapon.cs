using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Оружие "Кулаки" — двойная атака с ЛКМ и ПКМ.
/// Больший урон чем катана, но требует близкой дистанции.
/// </summary>
public class FistsWeapon : MonoBehaviour
{
    [Header("=== УРОН ===")]
    [Tooltip("Урон за удар (больше чем катана)")]
    public int damage = 4;
    
    [Tooltip("Сила отталкивания")]
    public float knockbackForce = 12f;

    [Header("=== СКОРОСТЬ АТАКИ ===")]
    [Tooltip("Кулдаун левого удара")]
    public float leftAttackCooldown = 0.4f;
    
    [Tooltip("Кулдаун правого удара")]
    public float rightAttackCooldown = 0.4f;
    
    [Tooltip("Общий кулдаун между любыми ударами")]
    public float globalCooldown = 0.2f;

    [Header("=== КОЛЛАЙДЕРЫ ===")]
    [Tooltip("Коллайдер левого кулака")]
    public Collider2D leftFistCollider;
    
    [Tooltip("Коллайдер правого кулака")]
    public Collider2D rightFistCollider;

    [Header("=== АНИМАЦИЯ ===")]
    public Animator animator;
    
    [Tooltip("Триггер левого удара")]
    public string leftAttackTrigger = "LeftPunch";
    
    [Tooltip("Триггер правого удара")]
    public string rightAttackTrigger = "RightPunch";

    [Header("=== АУДИО ===")]
    public AudioClip leftPunchSound;
    public AudioClip rightPunchSound;
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float punchVolume = 0.7f;
    [Range(0f, 1f)]
    public float hitVolume = 0.8f;

    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Эффект удара")]
    public GameObject hitEffectPrefab;

    // Приватные переменные
    private AudioSource audioSource;
    private float lastLeftAttackTime = -999f;
    private float lastRightAttackTime = -999f;
    private float lastAnyAttackTime = -999f;
    private bool isAttacking = false;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private WeaponSwitcher weaponSwitcher;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (animator == null)
            animator = GetComponent<Animator>();

        weaponSwitcher = GetComponentInParent<WeaponSwitcher>();

        // Выключаем коллайдеры изначально
        DisableColliders();
    }

    void Update()
    {
        // Проверяем, активны ли кулаки
        if (weaponSwitcher != null && !weaponSwitcher.IsFistsActive())
            return;

        // Проверяем паузу
        if (PauseMenu.isPaused) return;

        // Левый клик — левый удар
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (CanAttackLeft())
            {
                StartCoroutine(LeftPunch());
            }
        }

        // Правый клик — правый удар
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (CanAttackRight())
            {
                StartCoroutine(RightPunch());
            }
        }
    }

    bool CanAttackLeft()
    {
        if (isAttacking) return false;
        if (Time.time < lastAnyAttackTime + globalCooldown) return false;
        if (Time.time < lastLeftAttackTime + leftAttackCooldown) return false;
        return true;
    }

    bool CanAttackRight()
    {
        if (isAttacking) return false;
        if (Time.time < lastAnyAttackTime + globalCooldown) return false;
        if (Time.time < lastRightAttackTime + rightAttackCooldown) return false;
        return true;
    }

    IEnumerator LeftPunch()
    {
        isAttacking = true;
        lastLeftAttackTime = Time.time;
        lastAnyAttackTime = Time.time;
        hitEnemies.Clear();

        Debug.Log("[FistsWeapon] ЛЕВЫЙ УДАР!");

        // Анимация
        if (animator != null)
            animator.SetTrigger(leftAttackTrigger);

        // Звук
        if (leftPunchSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(leftPunchSound, punchVolume);
        }

        // Включаем коллайдер
        if (leftFistCollider != null)
            leftFistCollider.enabled = true;

        // Ждём активную фазу удара
        yield return new WaitForSeconds(0.1f);

        // Выключаем коллайдер
        if (leftFistCollider != null)
            leftFistCollider.enabled = false;

        // Ждём окончание анимации
        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
    }

    IEnumerator RightPunch()
    {
        isAttacking = true;
        lastRightAttackTime = Time.time;
        lastAnyAttackTime = Time.time;
        hitEnemies.Clear();

        Debug.Log("[FistsWeapon] ПРАВЫЙ УДАР!");

        // Анимация
        if (animator != null)
            animator.SetTrigger(rightAttackTrigger);

        // Звук
        if (rightPunchSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(rightPunchSound, punchVolume);
        }

        // Включаем коллайдер
        if (rightFistCollider != null)
            rightFistCollider.enabled = true;

        // Ждём активную фазу удара
        yield return new WaitForSeconds(0.1f);

        // Выключаем коллайдер
        if (rightFistCollider != null)
            rightFistCollider.enabled = false;

        // Ждём окончание анимации
        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
    }

    void DisableColliders()
    {
        if (leftFistCollider != null)
            leftFistCollider.enabled = false;
        if (rightFistCollider != null)
            rightFistCollider.enabled = false;
    }

    // Вызывается из дочерних объектов (коллайдеров кулаков)
    public void OnFistHit(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        if (other.isTrigger) return;
        if (hitEnemies.Contains(other.gameObject)) return;

        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null && !enemyHealth.IsDead)
        {
            // Урон
            enemyHealth.TakeDamage(damage);
            hitEnemies.Add(other.gameObject);

            // Звук попадания
            if (hitSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(hitSound, hitVolume);
            }

            // Отбрасывание
            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero;
                Vector2 knockbackDir = (other.transform.position - transform.position).normalized;
                enemyRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
            }

            // Эффект
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }

            Debug.Log($"[FistsWeapon] Попадание! Урон: {damage}");
        }
    }

    void OnEnable()
    {
        DisableColliders();
        hitEnemies.Clear();
    }

    void OnDisable()
    {
        DisableColliders();
        isAttacking = false;
    }
}

/// <summary>
/// Хелпер-скрипт для коллайдера кулака.
/// Добавь на каждый коллайдер (левый/правый кулак).
/// </summary>
public class FistColliderHelper : MonoBehaviour
{
    public FistsWeapon fistsWeapon;

    void Start()
    {
        if (fistsWeapon == null)
            fistsWeapon = GetComponentInParent<FistsWeapon>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (fistsWeapon != null)
            fistsWeapon.OnFistHit(other);
    }
}
