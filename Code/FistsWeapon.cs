using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ИСПРАВЛЕНО: Перчатки больше не накладываются друг на друга.
/// Добавлены leftFistOffset/rightFistOffset — если localPosition обеих (0,0),
/// скрипт сам разводит их по X.
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
    public float punchDistance = 0.5f;
    public float punchOutTime = 0.08f;
    public float punchHoldTime = 0.12f;
    public float punchReturnTime = 0.15f;

    [Header("=== ОБЪЕКТЫ КУЛАКОВ ===")]
    public Transform leftFist;
    public Transform rightFist;

    [Header("=== ПОЗИЦИИ КУЛАКОВ ===")]
    [Tooltip("Смещение левого кулака от центра (если обе перчатки на 0,0)")]
    public Vector3 leftFistOffset = new Vector3(-0.3f, 0.1f, 0f);
    [Tooltip("Смещение правого кулака от центра")]
    public Vector3 rightFistOffset = new Vector3(0.3f, -0.1f, 0f);
    [Tooltip("Применить смещения при старте? (если перчатки на 0,0)")]
    public bool autoPositionFists = true;

    [Header("=== АНИМАТОРЫ КУЛАКОВ ===")]
    public Animator leftFistAnimator;
    public Animator rightFistAnimator;
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
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        weaponSwitcher = GetComponentInParent<WeaponSwitcher>();
        if (weaponSwitcher == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) weaponSwitcher = p.GetComponent<WeaponSwitcher>();
        }

        // 🔥 Разводим перчатки если они обе на (0,0)
        if (autoPositionFists && leftFist != null && rightFist != null)
        {
            float dist = Vector3.Distance(leftFist.localPosition, rightFist.localPosition);
            if (dist < 0.01f) // Обе на одной позиции
            {
                leftFist.localPosition = leftFistOffset;
                rightFist.localPosition = rightFistOffset;
                Debug.Log($"[FistsWeapon] Перчатки разведены: L={leftFistOffset}, R={rightFistOffset}");
            }
        }

        if (leftFist != null) leftFistStartPos = leftFist.localPosition;
        if (rightFist != null) rightFistStartPos = rightFist.localPosition;

        if (leftFistAnimator == null && leftFist != null) leftFistAnimator = leftFist.GetComponent<Animator>();
        if (rightFistAnimator == null && rightFist != null) rightFistAnimator = rightFist.GetComponent<Animator>();

        EnsureColliderHelper(leftFist, leftFistCollider);
        EnsureColliderHelper(rightFist, rightFistCollider);
        DisableColliders();
    }

    void EnsureColliderHelper(Transform fist, Collider2D col)
    {
        if (fist == null) return;
        FistColliderHelper h = fist.GetComponent<FistColliderHelper>();
        if (h == null) { h = fist.gameObject.AddComponent<FistColliderHelper>(); }
        h.fistsWeapon = this;

        if (col != null && col.gameObject != fist.gameObject)
        {
            FistColliderHelper ch = col.GetComponent<FistColliderHelper>();
            if (ch == null) { ch = col.gameObject.AddComponent<FistColliderHelper>(); }
            ch.fistsWeapon = this;
        }
    }

    void Update()
    {
        if (weaponSwitcher != null && !weaponSwitcher.IsFistsActive()) return;
        if (PauseMenu.isPaused) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && CanAttackLeft())
            StartCoroutine(PunchRoutine(true));
        if (Mouse.current.rightButton.wasPressedThisFrame && CanAttackRight())
            StartCoroutine(PunchRoutine(false));
    }

    bool CanAttackLeft() => !isAttacking && Time.time >= lastAnyAttackTime + globalCooldown && Time.time >= lastLeftAttackTime + leftAttackCooldown;
    bool CanAttackRight() => !isAttacking && Time.time >= lastAnyAttackTime + globalCooldown && Time.time >= lastRightAttackTime + rightAttackCooldown;

    IEnumerator PunchRoutine(bool isLeft)
    {
        isAttacking = true;
        hitEnemies.Clear();

        if (isLeft) lastLeftAttackTime = Time.time; else lastRightAttackTime = Time.time;
        lastAnyAttackTime = Time.time;

        Transform fist = isLeft ? leftFist : rightFist;
        Collider2D col = isLeft ? leftFistCollider : rightFistCollider;
        AudioClip sound = isLeft ? leftPunchSound : rightPunchSound;
        Animator anim = isLeft ? leftFistAnimator : rightFistAnimator;
        Vector3 startPos = isLeft ? leftFistStartPos : rightFistStartPos;

        if (sound != null && audioSource != null)
        { audioSource.pitch = Random.Range(0.9f, 1.1f); audioSource.PlayOneShot(sound, punchVolume); }

        if (anim != null) { anim.ResetTrigger(punchTrigger); anim.SetTrigger(punchTrigger); }

        if (fist != null)
        {
            Vector3 targetPos = startPos + Vector3.right * punchDistance;
            float elapsed = 0f;
            while (elapsed < punchOutTime)
            { fist.localPosition = Vector3.Lerp(startPos, targetPos, EaseOut(elapsed / punchOutTime)); elapsed += Time.deltaTime; yield return null; }
            fist.localPosition = targetPos;

            if (col != null) col.enabled = true;
            yield return new WaitForSeconds(punchHoldTime);
            if (col != null) col.enabled = false;

            elapsed = 0f;
            while (elapsed < punchReturnTime)
            { fist.localPosition = Vector3.Lerp(targetPos, startPos, EaseIn(elapsed / punchReturnTime)); elapsed += Time.deltaTime; yield return null; }
            fist.localPosition = startPos;
        }
        else
        { if (col != null) col.enabled = true; yield return new WaitForSeconds(punchHoldTime); if (col != null) col.enabled = false; yield return new WaitForSeconds(punchReturnTime); }

        isAttacking = false;
    }

    float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    float EaseIn(float t) => t * t * t;

    public void OnFistHit(Collider2D other)
    {
        if (other.CompareTag("Player") || other.isTrigger) return;
        if (hitEnemies.Contains(other.gameObject)) return;

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null && !eh.IsDead)
        {
            eh.TakeDamage(damage);
            hitEnemies.Add(other.gameObject);
            if (hitSound != null) { audioSource.pitch = Random.Range(0.9f, 1.1f); audioSource.PlayOneShot(hitSound, hitVolume); }
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.AddForce((other.transform.position - transform.position).normalized * knockbackForce, ForceMode2D.Impulse); }
            if (hitEffectPrefab != null) { GameObject e = Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity); Destroy(e, 1f); }
        }
    }

    void DisableColliders()
    { if (leftFistCollider != null) leftFistCollider.enabled = false; if (rightFistCollider != null) rightFistCollider.enabled = false; }

    void OnEnable() { DisableColliders(); hitEnemies.Clear(); }
    void OnDisable()
    { DisableColliders(); isAttacking = false;
      if (leftFist != null) leftFist.localPosition = leftFistStartPos;
      if (rightFist != null) rightFist.localPosition = rightFistStartPos; }
}

public class FistColliderHelper : MonoBehaviour
{
    public FistsWeapon fistsWeapon;
    void Start() { if (fistsWeapon == null) fistsWeapon = GetComponentInParent<FistsWeapon>(); }
    void OnTriggerEnter2D(Collider2D other) { if (fistsWeapon != null) fistsWeapon.OnFistHit(other); }
}
