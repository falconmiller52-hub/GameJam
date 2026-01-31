using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Электрошок — невидимая аура, при входе врага бьёт цепной молнией.
/// Молния прыгает от врага к врагу.
/// </summary>
public class ElectricShock : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ ===")]
    [Tooltip("Радиус обнаружения врагов")]
    public float detectionRadius = 4f;
    
    [Tooltip("Урон за удар молнии")]
    public int damagePerHit = 2;
    
    [Tooltip("Максимальное количество целей в цепи")]
    public int maxChainTargets = 4;
    
    [Tooltip("Максимальная дистанция прыжка молнии")]
    public float chainDistance = 3f;
    
    [Tooltip("Кулдаун между ударами")]
    public float shockCooldown = 2f;

    [Header("=== СПРАЙТЫ МОЛНИЙ ===")]
    [Tooltip("Короткая молния (< 1/3 chainDistance)")]
    public Sprite lightningShort;
    
    [Tooltip("Средняя молния (1/3 - 2/3 chainDistance)")]
    public Sprite lightningMedium;
    
    [Tooltip("Длинная молния (> 2/3 chainDistance)")]
    public Sprite lightningLong;

    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Цвет молнии")]
    public Color lightningColor = new Color(0.5f, 0.8f, 1f, 1f);
    
    [Tooltip("Время отображения молнии")]
    public float lightningDisplayTime = 0.15f;

    [Header("=== АУДИО ===")]
    public AudioClip shockSound;
    [Range(0f, 1f)]
    public float shockVolume = 0.6f;
    
    public AudioClip chainSound;
    [Range(0f, 1f)]
    public float chainVolume = 0.4f;

    // Приватные переменные
    private AudioSource audioSource;
    private float lastShockTime = -999f;
    private bool isActive = false;
    private List<GameObject> activeLightnings = new List<GameObject>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Создаём дефолтные спрайты если не назначены
        if (lightningShort == null || lightningMedium == null || lightningLong == null)
        {
            CreateDefaultLightningSprites();
        }

        isActive = true;
        Debug.Log("[ElectricShock] Электрошок активирован!");
    }

    void Update()
    {
        if (!isActive) return;

        // Проверяем кулдаун
        if (Time.time < lastShockTime + shockCooldown) return;

        // Ищем врага в радиусе
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                {
                    // Нашли врага — запускаем цепную молнию!
                    StartCoroutine(ChainLightning(hit.transform));
                    lastShockTime = Time.time;
                    break;
                }
            }
        }
    }

    IEnumerator ChainLightning(Transform firstTarget)
    {
        List<Transform> hitTargets = new List<Transform>();
        Transform currentTarget = firstTarget;
        Vector3 previousPosition = transform.position;

        // Звук первого удара
        if (shockSound != null)
        {
            audioSource.PlayOneShot(shockSound, shockVolume);
        }

        for (int i = 0; i < maxChainTargets; i++)
        {
            if (currentTarget == null) break;

            // Наносим урон
            EnemyHealth health = currentTarget.GetComponent<EnemyHealth>();
            if (health != null && !health.IsDead)
            {
                health.TakeDamage(damagePerHit);
            }

            hitTargets.Add(currentTarget);

            // Создаём визуал молнии
            CreateLightningVisual(previousPosition, currentTarget.position);

            // Звук цепи (для последующих ударов)
            if (i > 0 && chainSound != null)
            {
                audioSource.PlayOneShot(chainSound, chainVolume);
            }

            // Небольшая задержка между прыжками
            yield return new WaitForSeconds(0.05f);

            // Ищем следующую цель
            previousPosition = currentTarget.position;
            currentTarget = FindNextTarget(currentTarget.position, hitTargets);
        }

        // Ждём и удаляем визуалы молний
        yield return new WaitForSeconds(lightningDisplayTime);
        ClearLightningVisuals();

        Debug.Log($"[ElectricShock] Цепь поразила {hitTargets.Count} врагов!");
    }

    Transform FindNextTarget(Vector3 fromPosition, List<Transform> alreadyHit)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(fromPosition, chainDistance);
        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (alreadyHit.Contains(hit.transform)) continue;

            EnemyHealth health = hit.GetComponent<EnemyHealth>();
            if (health == null || health.IsDead) continue;

            float dist = Vector2.Distance(fromPosition, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        return closest;
    }

    void CreateLightningVisual(Vector3 from, Vector3 to)
    {
        GameObject lightningObj = new GameObject("Lightning");
        lightningObj.transform.position = (from + to) / 2f;

        SpriteRenderer sr = lightningObj.AddComponent<SpriteRenderer>();
        sr.color = lightningColor;
        sr.sortingOrder = 100;

        // Выбираем спрайт по длине
        float distance = Vector2.Distance(from, to);
        float shortThreshold = chainDistance / 3f;
        float longThreshold = chainDistance * 2f / 3f;

        if (distance < shortThreshold)
            sr.sprite = lightningShort;
        else if (distance < longThreshold)
            sr.sprite = lightningMedium;
        else
            sr.sprite = lightningLong;

        // Поворачиваем и растягиваем
        Vector3 direction = (to - from).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        lightningObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Масштабируем по длине
        if (sr.sprite != null)
        {
            float spriteWidth = sr.sprite.bounds.size.x;
            float scaleX = distance / spriteWidth;
            lightningObj.transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        else
        {
            // Фоллбэк без спрайта — используем LineRenderer
            CreateLineRenderer(lightningObj, from, to);
        }

        activeLightnings.Add(lightningObj);
    }

    void CreateLineRenderer(GameObject obj, Vector3 from, Vector3 to)
    {
        // Удаляем SpriteRenderer
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) Destroy(sr);

        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lightningColor;
        lr.endColor = lightningColor;
        lr.sortingOrder = 100;

        obj.transform.position = Vector3.zero;
    }

    void ClearLightningVisuals()
    {
        foreach (GameObject obj in activeLightnings)
        {
            if (obj != null)
                Destroy(obj);
        }
        activeLightnings.Clear();
    }

    void CreateDefaultLightningSprites()
    {
        // Создаём простые спрайты-линии разной длины
        lightningShort = CreateLightningSprite(32, 8);
        lightningMedium = CreateLightningSprite(64, 8);
        lightningLong = CreateLightningSprite(128, 8);
    }

    Sprite CreateLightningSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Зигзаг эффект
                float centerY = height / 2f + Mathf.Sin(x * 0.5f) * 2f;
                float distFromCenter = Mathf.Abs(y - centerY);
                
                if (distFromCenter < height / 2f)
                {
                    float alpha = 1f - (distFromCenter / (height / 2f));
                    colors[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    colors[y * width + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 32f);
    }

    /// <summary>
    /// Улучшает параметры электрошока
    /// </summary>
    public void Upgrade(int damageBonus, int chainBonus)
    {
        damagePerHit += damageBonus;
        maxChainTargets += chainBonus;
        Debug.Log($"[ElectricShock] Улучшен! Урон: {damagePerHit}, Цепь: {maxChainTargets}");
    }

    void OnDrawGizmosSelected()
    {
        // Радиус обнаружения
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Радиус цепи
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chainDistance);
    }
}
