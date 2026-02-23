using UnityEngine;
using System.Collections;

public class PlayerShield : MonoBehaviour
{
    [Header("=== SHIELD PARAMS ===")]
    public int maxShield = 1;
    public int currentShield;
    public float regenTime = 5f;
    public float regenDelay = 3f;

    [Header("=== SHOCKWAVE ===")]
    public float shockwaveRadius = 4f;
    public float knockbackForce = 15f;
    public int shockwaveDamage = 1;

    [Header("=== SHIELD VISUAL ===")]
    public SpriteRenderer shieldIcon;
    public Sprite shieldFullSprite;
    public Sprite shieldBrokenSprite;

    [Header("=== SHOCKWAVE ANIMATION ===")]
    public GameObject shockwaveEffectPrefab;
    public Sprite[] shockwaveFrames;
    public float shockwaveFrameTime = 0.1f;
    public float shockwaveVisualScale = 3f;

    [Header("=== AUDIO ===")]
    public AudioClip shieldHitSound;
    [Range(0f, 1f)] public float hitVolume = 0.6f;
    public AudioClip shieldRegenSound;
    [Range(0f, 1f)] public float regenVolume = 0.4f;
    public AudioClip shieldBreakSound;
    [Range(0f, 1f)] public float breakVolume = 1f;

    private AudioSource audioSource;
    private PlayerHealth playerHealth;
    private float lastDamageTime = -999f;
    private float regenTimer;
    private bool isActive;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        FindPlayerHealth();
        currentShield = maxShield;
        SetupShieldIcon();
        isActive = true;
        UpdateIcon();
    }

    void FindPlayerHealth()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) { GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) playerHealth = p.GetComponent<PlayerHealth>(); }
    }

    void SetupShieldIcon()
    {
        if (shieldIcon != null) return;
        Transform iconT = transform.Find("ShieldIcon");
        if (iconT != null) { shieldIcon = iconT.GetComponent<SpriteRenderer>(); return; }
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        { if (child.name == "ShieldIcon") { shieldIcon = child.GetComponent<SpriteRenderer>(); if (shieldIcon != null) return; } }
        if (shieldFullSprite != null || shieldBrokenSprite != null)
        {
            GameObject o = new GameObject("ShieldIcon"); o.transform.SetParent(transform);
            o.transform.localPosition = new Vector3(0f, 1.5f, 0); o.transform.localScale = Vector3.one * 5f;
            shieldIcon = o.AddComponent<SpriteRenderer>(); shieldIcon.sortingOrder = 10;
        }
    }

    void Update() { if (isActive) HandleRegen(); }

    void HandleRegen()
    {
        if (currentShield >= maxShield) return;
        if (playerHealth == null) { FindPlayerHealth(); return; }
        if (playerHealth.currentHealth < playerHealth.maxHealth) { regenTimer = 0f; return; }
        if (Time.time < lastDamageTime + regenDelay) return;
        regenTimer += Time.deltaTime;
        if (regenTimer >= regenTime)
        {
            currentShield = Mathf.Min(currentShield + 1, maxShield); regenTimer = 0f;
            if (shieldRegenSound != null && audioSource != null) audioSource.PlayOneShot(shieldRegenSound, regenVolume);
            UpdateIcon();
        }
    }

    public int TakeDamage(int damage)
    {
        if (!isActive || currentShield <= 0) return damage;
        lastDamageTime = Time.time; regenTimer = 0f;
        int absorbed = Mathf.Min(damage, currentShield); currentShield -= absorbed;
        if (shieldHitSound != null && audioSource != null) audioSource.PlayOneShot(shieldHitSound, hitVolume);
        UpdateIcon();
        if (currentShield <= 0) OnShieldBroken();
        return damage - absorbed;
    }

    void OnShieldBroken()
    {
        if (shieldBreakSound != null && audioSource != null) audioSource.PlayOneShot(shieldBreakSound, breakVolume);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            { Rigidbody2D rb = hit.GetComponent<Rigidbody2D>(); if (rb != null) rb.AddForce((hit.transform.position - transform.position).normalized * knockbackForce, ForceMode2D.Impulse);
              EnemyHealth eh = hit.GetComponent<EnemyHealth>(); if (eh != null && shockwaveDamage > 0) eh.TakeDamage(shockwaveDamage); }
            Projectile proj = hit.GetComponent<Projectile>(); if (proj != null) Destroy(hit.gameObject);
        }
        if (shockwaveEffectPrefab != null)
        { GameObject e = Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity); e.transform.localScale = Vector3.one * shockwaveVisualScale; Destroy(e, 2f); }
        else if (shockwaveFrames != null && shockwaveFrames.Length > 0)
            StartCoroutine(PlayShockwaveFrames());
    }

    IEnumerator PlayShockwaveFrames()
    {
        GameObject o = new GameObject("ShockwaveAnim"); o.transform.position = transform.position; o.transform.localScale = Vector3.one * shockwaveVisualScale;
        SpriteRenderer sr = o.AddComponent<SpriteRenderer>(); sr.sortingOrder = 50;
        foreach (Sprite f in shockwaveFrames) { sr.sprite = f; yield return new WaitForSeconds(shockwaveFrameTime); }
        Destroy(o);
    }

    void UpdateIcon()
    {
        if (shieldIcon == null) { SetupShieldIcon(); if (shieldIcon == null) return; }
        if (currentShield > 0) { shieldIcon.gameObject.SetActive(true); if (shieldFullSprite != null) shieldIcon.sprite = shieldFullSprite; shieldIcon.color = Color.white; }
        else { if (shieldBrokenSprite != null) { shieldIcon.gameObject.SetActive(true); shieldIcon.sprite = shieldBrokenSprite; shieldIcon.color = new Color(1f, 1f, 1f, 0.7f); } else shieldIcon.gameObject.SetActive(false); }
    }

    public bool HasShield() => currentShield > 0;
    public void Upgrade(int bonus) { maxShield += bonus; currentShield = Mathf.Min(currentShield + bonus, maxShield); UpdateIcon(); }
    void OnDrawGizmosSelected() { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, shockwaveRadius); }
}
