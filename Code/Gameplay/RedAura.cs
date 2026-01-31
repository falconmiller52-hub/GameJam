using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Красная Аура — видимая область вокруг игрока, наносящая урон врагам.
/// Добавляется как улучшение.
/// </summary>
public class RedAura : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ АУРЫ ===")]
    [Tooltip("Радиус ауры")]
    public float auraRadius = 2f;
    
    [Tooltip("Урон в секунду")]
    public float damagePerSecond = 2f;
    
    [Tooltip("Интервал нанесения урона")]
    public float damageInterval = 0.5f;

    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Спрайт ауры (круг)")]
    public SpriteRenderer auraSprite;
    
    [Tooltip("Цвет ауры")]
    public Color auraColor = new Color(1f, 0f, 0f, 0.3f);
    
    [Tooltip("Пульсация (мин/макс альфа)")]
    public float pulseMinAlpha = 0.2f;
    public float pulseMaxAlpha = 0.4f;
    public float pulseSpeed = 2f;

    [Header("=== ЭФФЕКТЫ ===")]
    [Tooltip("Партиклы при нанесении урона")]
    public ParticleSystem damageParticles;

    [Header("=== АУДИО ===")]
    public AudioClip auraLoopSound;
    [Range(0f, 1f)]
    public float loopVolume = 0.3f;
    public AudioClip damageTickSound;
    [Range(0f, 1f)]
    public float tickVolume = 0.2f;

    // Приватные переменные
    private AudioSource audioSource;
    private float lastDamageTime;
    private bool isActive = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Создаём визуал ауры если нет
        if (auraSprite == null)
        {
            CreateAuraVisual();
        }

        // Настраиваем размер
        if (auraSprite != null)
        {
            auraSprite.transform.localScale = Vector3.one * auraRadius * 2f;
            auraSprite.color = auraColor;
        }

        // Запускаем звук
        if (auraLoopSound != null)
        {
            audioSource.clip = auraLoopSound;
            audioSource.volume = loopVolume;
            audioSource.loop = true;
            audioSource.Play();
        }

        isActive = true;
        Debug.Log("[RedAura] Аура активирована!");
    }

    void Update()
    {
        if (!isActive) return;

        // Пульсация альфы
        if (auraSprite != null)
        {
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, 
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            Color c = auraSprite.color;
            c.a = alpha;
            auraSprite.color = c;
        }

        // Наносим урон
        if (Time.time >= lastDamageTime + damageInterval)
        {
            DealDamageToEnemiesInRadius();
            lastDamageTime = Time.time;
        }
    }

    void DealDamageToEnemiesInRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, auraRadius);
        bool hitAny = false;

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    int damage = Mathf.RoundToInt(damagePerSecond * damageInterval);
                    damage = Mathf.Max(1, damage);
                    enemyHealth.TakeDamage(damage);
                    hitAny = true;

                    // Партиклы на враге
                    if (damageParticles != null)
                    {
                        ParticleSystem ps = Instantiate(damageParticles, hit.transform.position, Quaternion.identity);
                        ps.Play();
                        Destroy(ps.gameObject, 1f);
                    }
                }
            }
        }

        // Звук тика урона
        if (hitAny && damageTickSound != null)
        {
            audioSource.PlayOneShot(damageTickSound, tickVolume);
        }
    }

    void CreateAuraVisual()
    {
        GameObject auraObj = new GameObject("AuraVisual");
        auraObj.transform.SetParent(transform);
        auraObj.transform.localPosition = Vector3.zero;

        auraSprite = auraObj.AddComponent<SpriteRenderer>();
        
        // Создаём круглый спрайт
        Texture2D texture = new Texture2D(128, 128);
        Color[] colors = new Color[128 * 128];
        Vector2 center = new Vector2(64, 64);

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 64)
                {
                    float alpha = 1f - (dist / 64f);
                    alpha = Mathf.Pow(alpha, 0.5f); // Мягкий градиент
                    colors[y * 128 + x] = new Color(1f, 0f, 0f, alpha);
                }
                else
                {
                    colors[y * 128 + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 64f);
        auraSprite.sprite = sprite;
        auraSprite.sortingOrder = -1;
    }

    /// <summary>
    /// Увеличивает параметры ауры (при повторном получении улучшения)
    /// </summary>
    public void Upgrade(float radiusBonus, float damageBonus)
    {
        auraRadius += radiusBonus;
        damagePerSecond += damageBonus;

        if (auraSprite != null)
        {
            auraSprite.transform.localScale = Vector3.one * auraRadius * 2f;
        }

        Debug.Log($"[RedAura] Улучшена! Радиус: {auraRadius}, Урон/сек: {damagePerSecond}");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }

    void OnDestroy()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}
