using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент рывка для врагов.
/// Добавь этот скрипт к префабу врага, который должен уметь делать рывок.
/// Работает совместно с EnemyAI и EnemyHealth.
/// 
/// ОСОБЕННОСТЬ: Перед рывком враг "телеграфирует" — замирает и показывает,
/// что сейчас атакует. Это даёт игроку шанс увернуться!
/// </summary>
public class EnemyDash : MonoBehaviour
{
    [Header("=== ДИСТАНЦИЯ ДЛЯ РЫВКА ===")]
    [Tooltip("Минимальное расстояние до игрока для рывка (не будет дашить вплотную)")]
    public float minDashDistance = 2f;
    
    [Tooltip("Максимальное расстояние до игрока для рывка (слишком далеко - не дашит)")]
    public float maxDashDistance = 5f;

    [Header("=== ПАРАМЕТРЫ РЫВКА ===")]
    [Tooltip("Скорость рывка (чем больше - тем быстрее летит)")]
    public float dashSpeed = 12f;
    
    [Tooltip("Длительность рывка в секундах")]
    public float dashDuration = 0.25f;
    
    [Tooltip("Время перезарядки между рывками")]
    public float dashCooldown = 3f;

    [Header("=== ТЕЛЕГРАФ (предупреждение) ===")]
    [Tooltip("Включить телеграф перед рывком?")]
    public bool useTelegraph = true;
    
    [Tooltip("Длительность телеграфа (время 'зарядки' перед рывком)")]
    public float telegraphDuration = 0.5f;
    
    [Tooltip("Цвет врага во время телеграфа (зарядки)")]
    public Color telegraphColor = Color.white;
    
    [Tooltip("Мигать во время телеграфа?")]
    public bool blinkDuringTelegraph = true;
    
    [Tooltip("Скорость мигания (раз в секунду)")]
    public float blinkSpeed = 10f;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    [Tooltip("Цвет врага во время рывка")]
    public Color dashColor = new Color(1f, 0.5f, 0f, 1f); // Оранжевый
    
    [Tooltip("Показывать цветовой эффект при рывке?")]
    public bool useColorFlash = true;

    [Header("=== АНИМАЦИЯ ===")]
    [Tooltip("Ссылка на Animator (если не указан - найдётся автоматически)")]
    public Animator animator;
    
    [Tooltip("Имя триггера для анимации телеграфа/подготовки")]
    public string telegraphTrigger = "DashTelegraph";
    
    [Tooltip("Имя триггера для анимации самого рывка")]
    public string dashTrigger = "Dash";
    
    [Tooltip("Имя bool-параметра 'находится в рывке' (опционально)")]
    public string isDashingBool = "IsDashing";

    [Header("=== АУДИО ===")]
    [Tooltip("Звук телеграфа (зарядки перед рывком)")]
    public AudioClip telegraphSound;
    
    [Tooltip("Громкость звука телеграфа")]
    [Range(0f, 1f)]
    public float telegraphVolume = 0.5f;
    
    [Tooltip("Звук самого рывка")]
    public AudioClip dashSound;
    
    [Tooltip("Громкость звука рывка")]
    [Range(0f, 1f)]
    public float dashVolume = 0.7f;

    [Header("=== ОТЛАДКА ===")]
    [Tooltip("Показывать радиусы в редакторе?")]
    public bool showDebugGizmos = true;
    
    [Tooltip("Выводить логи в консоль?")]
    public bool debugLogs = false;

    // ===== ПРИВАТНЫЕ ПЕРЕМЕННЫЕ =====
    private Transform playerTarget;
    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private bool isDashing = false;
    private bool isTelegraphing = false; // Новое: фаза телеграфа
    private bool canDash = true;
    private float lastDashTime = -999f;
    private Color originalColor;
    private Vector2 dashDirection; // Сохраняем направление в момент телеграфа

    // ===== UNITY CALLBACKS =====

