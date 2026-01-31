using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Защитный Щит — дополнительное ХП, которое восстанавливается при полном здоровье.
/// При разрушении создаёт ударную волну, отталкивающую врагов.
/// </summary>
public class PlayerShield : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ ЩИТА ===")]
    [Tooltip("Максимальное значение щита")]
    public int maxShield = 3;
    
    [Tooltip("Текущее значение щита")]
    public int currentShield;
    
    [Tooltip("Скорость регенерации (секунд на 1 единицу)")]
    public float regenRate = 2f;
    
    [Tooltip("Задержка после получения урона перед регеном")]
    public float regenDelay = 2f;

    [Header("=== УДАРНАЯ ВОЛНА ===")]
    [Tooltip("Радиус отталкивания при разрушении щита")]
    public float shockwaveRadius = 4f;
    
    [Tooltip("Сила отталкивания")]
    public float knockbackForce = 15f;
    
    [Tooltip("Урон ударной волны")]
    public int shockwaveDamage = 1;

    [Header("=== ВИЗУАЛ ЩИТА ===")]
    [Tooltip("Спрайт иконки щита над игроком")]
    public SpriteRenderer shieldIcon;
    
    [Tooltip("Спрайт полного щита")]
    public Sprite shieldFullSprite;
    
    [Tooltip("Спрайт повреждённого щита")]
    public Sprite shieldDamagedSprite;
    
    [Tooltip("Спрайт пустого щита")]
    public Sprite shieldEmptySprite;

    [Header("=== АНИМАЦИИ ===")]
    [Tooltip("Спрайт анимации регенерации")]
    public Sprite regenAnimSprite;
    
    [Tooltip("Спрайт анимации получения урона")]
    public Sprite damageAnimSprite;
    
    [Tooltip("Спрайт/Префаб ударной волны")]
    public GameObject shockwaveEffectPrefab;
    
    [Tooltip("Длительность показа анимации")]
    public float animDisplayTime = 0.3f;

    [Header("=== АУДИО ===")]
    public AudioClip shieldHitSound;
    [Range(0f, 1f)]
    public float hitVolume = 0.6f;
    
    public AudioClip shieldRegenSound;
    [Range(0f, 1f)]
    public float regenVolume = 0.4f;
    
    public AudioClip shieldBreakSound;
    [Range(0f, 1f)]
    public float breakVolume = 1f;

    [Header("=== ССЫЛКИ ===")]
    public PlayerHealth playerHealth;

    // Приватные переменные
    private AudioSource audioSource;
    private SpriteRenderer animationSprite;
    private float lastDamageTime = -999f;
    private float regenTimer = 0f;
    private bool isRegenerating = false;
    private bool isActive = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        currentShield = maxShield;

        // Создаём визуал
        SetupVisuals();

        isActive = true;
        UpdateShieldVisual();

        Debug.Log("[PlayerShield] Щит активирован!");
    }

    void Update()
    {
        if (!isActive) return;

        // Регенерация щита
        HandleRegeneration();
    }

    void HandleRegeneration()
    {
        // Условия для регенерации:
        // 1. Щит не полный
        // 2. Здоровье игрока ПОЛНОЕ
        // 3. Прошло достаточно времени после урона

        if (currentShield >= maxShield) return;

        if (playerHealth != null && playerHealth.currentHealth < playerHealth.maxHealth)
        {
            regenTimer = 0f;
            return; // Нельзя реген пока ХП не полное
        }

        if (Time.time < lastDamageTime + regenDelay) return;

        // Считаем таймер регена
        regenTimer += Time.deltaTime;

        if (regenTimer >= regenRate)
        {
            RegenShield(1);
            regenTimer = 0f;
        }
    }

    void RegenShield(int amount)
    {
        int oldShield = currentShield;
        currentShield = Mathf.Min(currentShield + amount, maxShield);

        if (currentShield > oldShield)
        {
            // Звук регена
            if (shieldRegenSound != null)
                audioSource.PlayOneShot(shieldRegenSound, regenVolume);

            // Анимация регена
            StartCoroutine(ShowAnimation(regenAnimSprite, Color.cyan));

            UpdateShieldVisual();
            Debug.Log($"[PlayerShield] Щит восстановлен: {currentShield}/{maxShield}");
        }
    }

    /// <summary>
    /// Щит принимает урон. Возвращает оставшийся урон (который пройдёт в ХП).
    /// </summary>
    public int TakeDamage(int damage)
    {
        if (!isActive || currentShield <= 0)
            return damage; // Весь урон проходит

        lastDamageTime = Time.time;
        regenTimer = 0f;

        int absorbed = Mathf.Min(damage, currentShield);
        currentShield -= absorbed;
        int remainingDamage = damage - absorbed;

        // Звук попадания по щиту
        if (shieldHitSound != null)
            audioSource.PlayOneShot(shieldHitSound, hitVolume);

        // Анимация урона
        StartCoroutine(ShowAnimation(damageAnimSprite, Color.white));

        UpdateShieldVisual();

        Debug.Log($"[PlayerShield] Щит поглотил {absorbed} урона. Осталось: {currentShield}/{maxShield}");

        // Щит сломан?
        if (currentShield <= 0)
        {
            OnShieldBroken();
        }

        return remainingDamage;
    }

    void OnShieldBroken()
    {
        Debug.Log("[PlayerShield] ЩИТ РАЗРУШЕН! Ударная волна!");

        // Звук разрушения
        if (shieldBreakSound != null)
            audioSource.PlayOneShot(shieldBreakSound, breakVolume);

        // Ударная волна — отталкиваем врагов и снаряды
        CreateShockwave();

        // Визуальный эффект
        if (shockwaveEffectPrefab != null)
        {
            GameObject effect = Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        else
        {
            StartCoroutine(CreateShockwaveVisual());
        }
    }

    void CreateShockwave()
    {
        // Находим всех в радиусе
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius);

        foreach (Collider2D hit in hits)
        {
            // Отталкиваем врагов
            if (hit.CompareTag("Enemy"))
            {
                Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                if (enemyRb != null)
                {
                    Vector2 direction = (hit.transform.position - transform.position).normalized;
                    enemyRb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
                }

                // Урон врагам
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null && shockwaveDamage > 0)
                {
                    enemyHealth.TakeDamage(shockwaveDamage);
                }
            }

            // Уничтожаем снаряды
            Projectile projectile = hit.GetComponent<Projectile>();
            if (projectile != null)
            {
                Destroy(hit.gameObject);
            }
        }

        Debug.Log($"[PlayerShield] Ударная волна поразила {hits.Length} объектов");
    }

    IEnumerator CreateShockwaveVisual()
    {
        // Создаём расширяющийся круг
        GameObject waveObj = new GameObject("ShockwaveVisual");
        waveObj.transform.position = transform.position;

        SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        sr.sortingOrder = 50;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(0.5f, shockwaveRadius * 2f, t);
            float alpha = Mathf.Lerp(0.8f, 0f, t);

            waveObj.transform.localScale = Vector3.one * scale;
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(waveObj);
    }

    IEnumerator ShowAnimation(Sprite sprite, Color color)
    {
        if (animationSprite == null || sprite == null) yield break;

        animationSprite.sprite = sprite;
        animationSprite.color = color;
        animationSprite.enabled = true;

        yield return new WaitForSeconds(animDisplayTime);

        animationSprite.enabled = false;
    }

    void SetupVisuals()
    {
        // Иконка щита
        if (shieldIcon == null)
        {
            GameObject iconObj = new GameObject("ShieldIcon");
            iconObj.transform.SetParent(transform);
            iconObj.transform.localPosition = new Vector3(0.5f, 0.8f, 0);

            shieldIcon = iconObj.AddComponent<SpriteRenderer>();
            shieldIcon.sortingOrder = 10;
        }

        // Спрайт для анимаций
        GameObject animObj = new GameObject("ShieldAnimation");
        animObj.transform.SetParent(transform);
        animObj.transform.localPosition = new Vector3(0, 1f, 0);

        animationSprite = animObj.AddComponent<SpriteRenderer>();
        animationSprite.sortingOrder = 11;
        animationSprite.enabled = false;

        // Создаём дефолтные спрайты если нет
        if (shieldFullSprite == null)
            shieldFullSprite = CreateShieldSprite(Color.cyan);
        if (shieldDamagedSprite == null)
            shieldDamagedSprite = CreateShieldSprite(Color.yellow);
        if (shieldEmptySprite == null)
            shieldEmptySprite = CreateShieldSprite(new Color(0.3f, 0.3f, 0.3f, 0.5f));
    }

    void UpdateShieldVisual()
    {
        if (shieldIcon == null) return;

        float percent = (float)currentShield / maxShield;

        if (percent >= 0.9f)
            shieldIcon.sprite = shieldFullSprite;
        else if (percent > 0.1f)
            shieldIcon.sprite = shieldDamagedSprite;
        else
            shieldIcon.sprite = shieldEmptySprite;

        // Прозрачность зависит от заполнения
        Color c = shieldIcon.color;
        c.a = Mathf.Lerp(0.3f, 1f, percent);
        shieldIcon.color = c;
    }

    Sprite CreateShieldSprite(Color color)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // Форма щита (шестиугольник)
                float cx = x - 16f;
                float cy = y - 16f;
                
                if (Mathf.Abs(cx) + Mathf.Abs(cy) * 0.7f < 14f)
                {
                    colors[y * 32 + x] = color;
                }
                else
                {
                    colors[y * 32 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        Vector2 center = new Vector2(32, 32);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist > 28 && dist < 32)
                {
                    colors[y * 64 + x] = Color.white;
                }
                else
                {
                    colors[y * 64 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 32f);
    }

    /// <summary>
    /// Проверяет, есть ли щит
    /// </summary>
    public bool HasShield() => currentShield > 0;

    /// <summary>
    /// Улучшает щит
    /// </summary>
    public void Upgrade(int maxShieldBonus)
    {
        maxShield += maxShieldBonus;
        currentShield = Mathf.Min(currentShield + maxShieldBonus, maxShield);
        UpdateShieldVisual();
        Debug.Log($"[PlayerShield] Улучшен! Макс: {maxShield}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
