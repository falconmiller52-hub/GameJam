using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Компонент прыжковой атаки для врагов типа "Голем".
/// Враг прыгает к игроку и наносит урон по области при приземлении.
/// 
/// МЕХАНИКА:
/// 1. Игрок входит в зону атаки
/// 2. Появляется индикатор области (где упадёт голем)
/// 3. Голем готовится к прыжку (телеграф)
/// 4. Голем прыгает в отмеченную точку
/// 5. При приземлении — урон всем в радиусе
/// </summary>
public class EnemyJumpAttack : MonoBehaviour
{
    [Header("=== ДИСТАНЦИЯ АТАКИ ===")]
    [Tooltip("Минимальное расстояние для прыжка (не прыгает вплотную)")]
    public float minJumpDistance = 2f;
    
    [Tooltip("Максимальное расстояние для прыжка")]
    public float maxJumpDistance = 7f;

    [Header("=== ПАРАМЕТРЫ ПРЫЖКА ===")]
    [Tooltip("Высота прыжка (визуальная)")]
    public float jumpHeight = 2f;
    
    [Tooltip("Длительность прыжка в секундах")]
    public float jumpDuration = 0.6f;
    
    [Tooltip("Время перезарядки между прыжками")]
    public float jumpCooldown = 4f;
    
    [Tooltip("Время 'оглушения' после приземления (голем стоит на месте)")]
    public float landingStunDuration = 1.0f;

    [Header("=== ТЕЛЕГРАФ (подготовка) ===")]
    [Tooltip("Длительность подготовки перед прыжком")]
    public float telegraphDuration = 0.8f;
    
    [Tooltip("Цвет голема во время подготовки")]
    public Color telegraphColor = new Color(1f, 0.8f, 0.3f, 1f); // Жёлто-оранжевый
    
    [Tooltip("Тряска во время подготовки")]
    public bool shakeOnTelegraph = true;
    
    [Tooltip("Интенсивность тряски")]
    public float shakeIntensity = 0.1f;

    [Header("=== УРОН ПО ОБЛАСТИ ===")]
    [Tooltip("Радиус урона при приземлении")]
    public float damageRadius = 1.5f;
    
    [Tooltip("Урон игроку")]
    public int damageAmount = 2;
    
    [Tooltip("Сила отбрасывания")]
    public float knockbackForce = 8f;

    [Header("=== ИНДИКАТОР ОБЛАСТИ ===")]
    [Tooltip("Префаб индикатора области (круг на земле)")]
    public GameObject areaIndicatorPrefab;
    
    [Tooltip("Цвет индикатора (предупреждение)")]
    public Color indicatorWarningColor = new Color(1f, 1f, 0f, 0.5f); // Жёлтый полупрозрачный
    
    [Tooltip("Цвет индикатора (опасность!)")]
    public Color indicatorDangerColor = new Color(1f, 0f, 0f, 0.7f); // Красный
    
    [Tooltip("Время мигания индикатора перед ударом")]
    public float indicatorBlinkTime = 0.3f;

    [Header("=== ВИЗУАЛЬНЫЕ ЭФФЕКТЫ ===")]
    [Tooltip("Цвет голема в воздухе")]
    public Color jumpColor = new Color(0.8f, 0.4f, 0.1f, 1f); // Коричнево-оранжевый
    
    [Tooltip("Префаб эффекта приземления (пыль, волна и т.д.)")]
    public GameObject landingEffectPrefab;
    
    [Tooltip("Добавить тень под големом во время прыжка?")]
    public bool showJumpShadow = true;
    
    [Tooltip("Префаб тени (простой тёмный круг)")]
    public GameObject shadowPrefab;

    [Header("=== АНИМАЦИЯ ===")]
    [Tooltip("Ссылка на Animator")]
    public Animator animator;
    
    [Tooltip("Триггер подготовки к прыжку")]
    public string telegraphTrigger = "JumpTelegraph";
    
    [Tooltip("Триггер самого прыжка")]
    public string jumpTrigger = "Jump";
    
    [Tooltip("Триггер приземления")]
    public string landTrigger = "Land";
    
    [Tooltip("Bool: в воздухе")]
    public string isAirborneBool = "IsAirborne";

    [Header("=== АУДИО ===")]
    [Tooltip("Звук подготовки (рычание, зарядка)")]
    public AudioClip telegraphSound;
    [Range(0f, 1f)]
    public float telegraphVolume = 0.6f;
    
    [Tooltip("Звук прыжка (отрыв от земли)")]
    public AudioClip jumpSound;
    [Range(0f, 1f)]
    public float jumpVolume = 0.7f;
    
    [Tooltip("Звук приземления (удар, землетрясение)")]
    public AudioClip landSound;
    [Range(0f, 1f)]
    public float landVolume = 1f;

