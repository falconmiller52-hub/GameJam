using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Красная Аура — видимая область вокруг игрока.
/// Наносит n урона врагам внутри с кулдауном m.
/// Поддерживает назначенный SpriteRenderer с анимацией в инспекторе.
/// </summary>
public class RedAura : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ АУРЫ ===")]
    public float auraRadius = 2f;
    [Tooltip("Урон за тик")]
    public int damagePerTick = 2;
    [Tooltip("Кулдаун между тиками урона (секунды)")]
    public float damageCooldown = 0.5f;

    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Назначь сюда SpriteRenderer ауры (если есть спрайт с анимацией)")]
    public SpriteRenderer auraSprite;
    public Color auraColor = new Color(1f, 0f, 0f, 0.3f);
    [Tooltip("Пульсация альфы")]
    public float pulseMinAlpha = 0.2f;
    public float pulseMaxAlpha = 0.4f;
    public float pulseSpeed = 2f;

    [Header("=== АУДИО ===")]
    public AudioClip auraLoopSound;
    [Range(0f, 1f)] public float loopVolume = 0.3f;
    public AudioClip damageTickSound;
    [Range(0f, 1f)] public float tickVolume = 0.2f;

    [Header("=== ЭФФЕКТЫ ===")]
    public ParticleSystem damageParticles;

    private AudioSource audioSource;
    private float lastDamageTime;
    private bool isActive = false;
    private bool wasCreatedProgrammatically = false; // Флаг: спрайт создан кодом или из префаба

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Если спрайт не назначен — создаём программный
        if (auraSprite == null)
        {
            CreateAuraVisual();
            wasCreatedProgrammatically = true;
        }

        // Размер — перезаписываем ТОЛЬКО для программного спрайта!
        // Если спрайт из префаба — он уже имеет нужный масштаб, не трогаем
        if (auraSprite != null)
        {
            if (wasCreatedProgrammatically)
            {
                auraSprite.transform.localScale = Vector3.one * auraRadius * 2f;
            }
            auraSprite.color = auraColor;
        }

        // Звук
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

        // Пульсация
        if (auraSprite != null)
        {
            float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
            Color c = auraSprite.color;
            c.a = alpha;
            auraSprite.color = c;
        }

        // Урон с кулдауном
        if (Time.time >= lastDamageTime + damageCooldown)
        {
            DealDamage();
            lastDamageTime = Time.time;
        }
    }

    void DealDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, auraRadius);
        bool hitAny = false;

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            EnemyHealth eh = hit.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead)
            {
                eh.TakeDamage(damagePerTick);
                hitAny = true;

                if (damageParticles != null)
                {
                    ParticleSystem ps = Instantiate(damageParticles, hit.transform.position, Quaternion.identity);
                    ps.Play();
                    Destroy(ps.gameObject, 1f);
                }
            }
        }

        if (hitAny && damageTickSound != null)
            audioSource.PlayOneShot(damageTickSound, tickVolume);
    }

    void CreateAuraVisual()
    {
        GameObject obj = new GameObject("AuraVisual");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        auraSprite = obj.AddComponent<SpriteRenderer>();

        Texture2D tex = new Texture2D(128, 128);
        Color[] cols = new Color[128 * 128];
        Vector2 center = new Vector2(64, 64);

        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < 64)
                {
                    float a = 1f - (dist / 64f);
                    a = Mathf.Pow(a, 0.5f);
                    cols[y * 128 + x] = new Color(1f, 0f, 0f, a);
                }
                else cols[y * 128 + x] = Color.clear;
            }
        }
        tex.SetPixels(cols); tex.Apply();
        auraSprite.sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 64f);
        auraSprite.sortingOrder = -1;
    }

    public void Upgrade(float radiusBonus, float damageBonus)
    {
        auraRadius += radiusBonus;
        damagePerTick += Mathf.RoundToInt(damageBonus);
        // Масштабируем только программно созданный спрайт
        if (wasCreatedProgrammatically && auraSprite != null)
            auraSprite.transform.localScale = Vector3.one * auraRadius * 2f;
    }

    void OnDestroy()
    {
        if (audioSource != null) audioSource.Stop();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}