    void Start()
    {
        // Получаем все необходимые компоненты
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
        enemyHealth = GetComponent<EnemyHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Animator - ищем если не назначен
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        // Аудио - создаём если нужно
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (dashSound != null || telegraphSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Сохраняем оригинальный цвет
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Ищем игрока
        FindPlayer();
        
        if (debugLogs) Debug.Log($"[EnemyDash] Инициализирован на {gameObject.name}");
    }

    void Update()
    {
        // Если игрок не найден - пробуем найти снова
        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        // Не начинаем новый даш если: уже дашим/телеграфим, мертвы, или кулдаун
        if (isDashing || isTelegraphing) return;
        if (enemyHealth != null && enemyHealth.IsDead) return;
        if (!canDash) return;

        // Проверяем расстояние до игрока
        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Условие для рывка: игрок в "зоне рывка"
        if (distanceToPlayer >= minDashDistance && distanceToPlayer <= maxDashDistance)
        {
            // Проверяем кулдаун
            if (Time.time >= lastDashTime + dashCooldown)
            {
                if (debugLogs) Debug.Log($"[EnemyDash] Игрок в зоне! Дистанция: {distanceToPlayer:F1}");
                StartCoroutine(DashSequence());
            }
        }
    }

    // ===== ОСНОВНАЯ ЛОГИКА =====

    void FindPlayer()
    {
        // Сначала пробуем взять цель из EnemyAI
        if (enemyAI != null && enemyAI.playerTarget != null)
        {
            playerTarget = enemyAI.playerTarget;
            return;
        }

        // Если нет - ищем по тегу
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }
    }

    /// <summary>
    /// Полная последовательность: Телеграф → Рывок → Восстановление
    /// </summary>
    IEnumerator DashSequence()
    {
        // ===== ФАЗА 1: ТЕЛЕГРАФ (предупреждение) =====
        if (useTelegraph)
        {
            yield return StartCoroutine(TelegraphPhase());
        }
        else
        {
            // Если телеграф выключен - всё равно запоминаем направление
            dashDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        }

        // Проверяем, не умер ли враг во время телеграфа
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            ResetState();
            yield break;
        }

        // ===== ФАЗА 2: РЫВОК =====
        yield return StartCoroutine(DashPhase());

