using UnityEngine;

/// <summary>
/// Система прокачки игрока.
/// Хранит все бонусы и применяет их к характеристикам.
/// </summary>
public class PlayerUpgrades : MonoBehaviour
{
    public static PlayerUpgrades Instance { get; private set; }

    [Header("=== ТЕКУЩИЕ БОНУСЫ ===")]
    [Tooltip("Бонус к скорости (множитель)")]
    public float speedMultiplier = 1f;
    
    [Tooltip("Бонус к урону (добавка)")]
    public int damageBonus = 0;
    
    [Tooltip("Бонус к скорости атаки (множитель)")]
    public float attackSpeedMultiplier = 1f;
    
    [Tooltip("Бонус к максимальному здоровью")]
    public int maxHealthBonus = 0;

    [Header("=== ССЫЛКИ НА КОМПОНЕНТЫ ===")]
    public PlayerMovement playerMovement;
    public PlayerHealth playerHealth;
    public SwordDamage swordDamage;
    public PlayerAttack playerAttack;

    [Header("=== БАЗОВЫЕ ЗНАЧЕНИЯ (сохраняются при старте) ===")]
    private float baseSpeed;
    private int baseDamage;
    private float baseAttackRate;
    private int baseMaxHealth;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Автопоиск компонентов
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();
        
        if (playerAttack == null)
            playerAttack = GetComponent<PlayerAttack>();
        
        if (swordDamage == null)
            swordDamage = GetComponentInChildren<SwordDamage>();

        // Сохраняем базовые значения
        SaveBaseStats();
    }

    void SaveBaseStats()
    {
        if (playerMovement != null)
            baseSpeed = playerMovement.moveSpeed;
        
        if (swordDamage != null)
            baseDamage = swordDamage.damageAmount;
        
        if (playerAttack != null)
            baseAttackRate = playerAttack.attackRate;
        
        if (playerHealth != null)
            baseMaxHealth = playerHealth.maxHealth;

        Debug.Log($"[PlayerUpgrades] Базовые статы сохранены: Speed={baseSpeed}, Damage={baseDamage}");
    }

    /// <summary>
    /// Применяет улучшение определённого типа
    /// </summary>
    public void ApplyUpgrade(UpgradeType type, float value)
    {
        switch (type)
        {
            case UpgradeType.Speed:
                speedMultiplier += value;
                break;
            
            case UpgradeType.Damage:
                damageBonus += (int)value;
                break;
            
            case UpgradeType.AttackSpeed:
                attackSpeedMultiplier += value;
                break;
            
            case UpgradeType.MaxHealth:
                maxHealthBonus += (int)value;
                break;
        }

        // Применяем к компонентам
        ApplyAllBonuses();

        Debug.Log($"[PlayerUpgrades] Применено улучшение: {type} +{value}");
    }

    /// <summary>
    /// Применяет все накопленные бонусы к компонентам игрока
    /// </summary>
    void ApplyAllBonuses()
    {
        // Скорость
        if (playerMovement != null)
        {
            playerMovement.moveSpeed = baseSpeed * speedMultiplier;
        }

        // Урон
        if (swordDamage != null)
        {
            swordDamage.damageAmount = baseDamage + damageBonus;
        }

        // Скорость атаки (меньше = быстрее)
        if (playerAttack != null)
        {
            playerAttack.attackRate = baseAttackRate / attackSpeedMultiplier;
        }

        // Максимальное здоровье
        if (playerHealth != null)
        {
            int oldMax = playerHealth.maxHealth;
            playerHealth.maxHealth = baseMaxHealth + maxHealthBonus;
            
            // Восстанавливаем добавленное здоровье
            int healthDiff = playerHealth.maxHealth - oldMax;
            if (healthDiff > 0)
            {
                playerHealth.currentHealth += healthDiff;
            }
        }
    }

    /// <summary>
    /// Сбрасывает все улучшения (для нового прохождения)
    /// </summary>
    public void ResetUpgrades()
    {
        speedMultiplier = 1f;
        damageBonus = 0;
        attackSpeedMultiplier = 1f;
        maxHealthBonus = 0;

        ApplyAllBonuses();

        Debug.Log("[PlayerUpgrades] Все улучшения сброшены");
    }

    /// <summary>
    /// Возвращает описание текущих бонусов
    /// </summary>
    public string GetStatsDescription()
    {
        return $"Скорость: x{speedMultiplier:F1}\n" +
               $"Урон: +{damageBonus}\n" +
               $"Скорость атаки: x{attackSpeedMultiplier:F1}\n" +
               $"Макс. здоровье: +{maxHealthBonus}";
    }
}

/// <summary>
/// Типы улучшений
/// </summary>
public enum UpgradeType
{
    // Базовые улучшения статов
    Speed,          // Увеличение скорости передвижения
    Damage,         // Увеличение урона
    AttackSpeed,    // Увеличение скорости атаки
    MaxHealth,      // Увеличение максимального здоровья
    
    // Особые улучшения
    RedAura,        // Красная аура — урон врагам вокруг
    ElectricShock,  // Электрошок — цепная молния
    Shield,         // Защитный щит
    Fists,          // Оружие "Кулаки"
    
    // Добавляй новые типы сюда!
}
