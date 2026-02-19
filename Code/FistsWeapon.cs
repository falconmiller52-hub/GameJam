using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Оружие "Кулаки" — ЛКМ левый удар, ПКМ правый удар.
/// Каждый кулак имеет:
///   - Animator с 3-кадровой анимацией удара
///   - Движение по X при ударе (через код)
///   - Коллайдер включается только во время удара (через код)
///   
/// АНИМАЦИЯ: Можно делать включение/выключение коллайдеров через Animation Events
/// в клипе, НО рекомендуется оставить управление через код — так надёжнее и проще
/// контролировать тайминги. Если хочешь через клип — просто убери строки
/// col.enabled = true/false и добавь в клип Events, вызывающие EnableCollider()/DisableCollider().
/// </summary>
public class FistsWeapon : MonoBehaviour
{
    [Header("=== УРОН ===")]
    public int damage = 4;
    public float knockbackForce = 12f;

    [Header("=== КУЛДАУНЫ ===")]
    public float leftAttackCooldown = 0.4f;
    public float rightAttackCooldown = 0.4f;
    public float globalCooldown = 0.2f;

    [Header("=== ДВИЖЕНИЕ КУЛАКА ПО X ===")]
    [Tooltip("Расстояние выброса кулака вперёд")]
    public float punchDistance = 0.5f;
    [Tooltip("Время выброса")]
    public float punchOutTime = 0.08f;
    [Tooltip("Время удержания (хитбокс активен)")]
    public float punchHoldTime = 0.12f;
    [Tooltip("Время возврата")]
    public float punchReturnTime = 0.15f;

    [Header("=== ОБЪЕКТЫ КУЛАКОВ ===")]
    [Tooltip("Transform левого кулака (со спрайтом)")]
    public Transform leftFist;
    [Tooltip("Transform правого кулака (со спрайтом)")]
    public Transform rightFist;

    [Header("=== АНИМАТОРЫ КУЛАКОВ ===")]
    [Tooltip("Animator левого кулака (3-кадровая анимация)")]
    public Animator leftFistAnimator;
    [Tooltip("Animator правого кулака")]
    public Animator rightFistAnimator;
    [Tooltip("Имя триггера анимации удара")]
    public string punchTrigger = "Punch";

    [Header("=== КОЛЛАЙДЕРЫ ===")]
    public Collider2D leftFistCollider;
    public Collider2D rightFistCollider;

    [Header("=== АУДИО ===")]
    public AudioClip leftPunchSound;
    public AudioClip rightPunchSound;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float punchVolume = 0.7f;
    [Range(0f, 1f)] public float hitVolume = 0.8f;

    [Header("=== ВИЗУАЛ ===")]
    public GameObject hitEffectPrefab;

    // Приватные
    private AudioSource audioSource;
    private float lastLeftAttackTime = -999f;
    private float lastRightAttackTime = -999f;
    private float lastAnyAttackTime = -999f;
    private bool isAttacking = false;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private WeaponSwitcher weaponSwitcher;

