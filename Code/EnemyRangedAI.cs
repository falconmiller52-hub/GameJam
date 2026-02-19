using UnityEngine;
using System.Collections;

/// <summary>
/// AI для врагов типа "Стрелец".
/// Держится на расстоянии от игрока и стреляет снарядами.
/// 
/// ПОВЕДЕНИЕ:
/// - Если игрок слишком близко → отходит назад
/// - Если игрок слишком далеко → подходит ближе
/// - Если игрок в зоне атаки → стреляет
/// 
/// ВАЖНО: Этот скрипт ЗАМЕНЯЕТ обычный EnemyAI для стрельцов!
/// </summary>
public class EnemyRangedAI : MonoBehaviour
{
    [Header("=== ДИСТАНЦИЯ ===")]
    [Tooltip("Минимальная дистанция (ближе — отходит)")]
    public float minDistance = 4f;
    
    [Tooltip("Идеальная дистанция для стрельбы")]
    public float idealDistance = 6f;
    
    [Tooltip("Максимальная дистанция (дальше — подходит)")]
    public float maxDistance = 8f;

    [Header("=== ДВИЖЕНИЕ ===")]
    [Tooltip("Скорость движения")]
    public float moveSpeed = 2f;
    
    [Tooltip("Скорость отступления (обычно быстрее)")]
    public float retreatSpeed = 3f;

    [Header("=== СТРЕЛЬБА ===")]
    [Tooltip("Префаб снаряда")]
    public GameObject projectilePrefab;
    
    [Tooltip("Точка спавна снаряда (если не указана — центр врага)")]
    public Transform firePoint;
    
    [Tooltip("Время между выстрелами")]
    public float fireRate = 2f;
    
    [Tooltip("Время прицеливания перед выстрелом")]
    public float aimDuration = 0.5f;

    [Header("=== АНИМАЦИЯ ===")]
    public Animator animator;
    
    [Tooltip("Триггер для анимации стрельбы")]
    public string shootTrigger = "Shoot";
    
    [Tooltip("Bool: движется ли враг")]
    public string isMovingBool = "IsMoving";

    [Header("=== АУДИО ===")]
    public AudioClip shootSound;
    [Range(0f, 1f)]
    public float shootVolume = 0.7f;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    [Tooltip("Цвет во время прицеливания")]
    public Color aimingColor = new Color(1f, 0.8f, 0.3f, 1f);
    
    [Tooltip("Показывать линию прицеливания?")]
    public bool showAimLine = true;
    
