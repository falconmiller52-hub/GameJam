using UnityEngine;
using System.Collections;

/// <summary>
/// Щит — дополнительная жизнь. Регенерируется только при полном ХП.
/// При разрушении — ударная волна с анимацией.
/// 
/// ИСПРАВЛЕНО: Использует ТОЛЬКО назначенные спрайты из инспектора.
/// Программные спрайты больше не создаются — если спрайт не назначен, он просто не показывается.
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
    [Tooltip("SpriteRenderer иконки щита (дочерний объект ShieldIcon на Player)")]
    public SpriteRenderer shieldIcon;
    [Tooltip("Спрайт полного щита")]
    public Sprite shieldFullSprite;
    [Tooltip("Спрайт разбитого щита")]
    public Sprite shieldBrokenSprite;

    [Header("=== АНИМАЦИЯ УДАРНОЙ ВОЛНЫ ===")]
    [Tooltip("Префаб эффекта ударной волны (с Animator или анимацией)")]
    public GameObject shockwaveEffectPrefab;
    [Tooltip("ИЛИ: массив спрайтов для покадровой анимации (3 кадра)")]
    public Sprite[] shockwaveFrames;
    public float shockwaveFrameTime = 0.1f;
    [Tooltip("Размер эффекта ударной волны")]
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

        // 🔥 Ищем ShieldIcon на Player если не назначен
        if (shieldIcon == null)
        {
            Transform iconT = transform.Find("ShieldIcon");
            if (iconT != null)
                shieldIcon = iconT.GetComponent<SpriteRenderer>();
        }

        isActive = true;
        UpdateIcon();
        Debug.Log($"[PlayerShield] Щит активирован! {currentShield}/{maxShield}");
    }

    void FindPlayerHealth()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerHealth = p.GetComponent<PlayerHealth>();
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

        // Не регенит пока ХП не полное
        if (playerHealth.currentHealth < playerHealth.maxHealth)
        {
            regenTimer = 0f;
            return;
        }

        // Задержка после урона
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
        if (!isActive || currentShield <= 0)
            return damage;

        lastDamageTime = Time.time;
        regenTimer = 0f;

        int absorbed = Mathf.Min(damage, currentShield);
        currentShield -= absorbed;

        if (shieldHitSound != null && audioSource != null)
            audioSource.PlayOneShot(shieldHitSound, hitVolume);

        UpdateIcon();

        Debug.Log($"[PlayerShield] Поглощено: {absorbed}. Щит: {currentShield}/{maxShield}");

        if (currentShield <= 0)
            OnShieldBroken();

        return damage - absorbed;
    }

    void OnShieldBroken()
    {
        Debug.Log("[PlayerShield] ЩИТ РАЗРУШЕН! Ударная волна!");

        if (shieldBreakSound != null && audioSource != null)
            audioSource.PlayOneShot(shieldBreakSound, breakVolume);

        // Отбрасывание врагов
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
                if (eh != null && shockwaveDamage > 0)
                    eh.TakeDamage(shockwaveDamage);
            }

            Projectile proj = hit.GetComponent<Projectile>();
            if (proj != null) Destroy(hit.gameObject);
        }

        // Анимация ударной волны
        if (shockwaveEffectPrefab != null)
        {
            // Вариант 1: Префаб с Animator
            GameObject eff = Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity);
            eff.transform.localScale = Vector3.one * shockwaveVisualScale;
            Destroy(eff, 2f);
        }
        else if (shockwaveFrames != null && shockwaveFrames.Length > 0)
        {
            // Вариант 2: Покадровая анимация из спрайтов
            StartCoroutine(PlayShockwaveFrames());
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
        if (shieldIcon == null) return;

        if (currentShield > 0)
        {
            shieldIcon.gameObject.SetActive(true);
            if (shieldFullSprite != null)
                shieldIcon.sprite = shieldFullSprite;
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
        Debug.Log($"[PlayerShield] Улучшен! {currentShield}/{maxShield}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
