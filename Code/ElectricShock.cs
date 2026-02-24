using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Электрошок — цепная молния от игрока к ближайшему врагу,
/// затем к следующему ближайшему от того и т.д.
/// 3 спрайта молнии: короткий, средний, длинный (с настраиваемыми радиусами).
/// </summary>
public class ElectricShock : MonoBehaviour
{
    [Header("=== ПАРАМЕТРЫ ===")]
    public int damagePerHit = 2;
    [Tooltip("Сколько врагов поражает цепь (3 по умолчанию)")]
    public int maxChainTargets = 3;
    public float shockCooldown = 2f;

    [Header("=== РАДИУСЫ ===")]
    [Tooltip("Радиус поиска первого врага (от игрока)")]
    public float detectionRadius = 5f;
    [Tooltip("Радиус прыжка между врагами")]
    public float chainJumpRadius = 4f;

    [Header("=== СПРАЙТЫ МОЛНИЙ ===")]
    [Tooltip("Короткая молния")]
    public Sprite lightningShort;
    [Tooltip("Средняя молния")]
    public Sprite lightningMedium;
    [Tooltip("Длинная молния")]
    public Sprite lightningLong;

    [Header("=== ПОРОГИ ВЫБОРА СПРАЙТА ===")]
    [Tooltip("Макс. дистанция для короткого спрайта")]
    public float shortMaxDistance = 2f;
    [Tooltip("Макс. дистанция для среднего спрайта")]
    public float mediumMaxDistance = 4f;
    // Всё что больше — длинный спрайт

    [Header("=== ВИЗУАЛ ===")]
    public Color lightningColor = new Color(0.5f, 0.8f, 1f, 1f);
    public float lightningDisplayTime = 0.15f;

    [Header("=== АУДИО ===")]
    public AudioClip shockSound;
    [Range(0f, 1f)] public float shockVolume = 0.6f;
    public AudioClip chainSound;
    [Range(0f, 1f)] public float chainVolume = 0.4f;

    private AudioSource audioSource;
    private float lastShockTime = -999f;
    private bool isActive = false;
    private List<GameObject> activeLightnings = new List<GameObject>();

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Создаём дефолтные спрайты ТОЛЬКО для тех, что не назначены
        // (раньше создавались только если ВСЕ три null — если назначен один, остальные оставались null)
        if (lightningShort == null)
            lightningShort = CreateLightningSprite(32, 8);
        if (lightningMedium == null)
            lightningMedium = CreateLightningSprite(64, 8);
        if (lightningLong == null)
            lightningLong = CreateLightningSprite(128, 8);

        isActive = true;
        Debug.Log("[ElectricShock] Электрошок активирован!");
    }

    void Update()
    {
        if (!isActive) return;
        if (Time.time < lastShockTime + shockCooldown) return;

        // Ищем ближайшего врага в радиусе от игрока
        Transform firstTarget = FindClosestEnemy(transform.position, detectionRadius, null);
        if (firstTarget != null)
        {
            StartCoroutine(ChainLightning(firstTarget));
            lastShockTime = Time.time;
        }
    }

    IEnumerator ChainLightning(Transform firstTarget)
    {
        List<Transform> hitTargets = new List<Transform>();
        Vector3 prevPos = transform.position;
        Transform current = firstTarget;

        if (shockSound != null)
            audioSource.PlayOneShot(shockSound, shockVolume);

        for (int i = 0; i < maxChainTargets; i++)
        {
            if (current == null) break;

            // Урон
            EnemyHealth eh = current.GetComponent<EnemyHealth>();
            if (eh != null && !eh.IsDead)
                eh.TakeDamage(damagePerHit);

            hitTargets.Add(current);

            // Визуал молнии
            CreateLightningVisual(prevPos, current.position);

            // Звук цепи
            if (i > 0 && chainSound != null)
                audioSource.PlayOneShot(chainSound, chainVolume);

            yield return new WaitForSeconds(0.05f);

            // Следующая цель — ближайший враг к текущему (не к игроку!)
            prevPos = current.position;
            current = FindClosestEnemy(prevPos, chainJumpRadius, hitTargets);
        }

        yield return new WaitForSeconds(lightningDisplayTime);
        ClearLightnings();
    }

    Transform FindClosestEnemy(Vector3 from, float radius, List<Transform> exclude)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(from, radius);
        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            if (exclude != null && exclude.Contains(hit.transform)) continue;

            EnemyHealth eh = hit.GetComponent<EnemyHealth>();
            if (eh == null || eh.IsDead) continue;

            float d = Vector2.Distance(from, hit.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = hit.transform;
            }
        }
        return closest;
    }

    void CreateLightningVisual(Vector3 from, Vector3 to)
    {
        GameObject obj = new GameObject("Lightning");
        obj.transform.position = (from + to) / 2f;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.color = lightningColor;
        sr.sortingOrder = 100;

        float distance = Vector2.Distance(from, to);

        // Выбор спрайта по настраиваемым порогам (с fallback)
        Sprite chosenSprite;
        if (distance <= shortMaxDistance && lightningShort != null)
            chosenSprite = lightningShort;
        else if (distance <= mediumMaxDistance && lightningMedium != null)
            chosenSprite = lightningMedium;
        else if (lightningLong != null)
            chosenSprite = lightningLong;
        else
            chosenSprite = lightningMedium ?? lightningShort; // fallback

        if (chosenSprite != null)
        {
            sr.sprite = chosenSprite;
        }
        else
        {
            // Все спрайты null — используем LineRenderer
            CreateLineRendererFallback(obj, from, to);
            activeLightnings.Add(obj);
            return;
        }

        // Поворот
        Vector3 dir = (to - from).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        obj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Масштаб
        if (sr.sprite != null)
        {
            float sprWidth = sr.sprite.bounds.size.x;
            float scaleX = distance / sprWidth;
            obj.transform.localScale = new Vector3(scaleX, 1f, 1f);
        }
        else
        {
            CreateLineRendererFallback(obj, from, to);
        }

        activeLightnings.Add(obj);
    }

    void CreateLineRendererFallback(GameObject obj, Vector3 from, Vector3 to)
    {
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

    void ClearLightnings()
    {
        foreach (GameObject obj in activeLightnings)
            if (obj != null) Destroy(obj);
        activeLightnings.Clear();
    }

    void CreateDefaultSprites()
    {
        lightningShort = CreateLightningSprite(32, 8);
        lightningMedium = CreateLightningSprite(64, 8);
        lightningLong = CreateLightningSprite(128, 8);
    }

    Sprite CreateLightningSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h);
        Color[] cols = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float cy = h / 2f + Mathf.Sin(x * 0.5f) * 2f;
                float d = Mathf.Abs(y - cy);
                cols[y * w + x] = d < h / 2f
                    ? new Color(1f, 1f, 1f, 1f - d / (h / 2f))
                    : Color.clear;
            }
        }
        tex.SetPixels(cols); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 32f);
    }

    public void Upgrade(int damageBonus, int chainBonus)
    {
        damagePerHit += damageBonus;
        maxChainTargets += chainBonus;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chainJumpRadius);
    }
}