    [Tooltip("Цвет линии прицеливания")]
    public Color aimLineColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("=== ОТЛАДКА ===")]
    public bool showDebugGizmos = true;
    public bool debugLogs = false;

    // ===== ПРИВАТНЫЕ ПЕРЕМЕННЫЕ =====
    private Transform playerTarget;
    private Rigidbody2D rb;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private LineRenderer aimLineRenderer;

    private bool canShoot = true;
    private bool isAiming = false;
    private bool isShooting = false;
    private float lastShootTime = -999f;
    private Color originalColor;
    private Vector2 aimDirection;

    // ===== UNITY CALLBACKS =====

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Создаём firePoint если не указан
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(0.5f, 0, 0);
            firePoint = fp.transform;
        }

        // Создаём LineRenderer для линии прицеливания
        if (showAimLine)
        {
            SetupAimLine();
        }

        FindPlayer();
        
        if (debugLogs) Debug.Log($"[EnemyRangedAI] Стрелец {gameObject.name} готов!");
    }

    void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        // Мёртвые не стреляют
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            if (aimLineRenderer != null)
                aimLineRenderer.enabled = false;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Обновляем направление взгляда (флип спрайта)
        UpdateFacing();

        // Обновляем линию прицеливания
        if (showAimLine && aimLineRenderer != null)
        {
            UpdateAimLine(distanceToPlayer);
        }

        // Проверяем возможность выстрела
        if (!isAiming && !isShooting && canShoot)
        {
            // Игрок в зоне стрельбы?
            if (distanceToPlayer >= minDistance && distanceToPlayer <= maxDistance)
            {
                if (Time.time >= lastShootTime + fireRate)
                {
                    StartCoroutine(ShootSequence());
                }
            }
        }
    }

    void FixedUpdate()
    {
        if (playerTarget == null) return;
        if (enemyHealth != null && enemyHealth.IsDead) return;
        
        // Не двигаемся во время прицеливания/стрельбы
        if (isAiming || isShooting)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            
            if (animator != null)
                animator.SetBool(isMovingBool, false);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);
        Vector2 directionToPlayer = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;

        Vector2 moveDirection = Vector2.zero;
        float currentSpeed = moveSpeed;

        // Логика движения
        if (distanceToPlayer < minDistance)
        {
            // Слишком близко — ОТСТУПАЕМ!
            moveDirection = -directionToPlayer;
            currentSpeed = retreatSpeed;
            
            if (debugLogs) Debug.Log($"[EnemyRangedAI] Отступаем! Дистанция: {distanceToPlayer:F1}");
        }
        else if (distanceToPlayer > maxDistance)
        {
            // Слишком далеко — подходим
            moveDirection = directionToPlayer;
            currentSpeed = moveSpeed;
            
            if (debugLogs) Debug.Log($"[EnemyRangedAI] Приближаемся! Дистанция: {distanceToPlayer:F1}");
        }
        else
        {
            // В идеальной зоне — стоим (или слегка корректируем к ideal)
            float diff = distanceToPlayer - idealDistance;
            if (Mathf.Abs(diff) > 0.5f)
            {
                moveDirection = diff > 0 ? directionToPlayer : -directionToPlayer;
                currentSpeed = moveSpeed * 0.5f; // Медленная корректировка
            }
        }

        // Применяем движение
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * currentSpeed;
        }

        // Анимация движения
        if (animator != null)
        {
            animator.SetBool(isMovingBool, moveDirection.sqrMagnitude > 0.01f);
        }
    }

    // ===== СТРЕЛЬБА =====

    IEnumerator ShootSequence()
    {
        isAiming = true;
        canShoot = false;
        
        if (debugLogs) Debug.Log($"[EnemyRangedAI] Прицеливание...");

        // Останавливаемся
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Фиксируем направление выстрела
        aimDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;

        // Визуальный эффект прицеливания
        if (spriteRenderer != null)
            spriteRenderer.color = aimingColor;

        // Ждём время прицеливания
        yield return new WaitForSeconds(aimDuration);

        // Проверка на смерть
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            ResetState();
            yield break;
        }

        isAiming = false;
        isShooting = true;

        // ВЫСТРЕЛ!
        if (debugLogs) Debug.Log($"[EnemyRangedAI] ВЫСТРЕЛ!");

        // Анимация выстрела
        if (animator != null)
            animator.SetTrigger(shootTrigger);

        // Звук выстрела
        if (shootSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(shootSound, shootVolume);
        }

        // Возвращаем цвет
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        // Создаём снаряд
        SpawnProjectile();

        lastShootTime = Time.time;

        // Небольшая пауза после выстрела (анимация отдачи)
        yield return new WaitForSeconds(0.3f);

        isShooting = false;
        canShoot = true;
    }

    void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[EnemyRangedAI] Префаб снаряда не назначен!");
            return;
        }

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        
        // Создаём снаряд
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Поворачиваем снаряд в направлении полёта
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Передаём направление и ВЛАДЕЛЬЦА в скрипт снаряда
        Projectile projScript = projectile.GetComponent<Projectile>();
        if (projScript != null)
        {
            // 🔥 Передаём gameObject как владельца, чтобы снаряд игнорировал наш коллайдер!
            projScript.Initialize(aimDirection, projScript.initialSpeed, gameObject);
        }
        else
        {
            // Если нет скрипта Projectile, пробуем просто задать скорость
            Rigidbody2D projRB = projectile.GetComponent<Rigidbody2D>();
            if (projRB != null)
            {
                projRB.linearVelocity = aimDirection * 10f;
            }
        }
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTarget = player.transform;
    }

    void UpdateFacing()
    {
        if (spriteRenderer == null || playerTarget == null) return;
        
        // Флип спрайта в сторону игрока
        spriteRenderer.flipX = playerTarget.position.x < transform.position.x;
    }

    void SetupAimLine()
    {
        aimLineRenderer = gameObject.AddComponent<LineRenderer>();
        aimLineRenderer.startWidth = 0.05f;
        aimLineRenderer.endWidth = 0.02f;
        aimLineRenderer.positionCount = 2;
        aimLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        aimLineRenderer.startColor = aimLineColor;
        aimLineRenderer.endColor = new Color(aimLineColor.r, aimLineColor.g, aimLineColor.b, 0f);
        aimLineRenderer.sortingOrder = 10;
        aimLineRenderer.enabled = false;
    }

    void UpdateAimLine(float distanceToPlayer)
    {
        // Показываем линию только когда игрок в зоне стрельбы
        bool shouldShow = isAiming || (distanceToPlayer >= minDistance && distanceToPlayer <= maxDistance && canShoot);
        
        aimLineRenderer.enabled = shouldShow;
        
        if (shouldShow)
        {
            Vector3 start = firePoint != null ? firePoint.position : transform.position;
            Vector3 direction = isAiming ? (Vector3)aimDirection : (playerTarget.position - transform.position).normalized;
            Vector3 end = start + direction * 3f;
            
            aimLineRenderer.SetPosition(0, start);
            aimLineRenderer.SetPosition(1, end);
            
            // Более яркая линия при прицеливании
            if (isAiming)
            {
                aimLineRenderer.startColor = Color.red;
                aimLineRenderer.endColor = new Color(1f, 0f, 0f, 0.3f);
            }
            else
            {
                aimLineRenderer.startColor = aimLineColor;
                aimLineRenderer.endColor = new Color(aimLineColor.r, aimLineColor.g, aimLineColor.b, 0f);
            }
        }
    }

    void ResetState()
    {
        isAiming = false;
        isShooting = false;
        canShoot = true;
        
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        if (aimLineRenderer != null)
            aimLineRenderer.enabled = false;
    }

    // ===== ПУБЛИЧНЫЕ МЕТОДЫ =====

    public bool IsAiming() => isAiming;
    public bool IsShooting() => isShooting;
    public bool IsBusy() => isAiming || isShooting;

    /// <summary>
    /// Прерывает текущее действие (при смерти)
    /// </summary>
    public void InterruptAction()
    {
        StopAllCoroutines();
        ResetState();
    }

    // ===== ОТЛАДКА =====

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Минимальная дистанция — красный (опасная зона)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // Идеальная дистанция — зелёный
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, idealDistance);

        // Максимальная дистанция — жёлтый
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        // Точка стрельбы
        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.1f);
        }
    }
}