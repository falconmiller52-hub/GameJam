using UnityEngine;
using System.Collections;

/// <summary>
/// Щит — дополнительная жизнь.
/// 
/// ИСПРАВЛЕНО:
/// - Ищет/создаёт ShieldIcon как дочерний объект Player
/// - Корректно показывает спрайты полного/сломанного щита
/// - Ударная волна: префаб ИЛИ покадровая анимация из shockwaveFrames
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ ЩИТА ===")]
    public int maxShield = 1;
    public int currentShield;
    public float regenTime = 5f;
    public float regenDelay = 3f;

    [Header("=== УДАРНАЯ ВОЛНА ===")]
    public float shockwaveRadius = 4f;
    public float knockbackForce = 15f;
    public int shockwaveDamage = 1;

    [Header("=== ВИЗУАЛ ЩИТА ===")]
    [Tooltip("Автонаходится по имени ShieldIcon на Player")]
    public SpriteRenderer shieldIcon;
    public Sprite shieldFullSprite;
    public Sprite shieldBrokenSprite;

    [Header("=== АНИМАЦИЯ УДАРНОЙ ВОЛНЫ ===")]
    public GameObject shockwaveEffectPrefab;
    public Sprite[] shockwaveFrames;
    public float shockwaveFrameTime = 0.1f;
    public float shockwaveVisualScale = 3f;

    [Header("=== АУДИО ===")]
    public AudioClip shieldHitSound;
    [Range(0f, 1f)] public float hitVolume = 0.6f;
    public AudioClip shieldRegenSound;
    [Range(0f, 1f)] public float regenVolume = 0.4f;
    public AudioClip shieldBreakSound;
    [Range(0f, 1f)] public float breakVolume = 1f;

    private AudioSource audioSource;
    private PlayerHealth playerHealth;
    private float lastDamageTime = -999f;
    private float regenTimer = 0f;
    private bool isActive = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        FindPlayerHealth();
        currentShield = maxShield;

        // 🔥 Находим или создаём ShieldIcon
        SetupShieldIcon();

        isActive = true;
        UpdateIcon();
        Debug.Log($"[PlayerShield] Щит активирован! {currentShield}/{maxShield}, Icon={shieldIcon != null}");
    }

    void FindPlayerHealth()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerHealth = p.GetComponent<PlayerHealth>();
        }
    }

    /// <summary>
    /// Ищет ShieldIcon среди дочерних объектов Player.
    /// Если не находит — создаёт новый.
    /// </summary>
    void SetupShieldIcon()
    {
        if (shieldIcon != null) return;

        // 1. Ищем по имени среди дочерних
        Transform iconT = transform.Find("ShieldIcon");
        if (iconT != null)
        {
            shieldIcon = iconT.GetComponent<SpriteRenderer>();
            Debug.Log("[PlayerShield] ShieldIcon найден на Player!");
            return;
        }

        // 2. Ищем по имени рекурсивно (может быть вложен глубже)
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "ShieldIcon")
            {
                shieldIcon = child.GetComponent<SpriteRenderer>();
                if (shieldIcon != null)
                {
                    Debug.Log("[PlayerShield] ShieldIcon найден в дочерних!");
                    return;
                }
            }
        }

        // 3. Создаём новый ShieldIcon
        if (shieldFullSprite != null || shieldBrokenSprite != null)
        {
            GameObject iconObj = new GameObject("ShieldIcon");
            iconObj.transform.SetParent(transform);
            iconObj.transform.localPosition = new Vector3(0f, 1.5f, 0);
            iconObj.transform.localScale = Vector3.one * 5f;
            
            shieldIcon = iconObj.AddComponent<SpriteRenderer>();
            shieldIcon.sortingOrder = 10;
            
            Debug.Log("[PlayerShield] ShieldIcon создан программно!");
        }
    }

    void Update()
    {
        if (!isActive) return;
        HandleRegen();
    }

    void HandleRegen()
    {
        if (currentShield >= maxShield) return;
        if (playerHealth == null) { FindPlayerHealth(); return; }
        if (playerHealth.currentHealth < playerHealth.maxHealth) { regenTimer = 0f; return; }
        if (Time.time < lastDamageTime + regenDelay) return;

        regenTimer += Time.deltaTime;
        if (regenTimer >= regenTime)
        {
            currentShield = Mathf.Min(currentShield + 1, maxShield);
            regenTimer = 0f;

            if (shieldRegenSound != null && audioSource != null)
                audioSource.PlayOneShot(shieldRegenSound, regenVolume);

            UpdateIcon();
            Debug.Log($"[PlayerShield] Щит восстановлен! {currentShield}/{maxShield}");
        }
    }

    public int TakeDamage(int damage)
    {
        if (!isActive || currentShield <= 0) return damage;

        lastDamageTime = Time.time;
        regenTimer = 0f;

        int absorbed = Mathf.Min(damage, currentShield);
        currentShield -= absorbed;

        if (shieldHitSound != null && audioSource != null)
            audioSource.PlayOneShot(shieldHitSound, hitVolume);

        UpdateIcon();

        if (currentShield <= 0)
            OnShieldBroken();

        return damage - absorbed;
    }

    void OnShieldBroken()
    {
        Debug.Log("[PlayerShield] ЩИТ РАЗРУШЕН! Ударная волна!");

        if (shieldBreakSound != null && audioSource != null)
            audioSource.PlayOneShot(shieldBreakSound, breakVolume);

        // Урон и отбрасывание
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = (hit.transform.position - transform.position).normalized;
                    rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }
                EnemyHealth eh = hit.GetComponent<EnemyHealth>();
                if (eh != null && shockwaveDamage > 0) eh.TakeDamage(shockwaveDamage);
            }
            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null) Destroy(hit.gameObject);
        }

        // Визуал ударной волны
        if (shockwaveEffectPrefab != null)
        {
            GameObject eff = Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity);
            eff.transform.localScale = Vector3.one * shockwaveVisualScale;
            Destroy(eff, 2f);
            Debug.Log("[PlayerShield] Ударная волна — из префаба!");
        }
        else if (shockwaveFrames != null && shockwaveFrames.Length > 0)
        {
            StartCoroutine(PlayShockwaveFrames());
            Debug.Log("[PlayerShield] Ударная волна — покадровая!");
        }
        else
        {
            Debug.LogWarning("[PlayerShield] Нет ни префаба, ни спрайтов для ударной волны!");
        }
    }

    IEnumerator PlayShockwaveFrames()
    {
        GameObject obj = new GameObject("ShockwaveAnim");
        obj.transform.position = transform.position;
        obj.transform.localScale = Vector3.one * shockwaveVisualScale;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 50;

        foreach (Sprite frame in shockwaveFrames)
        {
            sr.sprite = frame;
            yield return new WaitForSeconds(shockwaveFrameTime);
        }
        Destroy(obj);
    }

    void UpdateIcon()
    {
        if (shieldIcon == null)
        {
            SetupShieldIcon();
            if (shieldIcon == null) return;
        }

        if (currentShield > 0)
        {
            shieldIcon.gameObject.SetActive(true);
            if (shieldFullSprite != null) shieldIcon.sprite = shieldFullSprite;
            shieldIcon.color = Color.white;
        }
        else
        {
            if (shieldBrokenSprite != null)
            {
                shieldIcon.gameObject.SetActive(true);
                shieldIcon.sprite = shieldBrokenSprite;
                shieldIcon.color = new Color(1f, 1f, 1f, 0.7f);
            }
            else
            {
                shieldIcon.gameObject.SetActive(false);
            }
        }
    }

    public bool HasShield() => currentShield > 0;

    public void Upgrade(int bonus)
    {
        maxShield += bonus;
        currentShield = Mathf.Min(currentShield + bonus, maxShield);
        UpdateIcon();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