    private Vector3 leftFistStartPos;
    private Vector3 rightFistStartPos;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        weaponSwitcher = GetComponentInParent<WeaponSwitcher>();
        if (weaponSwitcher == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) weaponSwitcher = p.GetComponent<WeaponSwitcher>();
        }

        if (leftFist != null) leftFistStartPos = leftFist.localPosition;
        if (rightFist != null) rightFistStartPos = rightFist.localPosition;

        // Автопоиск аниматоров
        if (leftFistAnimator == null && leftFist != null)
            leftFistAnimator = leftFist.GetComponent<Animator>();
        if (rightFistAnimator == null && rightFist != null)
            rightFistAnimator = rightFist.GetComponent<Animator>();

        DisableColliders();
    }

    void Update()
    {
        if (weaponSwitcher != null && !weaponSwitcher.IsFistsActive()) return;
        if (PauseMenu.isPaused) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && CanAttackLeft())
            StartCoroutine(PunchRoutine(isLeft: true));

        if (Mouse.current.rightButton.wasPressedThisFrame && CanAttackRight())
            StartCoroutine(PunchRoutine(isLeft: false));
    }

    bool CanAttackLeft()
    {
        return !isAttacking &&
               Time.time >= lastAnyAttackTime + globalCooldown &&
               Time.time >= lastLeftAttackTime + leftAttackCooldown;
    }

    bool CanAttackRight()
    {
        return !isAttacking &&
               Time.time >= lastAnyAttackTime + globalCooldown &&
               Time.time >= lastRightAttackTime + rightAttackCooldown;
    }

    IEnumerator PunchRoutine(bool isLeft)
    {
        isAttacking = true;
        hitEnemies.Clear();

        if (isLeft) { lastLeftAttackTime = Time.time; }
        else { lastRightAttackTime = Time.time; }
        lastAnyAttackTime = Time.time;

        Transform fist = isLeft ? leftFist : rightFist;
        Collider2D col = isLeft ? leftFistCollider : rightFistCollider;
        AudioClip sound = isLeft ? leftPunchSound : rightPunchSound;
        Animator anim = isLeft ? leftFistAnimator : rightFistAnimator;
        Vector3 startPos = isLeft ? leftFistStartPos : rightFistStartPos;

        // 1. Звук
        if (sound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(sound, punchVolume);
        }

        // 2. Анимация (3 кадра) — запускаем сразу
        if (anim != null)
        {
            anim.ResetTrigger(punchTrigger);
            anim.SetTrigger(punchTrigger);
        }

        // 3. Движение по X + хитбокс
        if (fist != null)
        {
            Vector3 targetPos = startPos + Vector3.right * punchDistance;

            // ФАЗА 1: Выброс вперёд
            float elapsed = 0f;
            while (elapsed < punchOutTime)
            {
                float t = elapsed / punchOutTime;
                fist.localPosition = Vector3.Lerp(startPos, targetPos, EaseOutCubic(t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            fist.localPosition = targetPos;

            // ФАЗА 2: Удержание — хитбокс включён
            if (col != null) col.enabled = true;
            yield return new WaitForSeconds(punchHoldTime);
            if (col != null) col.enabled = false;

            // ФАЗА 3: Возврат
            elapsed = 0f;
            while (elapsed < punchReturnTime)
            {
                float t = elapsed / punchReturnTime;
                fist.localPosition = Vector3.Lerp(targetPos, startPos, EaseInCubic(t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            fist.localPosition = startPos;
        }
        else
        {
            // Фоллбэк без Transform
            if (col != null) col.enabled = true;
            yield return new WaitForSeconds(punchHoldTime);
            if (col != null) col.enabled = false;
            yield return new WaitForSeconds(punchReturnTime);
        }

        isAttacking = false;
    }

    float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseInCubic(float t) => t * t * t;

    // ==================== ПОПАДАНИЕ ====================

    public void OnFistHit(Collider2D other)
    {
        if (other.CompareTag("Player")) return;
        if (other.isTrigger) return;
        if (hitEnemies.Contains(other.gameObject)) return;

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null && !eh.IsDead)
        {
            eh.TakeDamage(damage);
            hitEnemies.Add(other.gameObject);

            if (hitSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(hitSound, hitVolume);
            }

            Rigidbody2D enemyRb = other.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero;
                Vector2 dir = (other.transform.position - transform.position).normalized;
                enemyRb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }

            if (hitEffectPrefab != null)
            {
                GameObject eff = Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity);
                Destroy(eff, 1f);
            }
        }
    }

    // ==================== МЕТОДЫ ДЛЯ ANIMATION EVENTS ====================
    // Если хочешь управлять коллайдерами через анимационный клип:
    //   - Добавь Animation Event на нужный кадр
    //   - Вызови EnableLeftCollider() / DisableLeftCollider() и т.д.

    public void EnableLeftCollider() { if (leftFistCollider != null) leftFistCollider.enabled = true; }
    public void DisableLeftCollider() { if (leftFistCollider != null) leftFistCollider.enabled = false; }
    public void EnableRightCollider() { if (rightFistCollider != null) rightFistCollider.enabled = true; }
    public void DisableRightCollider() { if (rightFistCollider != null) rightFistCollider.enabled = false; }

    // ==================== УТИЛИТЫ ====================

    void DisableColliders()
    {
        if (leftFistCollider != null) leftFistCollider.enabled = false;
        if (rightFistCollider != null) rightFistCollider.enabled = false;
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
        if (leftFist != null) leftFist.localPosition = leftFistStartPos;
        if (rightFist != null) rightFist.localPosition = rightFistStartPos;
    }
}

/// <summary>
/// Хелпер для коллайдера кулака. Добавь на каждый дочерний объект с коллайдером.
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