    [Header("=== ОТЛАДКА ===")]
    public bool showDebugGizmos = true;
    public bool debugLogs = false;

    // ===== ПРИВАТНЫЕ ПЕРЕМЕННЫЕ =====
    private Transform playerTarget;
    private Rigidbody2D rb;
    private EnemyAI enemyAI;
    private EnemyHealth enemyHealth;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private bool isJumping = false;
    private bool isTelegraphing = false;
    private bool canJump = true;
    private float lastJumpTime = -999f;
    private Color originalColor;
    private Vector3 originalPosition;
    private Vector2 targetPosition; // Куда прыгаем
    
    private GameObject currentIndicator; // Текущий индикатор области
    private GameObject currentShadow;    // Тень под големом

    // ===== UNITY CALLBACKS =====

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyAI = GetComponent<EnemyAI>();
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

        FindPlayer();
        
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] Голем {gameObject.name} готов!");
    }

    void Update()
    {
        if (playerTarget == null)
        {
            FindPlayer();
            return;
        }

        // Не начинаем новый прыжок если заняты
        if (isJumping || isTelegraphing) return;
        if (enemyHealth != null && enemyHealth.IsDead) return;
        if (!canJump) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTarget.position);

        // Игрок в зоне атаки?
        if (distanceToPlayer >= minJumpDistance && distanceToPlayer <= maxJumpDistance)
        {
            if (Time.time >= lastJumpTime + jumpCooldown)
            {
                if (debugLogs) Debug.Log($"[EnemyJumpAttack] Игрок в зоне! Начинаем прыжок...");
                StartCoroutine(JumpAttackSequence());
            }
        }
    }

    // ===== ОСНОВНАЯ ЛОГИКА =====

    void FindPlayer()
    {
        if (enemyAI != null && enemyAI.playerTarget != null)
        {
            playerTarget = enemyAI.playerTarget;
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTarget = player.transform;
    }

    /// <summary>
    /// Полная последовательность прыжковой атаки
    /// </summary>
    IEnumerator JumpAttackSequence()
    {
        // ===== ФАЗА 1: ТЕЛЕГРАФ =====
        yield return StartCoroutine(TelegraphPhase());

        // Проверка на смерть
        if (enemyHealth != null && enemyHealth.IsDead)
        {
            CleanupIndicators();
            ResetState();
            yield break;
        }

        // ===== ФАЗА 2: ПРЫЖОК =====
        yield return StartCoroutine(JumpPhase());

        // ===== ФАЗА 3: ПРИЗЕМЛЕНИЕ И УРОН =====
        yield return StartCoroutine(LandingPhase());

        // ===== ФАЗА 4: ВОССТАНОВЛЕНИЕ =====
        yield return StartCoroutine(RecoveryPhase());
    }

    /// <summary>
    /// Фаза подготовки — голем трясётся, появляется индикатор
    /// </summary>
    IEnumerator TelegraphPhase()
    {
        isTelegraphing = true;
        
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] Телеграф!");

        // Останавливаем AI
        if (enemyAI != null)
            enemyAI.enabled = false;
        
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // ФИКСИРУЕМ ЦЕЛЬ — где был игрок в этот момент
        targetPosition = playerTarget.position;
        originalPosition = transform.position;

        // Создаём индикатор области
        SpawnAreaIndicator(targetPosition);

        // Анимация телеграфа
        if (animator != null)
            animator.SetTrigger(telegraphTrigger);

        // Звук подготовки
        if (telegraphSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(telegraphSound, telegraphVolume);
        }

        // Визуальный эффект: тряска + смена цвета
        float elapsed = 0f;
        Vector3 originalLocalPos = transform.localPosition;
        
        while (elapsed < telegraphDuration)
        {
            // Смена цвета
            if (spriteRenderer != null)
            {
                float t = Mathf.PingPong(elapsed * 8f, 1f);
                spriteRenderer.color = Color.Lerp(originalColor, telegraphColor, t);
            }

            // Тряска
            if (shakeOnTelegraph)
            {
                float shakeX = Random.Range(-shakeIntensity, shakeIntensity);
                float shakeY = Random.Range(-shakeIntensity, shakeIntensity);
                transform.localPosition = originalLocalPos + new Vector3(shakeX, shakeY, 0);
            }

            // Индикатор постепенно становится красным
            if (currentIndicator != null)
            {
                SpriteRenderer indicatorSR = currentIndicator.GetComponent<SpriteRenderer>();
                if (indicatorSR != null)
                {
                    float progress = elapsed / telegraphDuration;
                    indicatorSR.color = Color.Lerp(indicatorWarningColor, indicatorDangerColor, progress);
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Возвращаем позицию после тряски
        transform.localPosition = originalLocalPos;
        
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        isTelegraphing = false;
    }

    /// <summary>
    /// Фаза прыжка — голем летит по дуге к цели
    /// </summary>
    IEnumerator JumpPhase()
    {
        isJumping = true;
        lastJumpTime = Time.time;
        
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] ПРЫЖОК к {targetPosition}!");

        // Анимация прыжка
        if (animator != null)
        {
            animator.SetTrigger(jumpTrigger);
            animator.SetBool(isAirborneBool, true);
        }

        // Звук прыжка
        if (jumpSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(jumpSound, jumpVolume);
        }

        // Цвет в воздухе
        if (spriteRenderer != null)
            spriteRenderer.color = jumpColor;

        // Создаём тень
        if (showJumpShadow)
            SpawnShadow(targetPosition);

        // Отключаем коллайдер во время прыжка (голем в воздухе)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        // Прыжок по параболе
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            
            // Линейная интерполяция по X и Y
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            
            // Парабола для высоты (эффект прыжка)
            // Формула: 4 * h * t * (1 - t) даёт параболу с максимумом h в середине
            float heightOffset = 4f * jumpHeight * t * (1f - t);
            currentPos.y += heightOffset;
            
            transform.position = currentPos;

            // Обновляем позицию тени (тень остаётся на земле)
            if (currentShadow != null)
            {
                // Тень уменьшается когда голем высоко
                float shadowScale = 1f - (heightOffset / jumpHeight) * 0.5f;
                currentShadow.transform.localScale = Vector3.one * shadowScale * damageRadius;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Финальная позиция
        transform.position = endPos;

        // Включаем коллайдер обратно
        if (col != null)
            col.enabled = true;
    }

    /// <summary>
    /// Фаза приземления — удар по области, урон
    /// </summary>
    IEnumerator LandingPhase()
    {
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] ПРИЗЕМЛЕНИЕ!");

        // 🔥 ФИКСИРУЕМ ПОЗИЦИЮ ПРИЗЕМЛЕНИЯ
        // Это предотвращает "проваливание" из-за анимации
        Vector3 landingPosition = transform.position;

        // Анимация приземления
        if (animator != null)
        {
            animator.SetBool(isAirborneBool, false);
            animator.SetTrigger(landTrigger);
        }

        // 🔥 Принудительно возвращаем позицию (на случай если анимация сдвинула)
        transform.position = landingPosition;

        // Звук приземления
        if (landSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(landSound, landVolume);
        }

        // Возвращаем цвет
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        // Эффект приземления (пыль, волна)
        if (landingEffectPrefab != null)
        {
            GameObject effect = Instantiate(landingEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // УРОН ПО ОБЛАСТИ!
        DealAreaDamage();

        // Мигание индикатора перед исчезновением
        if (currentIndicator != null)
        {
            StartCoroutine(BlinkAndDestroyIndicator());
        }

        // Убираем тень
        if (currentShadow != null)
        {
            Destroy(currentShadow);
            currentShadow = null;
        }

        // 🔥 СТАН ПОСЛЕ ПРИЗЕМЛЕНИЯ — голем стоит на месте!
        // Это даёт игроку окно для контратаки
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] Стан {landingStunDuration} сек...");
        
        // Убеждаемся что голем не двигается во время стана
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        
        // 🔥 ФИКСАЦИЯ ПОЗИЦИИ ВО ВРЕМЯ СТАНА
        // Каждый кадр возвращаем голема на место, чтобы анимация не сдвигала его
        float stunElapsed = 0f;
        while (stunElapsed < landingStunDuration)
        {
            transform.position = landingPosition;  // Держим на месте!
            
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            
            stunElapsed += Time.deltaTime;
            yield return null;
        }

        isJumping = false;
    }

    /// <summary>
    /// Наносит урон всем в радиусе
    /// </summary>
    void DealAreaDamage()
    {
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] Проверяем урон в радиусе {damageRadius}");

        // Находим всех в радиусе
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, damageRadius);

        foreach (Collider2D hit in hits)
        {
            // Не бьём себя
            if (hit.gameObject == gameObject) continue;

            // Проверяем игрока
            if (hit.CompareTag("Player"))
            {
                PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damageAmount);
                    if (debugLogs) Debug.Log($"[EnemyJumpAttack] Нанесли {damageAmount} урона игроку!");
                }

                // Отбрасывание
                Rigidbody2D playerRB = hit.GetComponent<Rigidbody2D>();
                if (playerRB != null && knockbackForce > 0)
                {
                    Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
                    playerRB.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    /// <summary>
    /// Фаза восстановления — AI включается только здесь, ПОСЛЕ стана
    /// </summary>
    IEnumerator RecoveryPhase()
    {
        canJump = false;

        // 🔥 AI включается только ПОСЛЕ стана (который был в LandingPhase)
        // Теперь голем начнёт двигаться к игроку только здесь
        if (enemyAI != null && (enemyHealth == null || !enemyHealth.IsDead))
        {
            enemyAI.enabled = true;
            if (debugLogs) Debug.Log($"[EnemyJumpAttack] AI включен, голем снова двигается");
        }

        if (debugLogs) Debug.Log($"[EnemyJumpAttack] Кулдаун прыжка {jumpCooldown} сек...");

        yield return new WaitForSeconds(jumpCooldown);
        
        canJump = true;
        
        if (debugLogs) Debug.Log($"[EnemyJumpAttack] Готов к новому прыжку!");
    }

    // ===== ИНДИКАТОР ОБЛАСТИ =====

    /// <summary>
    /// Создаёт индикатор области на земле
    /// </summary>
    void SpawnAreaIndicator(Vector2 position)
    {
        // Удаляем старый если есть
        if (currentIndicator != null)
            Destroy(currentIndicator);

        if (areaIndicatorPrefab != null)
        {
            // Используем префаб
            currentIndicator = Instantiate(areaIndicatorPrefab, position, Quaternion.identity);
            currentIndicator.transform.localScale = Vector3.one * damageRadius * 2f;
        }
        else
        {
            // Создаём простой круг программно
            currentIndicator = CreateSimpleCircle(position, damageRadius, indicatorWarningColor);
        }
    }

    /// <summary>
    /// Создаёт простой круг-индикатор если нет префаба
    /// </summary>
    GameObject CreateSimpleCircle(Vector2 position, float radius, Color color)
    {
        GameObject circle = new GameObject("AreaIndicator");
        circle.transform.position = new Vector3(position.x, position.y, 0.1f); // Чуть ниже врагов
        
        SpriteRenderer sr = circle.AddComponent<SpriteRenderer>();
        
        // Создаём круглый спрайт программно
        Texture2D texture = new Texture2D(128, 128);
        Color[] colors = new Color[128 * 128];
        
        Vector2 center = new Vector2(64, 64);
        float maxDist = 64f;
        
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < maxDist)
                {
                    // Градиент от центра к краям
                    float alpha = 1f - (dist / maxDist);
                    colors[y * 128 + x] = new Color(1f, 1f, 1f, alpha * 0.8f);
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
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = -1; // Под врагами
        
        circle.transform.localScale = Vector3.one * radius * 2f;
        
        return circle;
    }

    /// <summary>
    /// Мигание и удаление индикатора
    /// </summary>
    IEnumerator BlinkAndDestroyIndicator()
    {
        if (currentIndicator == null) yield break;

        SpriteRenderer sr = currentIndicator.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
            yield break;
        }

        // Быстрое мигание
        float elapsed = 0f;
        while (elapsed < indicatorBlinkTime)
        {
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }

        Destroy(currentIndicator);
        currentIndicator = null;
    }

    /// <summary>
    /// Создаёт тень под големом
    /// </summary>
    void SpawnShadow(Vector2 position)
    {
        if (currentShadow != null)
            Destroy(currentShadow);

        if (shadowPrefab != null)
        {
            currentShadow = Instantiate(shadowPrefab, position, Quaternion.identity);
        }
        else
        {
            // Простая тень
            currentShadow = CreateSimpleCircle(position, damageRadius * 0.5f, new Color(0, 0, 0, 0.3f));
            currentShadow.name = "JumpShadow";
        }
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

    void CleanupIndicators()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
        if (currentShadow != null)
        {
            Destroy(currentShadow);
            currentShadow = null;
        }
    }

    void ResetState()
    {
        isJumping = false;
        isTelegraphing = false;
        
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
        
        if (animator != null)
            animator.SetBool(isAirborneBool, false);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;
    }

    /// <summary>
    /// Прерывает прыжок (при смерти)
    /// </summary>
    public void InterruptJump()
    {
        if (isJumping || isTelegraphing)
        {
            StopAllCoroutines();
            CleanupIndicators();
            ResetState();
            
            if (debugLogs) Debug.Log($"[EnemyJumpAttack] Прыжок прерван!");
        }
    }

    public bool IsJumping() => isJumping;
    public bool IsTelegraphing() => isTelegraphing;
    public bool IsBusy() => isJumping || isTelegraphing;

    // ===== ОТЛАДКА =====

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Минимальная дистанция - красный
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minJumpDistance);

        // Максимальная дистанция - жёлтый
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxJumpDistance);

        // Радиус урона - магента
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, damageRadius);

        // Цель прыжка (если в процессе)
        if (targetPosition != Vector2.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, (Vector3)targetPosition);
            Gizmos.DrawWireSphere((Vector3)targetPosition, 0.3f);
        }
    }
}