        // ===== ФАЗА 3: ВОССТАНОВЛЕНИЕ =====
        yield return StartCoroutine(RecoveryPhase());
    }

    /// <summary>
    /// Фаза телеграфа - враг замирает и "заряжается"
    /// </summary>
    IEnumerator TelegraphPhase()
    {
        isTelegraphing = true;
        
        if (debugLogs) Debug.Log($"[EnemyDash] Телеграф начат!");

        // Останавливаем AI - враг замирает
        if (enemyAI != null)
        {
            enemyAI.enabled = false;
        }
        
        // Останавливаем движение
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // ВАЖНО: Запоминаем направление к игроку СЕЙЧАС
        // Враг будет дашить туда, где игрок БЫЛ в момент телеграфа
        // Это даёт игроку возможность увернуться!
        dashDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;

        // Запускаем анимацию телеграфа
        if (animator != null)
        {
            animator.SetTrigger(telegraphTrigger);
        }

        // Звук телеграфа
        if (telegraphSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(telegraphSound, telegraphVolume);
        }

        // Визуальный эффект телеграфа
        if (blinkDuringTelegraph && spriteRenderer != null)
        {
            // Мигание
            yield return StartCoroutine(BlinkEffect(telegraphDuration));
        }
        else if (spriteRenderer != null)
        {
            // Просто меняем цвет
            spriteRenderer.color = telegraphColor;
            yield return new WaitForSeconds(telegraphDuration);
            spriteRenderer.color = originalColor;
        }
        else
        {
            yield return new WaitForSeconds(telegraphDuration);
        }

        isTelegraphing = false;
    }

    /// <summary>
    /// Эффект мигания во время телеграфа
    /// </summary>
    IEnumerator BlinkEffect(float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            // Синусоида для плавного мигания
            float t = Mathf.Sin(elapsed * blinkSpeed * Mathf.PI);
            
            // Интерполируем между оригинальным цветом и цветом телеграфа
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(originalColor, telegraphColor, Mathf.Abs(t));
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Возвращаем оригинальный цвет
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// Фаза рывка - враг летит вперёд
    /// </summary>
    IEnumerator DashPhase()
    {
        isDashing = true;
        lastDashTime = Time.time;
        
        if (debugLogs) Debug.Log($"[EnemyDash] РЫВОК! Направление: {dashDirection}");

        // Анимация рывка
        if (animator != null)
        {
            animator.SetTrigger(dashTrigger);
            animator.SetBool(isDashingBool, true);
        }

        // Звук рывка
        if (dashSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(dashSound, dashVolume);
        }

        // Визуальный эффект - меняем цвет
        if (useColorFlash && spriteRenderer != null)
        {
            spriteRenderer.color = dashColor;
        }

        // РЫВОК! Используем сохранённое направление (из телеграфа)
        if (rb != null)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }

        // Ждём окончания рывка
        yield return new WaitForSeconds(dashDuration);

        // Останавливаем врага
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Возвращаем цвет
        if (useColorFlash && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Выключаем анимацию рывка
        if (animator != null)
        {
            animator.SetBool(isDashingBool, false);
        }

        isDashing = false;
    }

    /// <summary>
    /// Фаза восстановления - враг возвращается к обычному поведению
    /// </summary>
    IEnumerator RecoveryPhase()
    {
        canDash = false;

        // Включаем AI обратно (если враг жив)
        if (enemyAI != null && enemyHealth != null && !enemyHealth.IsDead)
        {
            enemyAI.enabled = true;
        }
        else if (enemyAI != null && enemyHealth == null)
        {
            // Если нет компонента здоровья - просто включаем
            enemyAI.enabled = true;
        }

        if (debugLogs) Debug.Log($"[EnemyDash] Кулдаун {dashCooldown} сек...");

        // Ждём кулдаун
        yield return new WaitForSeconds(dashCooldown);
        
        canDash = true;
        
        if (debugLogs) Debug.Log($"[EnemyDash] Готов к новому рывку!");
    }

    // ===== ПУБЛИЧНЫЕ МЕТОДЫ =====

    /// <summary>
    /// Принудительно прерывает рывок (например, при смерти врага)
    /// </summary>
    public void InterruptDash()
    {
        if (isDashing || isTelegraphing)
        {
            StopAllCoroutines();
            ResetState();
            
            if (debugLogs) Debug.Log($"[EnemyDash] Рывок прерван!");
        }
    }

    /// <summary>
    /// Сбрасывает состояние в исходное
    /// </summary>
    private void ResetState()
    {
        isDashing = false;
        isTelegraphing = false;
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        if (animator != null)
        {
            animator.SetBool(isDashingBool, false);
        }
    }

    /// <summary>
    /// Проверка, выполняет ли враг рывок прямо сейчас
    /// </summary>
    public bool IsDashing()
    {
        return isDashing;
    }

    /// <summary>
    /// Проверка, находится ли враг в фазе телеграфа
    /// </summary>
    public bool IsTelegraphing()
    {
        return isTelegraphing;
    }

    /// <summary>
    /// Проверка, занят ли враг (телеграф или рывок)
    /// </summary>
    public bool IsBusy()
    {
        return isDashing || isTelegraphing;
    }

    // ===== ОТЛАДКА =====

    // Визуализация радиусов в редакторе
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Минимальная дистанция - красный круг
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDashDistance);

        // Максимальная дистанция - жёлтый круг
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDashDistance);

        // Направление последнего/текущего рывка - синяя линия
        if (dashDirection != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + dashDirection * 2f);
        }
    }
}
