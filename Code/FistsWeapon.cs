using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class FistsWeapon : MonoBehaviour
{
    [Header("=== DAMAGE ===")]
    public int damage = 4;
    public float knockbackForce = 12f;

    [Header("=== COOLDOWNS ===")]
    public float leftAttackCooldown = 0.4f;
    public float rightAttackCooldown = 0.4f;
    public float globalCooldown = 0.2f;

    [Header("=== PUNCH MOVEMENT ===")]
    public float punchDistance = 0.5f;
    public float punchOutTime = 0.08f;
    public float punchHoldTime = 0.12f;
    public float punchReturnTime = 0.15f;

    [Header("=== FIST OBJECTS ===")]
    public Transform leftFist;
    public Transform rightFist;

    [Header("=== FIST POSITION ===")]
    public Vector3 leftFistOffset = new Vector3(-0.3f, 0.1f, 0f);
    public Vector3 rightFistOffset = new Vector3(0.3f, -0.1f, 0f);
    public bool autoPositionFists = true;

    [Header("=== ANIMATORS ===")]
    public Animator leftFistAnimator;
    public Animator rightFistAnimator;
    public string punchTrigger = "Punch";

    [Header("=== COLLIDERS ===")]
    public Collider2D leftFistCollider;
    public Collider2D rightFistCollider;

    [Header("=== AUDIO ===")]
    public AudioClip leftPunchSound;
    public AudioClip rightPunchSound;
    public AudioClip hitSound;
    [Range(0f, 1f)] public float punchVolume = 0.7f;
    [Range(0f, 1f)] public float hitVolume = 0.8f;

    [Header("=== VISUAL ===")]
    public GameObject hitEffectPrefab;

    private AudioSource audioSource;
    private float lastLeftTime = -999f, lastRightTime = -999f, lastAnyTime = -999f;
    private bool isAttacking;
    private List<GameObject> hitEnemies = new List<GameObject>();
    private WeaponSwitcher weaponSwitcher;
    private Vector3 leftStartPos, rightStartPos;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        weaponSwitcher = GetComponentInParent<WeaponSwitcher>();
        if (weaponSwitcher == null) { GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) weaponSwitcher = p.GetComponent<WeaponSwitcher>(); }

        if (autoPositionFists && leftFist != null && rightFist != null)
        {
            if (Vector3.Distance(leftFist.localPosition, rightFist.localPosition) < 0.01f)
            { leftFist.localPosition = leftFistOffset; rightFist.localPosition = rightFistOffset; }
        }

        if (leftFist != null) leftStartPos = leftFist.localPosition;
        if (rightFist != null) rightStartPos = rightFist.localPosition;
        if (leftFistAnimator == null && leftFist != null) leftFistAnimator = leftFist.GetComponent<Animator>();
        if (rightFistAnimator == null && rightFist != null) rightFistAnimator = rightFist.GetComponent<Animator>();

        EnsureHelper(leftFist, leftFistCollider);
        EnsureHelper(rightFist, rightFistCollider);
        DisableCol();
    }

    void EnsureHelper(Transform f, Collider2D c)
    {
        if (f == null) return;
        FistColliderHelper h = f.GetComponent<FistColliderHelper>();
        if (h == null) h = f.gameObject.AddComponent<FistColliderHelper>();
        h.fistsWeapon = this;
        if (c != null && c.gameObject != f.gameObject)
        { FistColliderHelper ch = c.GetComponent<FistColliderHelper>(); if (ch == null) ch = c.gameObject.AddComponent<FistColliderHelper>(); ch.fistsWeapon = this; }
    }

    void Update()
    {
        if (weaponSwitcher != null && !weaponSwitcher.IsFistsActive()) return;
        if (PauseMenu.isPaused || Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame && CanL()) StartCoroutine(Punch(true));
        if (Mouse.current.rightButton.wasPressedThisFrame && CanR()) StartCoroutine(Punch(false));
    }

    bool CanL() => !isAttacking && Time.time >= lastAnyTime + globalCooldown && Time.time >= lastLeftTime + leftAttackCooldown;
    bool CanR() => !isAttacking && Time.time >= lastAnyTime + globalCooldown && Time.time >= lastRightTime + rightAttackCooldown;

    IEnumerator Punch(bool left)
    {
        isAttacking = true; hitEnemies.Clear();
        if (left) lastLeftTime = Time.time; else lastRightTime = Time.time;
        lastAnyTime = Time.time;

        Transform fist = left ? leftFist : rightFist;
        Collider2D col = left ? leftFistCollider : rightFistCollider;
        AudioClip snd = left ? leftPunchSound : rightPunchSound;
        Animator anim = left ? leftFistAnimator : rightFistAnimator;
        Vector3 sp = left ? leftStartPos : rightStartPos;

        if (snd != null) { audioSource.pitch = Random.Range(0.9f, 1.1f); audioSource.PlayOneShot(snd, punchVolume); }
        if (anim != null) { anim.ResetTrigger(punchTrigger); anim.SetTrigger(punchTrigger); }

        if (fist != null)
        {
            Vector3 tp = sp + Vector3.right * punchDistance;
            float e = 0f;
            while (e < punchOutTime) { fist.localPosition = Vector3.Lerp(sp, tp, 1f - Mathf.Pow(1f - e / punchOutTime, 3f)); e += Time.deltaTime; yield return null; }
            fist.localPosition = tp;
            if (col != null) col.enabled = true;
            yield return new WaitForSeconds(punchHoldTime);
            if (col != null) col.enabled = false;
            e = 0f;
            while (e < punchReturnTime) { fist.localPosition = Vector3.Lerp(tp, sp, (e / punchReturnTime) * (e / punchReturnTime) * (e / punchReturnTime)); e += Time.deltaTime; yield return null; }
            fist.localPosition = sp;
        }
        else { if (col != null) col.enabled = true; yield return new WaitForSeconds(punchHoldTime); if (col != null) col.enabled = false; yield return new WaitForSeconds(punchReturnTime); }
        isAttacking = false;
    }

    public void OnFistHit(Collider2D other)
    {
        if (other.CompareTag("Player") || other.isTrigger || hitEnemies.Contains(other.gameObject)) return;
        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null && !eh.IsDead)
        {
            eh.TakeDamage(damage); hitEnemies.Add(other.gameObject);
            if (hitSound != null) { audioSource.pitch = Random.Range(0.9f, 1.1f); audioSource.PlayOneShot(hitSound, hitVolume); }
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.AddForce((other.transform.position - transform.position).normalized * knockbackForce, ForceMode2D.Impulse); }
            if (hitEffectPrefab != null) Destroy(Instantiate(hitEffectPrefab, other.transform.position, Quaternion.identity), 1f);
        }
    }

    void DisableCol() { if (leftFistCollider != null) leftFistCollider.enabled = false; if (rightFistCollider != null) rightFistCollider.enabled = false; }
    void OnEnable() { DisableCol(); hitEnemies.Clear(); }
    void OnDisable() { DisableCol(); isAttacking = false; if (leftFist != null) leftFist.localPosition = leftStartPos; if (rightFist != null) rightFist.localPosition = rightStartPos; }
}

public class FistColliderHelper : MonoBehaviour
{
    public FistsWeapon fistsWeapon;
    void Start() { if (fistsWeapon == null) fistsWeapon = GetComponentInParent<FistsWeapon>(); }
    void OnTriggerEnter2D(Collider2D other) { if (fistsWeapon != null) fistsWeapon.OnFistHit(other); }
}
