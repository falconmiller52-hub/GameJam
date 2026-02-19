using UnityEngine;
using System.Collections;

/// <summary>
/// Снаряд для стрельцов.
/// 
/// ПОВЕДЕНИЕ:
/// 1. Высокая начальная скорость
/// 2. Постепенное замедление
/// 3. Плавное растворение (fade out)
/// 4. Урон при столкновении с игроком
/// 5. Исчезновение при столкновении со стенами
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("=== СКОРОСТЬ ===")]
    [Tooltip("Начальная скорость снаряда")]
    public float initialSpeed = 12f;
    
    [Tooltip("Минимальная скорость (перестаёт замедляться)")]
    public float minSpeed = 2f;
    
    [Tooltip("Скорость замедления (единиц в секунду)")]
    public float deceleration = 4f;

    [Header("=== ВРЕМЯ ЖИЗНИ ===")]
    [Tooltip("Время до начала растворения")]
    public float timeBeforeFade = 1.5f;
    
    [Tooltip("Длительность растворения")]
    public float fadeDuration = 0.8f;
    
    [Tooltip("Максимальное время жизни (аварийное удаление)")]
    public float maxLifetime = 5f;

    [Header("=== УРОН ===")]
    [Tooltip("Урон игроку")]
    public int damage = 1;
    
    [Tooltip("Уничтожать при попадании в игрока?")]
    public bool destroyOnHit = true;
    
    [Tooltip("Уничтожать при попадании в стены?")]
    public bool destroyOnWall = true;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    [Tooltip("Вращение снаряда в полёте (градусов в секунду)")]
    public float rotationSpeed = 0f;
    
    [Tooltip("Префаб эффекта при попадании")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("Оставлять след? (Trail Renderer)")]
    public bool useTrail = true;
    
    [Tooltip("Цвет следа")]
    public Color trailColor = new Color(1f, 0.5f, 0f, 0.5f);

    [Header("=== АУДИО ===")]
    [Tooltip("Звук полёта (loop)")]
    public AudioClip flySound;
    [Range(0f, 1f)]
    public float flyVolume = 0.3f;
    
    [Tooltip("Звук попадания")]
    public AudioClip hitSound;
    [Range(0f, 1f)]
    public float hitVolume = 0.6f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    // ===== ПРИВАТНЫЕ ПЕРЕМЕННЫЕ =====
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private TrailRenderer trailRenderer;
    private AudioSource audioSource;
    private Collider2D col;

    private Vector2 moveDirection;
    private float currentSpeed;
    private bool isInitialized = false;
    private bool isFading = false;
    private bool isDestroyed = false;
    private Color originalColor;
    private float spawnTime;
    private GameObject owner; // 🔥 Владелец снаряда (враг, который выстрелил)

    // ===== UNITY CALLBACKS =====

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        
        // Настройка Rigidbody2D
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Аудио
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;

        // Сохраняем оригинальный цвет
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Создаём Trail Renderer если нужно
        if (useTrail)
        {
            SetupTrail();
        }

        spawnTime = Time.time;
    }

    void Start()
    {
        // Если не инициализирован извне — летим вперёд
        if (!isInitialized)
        {
            Initialize(transform.right);
        }

        // Звук полёта
        if (flySound != null)
        {
            audioSource.clip = flySound;
            audioSource.volume = flyVolume;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Запускаем корутину жизненного цикла
        StartCoroutine(LifecycleRoutine());

        // Аварийное удаление
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (isDestroyed) return;

        // Вращение в полёте
        if (rotationSpeed != 0f)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (isDestroyed) return;

        // Замедление
        if (currentSpeed > minSpeed)
        {
            currentSpeed -= deceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Max(currentSpeed, minSpeed);
        }

        // Применяем скорость
        rb.linearVelocity = moveDirection * currentSpeed;
    }

    // ===== ИНИЦИАЛИЗАЦИЯ =====

    /// <summary>
    /// Инициализация снаряда (вызывается стрельцом)
    /// </summary>
    public void Initialize(Vector2 direction)
    {
        Initialize(direction, initialSpeed, null);
    }

    /// <summary>
    /// Инициализация с кастомной скоростью
    /// </summary>
    public void Initialize(Vector2 direction, float speed)
    {
        Initialize(direction, speed, null);
    }

    /// <summary>
    /// Полная инициализация с владельцем (рекомендуется!)
    /// </summary>
    public void Initialize(Vector2 direction, float speed, GameObject projectileOwner)
    {
        moveDirection = direction.normalized;
        currentSpeed = speed;
        initialSpeed = speed;
        isInitialized = true;
        owner = projectileOwner;

        // 🔥 ИГНОРИРУЕМ КОЛЛАЙДЕР ВЛАДЕЛЬЦА!
        if (owner != null && col != null)
        {
            Collider2D ownerCollider = owner.GetComponent<Collider2D>();
            if (ownerCollider != null)
            {
                Physics2D.IgnoreCollision(col, ownerCollider, true);
                if (debugLogs) Debug.Log($"[Projectile] Игнорируем коллайдер владельца: {owner.name}");
            }
            
            // Также игнорируем все коллайдеры детей владельца (на всякий случай)
            Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
            foreach (var ownerCol in ownerColliders)
            {
                Physics2D.IgnoreCollision(col, ownerCol, true);
            }
        }

        // Сразу задаём начальную скорость
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * currentSpeed;
        }

        if (debugLogs) Debug.Log($"[Projectile] Запущен! Направление: {moveDirection}, Скорость: {currentSpeed}");
    }

    // ===== ЖИЗНЕННЫЙ ЦИКЛ =====

    IEnumerator LifecycleRoutine()
    {
        // Фаза 1: Полёт
        yield return new WaitForSeconds(timeBeforeFade);

        // Фаза 2: Растворение
        if (!isDestroyed)
        {
            yield return StartCoroutine(FadeOutRoutine());
        }
    }

    IEnumerator FadeOutRoutine()
    {
        isFading = true;
        
        if (debugLogs) Debug.Log($"[Projectile] Начинаем растворение...");

        float elapsed = 0f;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : originalColor;
        
        // Также затухаем звук
        float startVolume = audioSource != null ? audioSource.volume : 0f;

        while (elapsed < fadeDuration)
        {
            float t = elapsed / fadeDuration;
            
            // Плавное уменьшение альфы спрайта
            if (spriteRenderer != null)
            {
                Color newColor = startColor;
                newColor.a = Mathf.Lerp(startColor.a, 0f, t);
                spriteRenderer.color = newColor;
            }

            // Затухание звука
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
            }

            // Также можно уменьшать размер для эффекта
            float scale = Mathf.Lerp(1f, 0.5f, t);
            transform.localScale = Vector3.one * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Полностью исчез
        DestroySelf();
    }

    // ===== СТОЛКНОВЕНИЯ =====

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;

        // 🔥 Игнорируем владельца снаряда!
        if (owner != null && other.gameObject == owner) return;
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        // Игнорируем других врагов и их снаряды
        if (other.CompareTag("Enemy")) return;
        if (other.GetComponent<Projectile>() != null) return;
        
        // Игнорируем триггеры (кроме игрока)
        if (other.isTrigger && !other.CompareTag("Player")) return;

        if (debugLogs) Debug.Log($"[Projectile] Столкновение с: {other.name} (Tag: {other.tag})");

        // Попадание в игрока
        if (other.CompareTag("Player"))
        {
            HitPlayer(other);
            return;
        }

        // Попадание в стену/препятствие
        if (destroyOnWall)
        {
            HitWall(other);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;

        // 🔥 Игнорируем владельца снаряда!
        if (owner != null && collision.gameObject == owner) return;
        if (owner != null && collision.transform.IsChildOf(owner.transform)) return;

        // Игнорируем врагов
        if (collision.gameObject.CompareTag("Enemy")) return;

        if (debugLogs) Debug.Log($"[Projectile] Коллизия с: {collision.gameObject.name}");

        // Попадание в игрока
        if (collision.gameObject.CompareTag("Player"))
        {
            HitPlayer(collision.collider);
            return;
        }

        // Попадание в стену
        if (destroyOnWall)
        {
            HitWall(collision.collider);
        }
    }

    void HitPlayer(Collider2D playerCollider)
    {
        if (debugLogs) Debug.Log($"[Projectile] Попадание в игрока! Урон: {damage}");

        // Наносим урон
        PlayerHealth playerHealth = playerCollider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Эффект попадания
        SpawnHitEffect();

        // Звук попадания
        PlayHitSound();

        if (destroyOnHit)
        {
            DestroySelf();
        }
    }

    void HitWall(Collider2D wallCollider)
    {
        if (debugLogs) Debug.Log($"[Projectile] Попадание в стену: {wallCollider.name}");

        // Эффект попадания
        SpawnHitEffect();

        // Звук попадания
        PlayHitSound();

        DestroySelf();
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

    void SetupTrail()
    {
        trailRenderer = gameObject.AddComponent<TrailRenderer>();
        trailRenderer.time = 0.3f;
        trailRenderer.startWidth = 0.2f;
        trailRenderer.endWidth = 0f;
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trailRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : 0;
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }

    void PlayHitSound()
    {
        if (hitSound != null)
        {
            // Создаём временный объект для звука (так как снаряд уничтожится)
            GameObject soundObj = new GameObject("ProjectileHitSound");
            soundObj.transform.position = transform.position;
            AudioSource src = soundObj.AddComponent<AudioSource>();
            src.clip = hitSound;
            src.volume = hitVolume;
            src.pitch = Random.Range(0.9f, 1.1f);
            src.spatialBlend = 0f;
            src.Play();
            Destroy(soundObj, hitSound.length + 0.1f);
        }
    }

    void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // Останавливаем звук
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // Отключаем коллайдер
        if (col != null)
        {
            col.enabled = false;
        }

        // Останавливаем движение
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (debugLogs) Debug.Log($"[Projectile] Уничтожен!");

        Destroy(gameObject);
    }

    // ===== ОТЛАДКА =====

    void OnDrawGizmos()
    {
        if (Application.isPlaying && !isDestroyed)
        {
            // Направление движения
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + moveDirection * 0.5f);
        }
    }
}