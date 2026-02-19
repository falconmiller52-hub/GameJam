using UnityEngine;

/// <summary>
/// Менеджер применения улучшений.
/// 
/// КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Все способности (аура, электрошок, щит)
/// добавляются на объект PLAYER, а не на GameManager!
/// Раньше gameObject.AddComponent добавляло на себя (GameManager),
/// из-за чего способности были на неправильном объекте и не работали.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("=== ПРЕФАБЫ ЭФФЕКТОВ ===")]
    [Tooltip("Префаб визуала ауры (дочерний объект с SpriteRenderer)")]
    public GameObject redAuraPrefab;
    public GameObject electricShockPrefab;
    public GameObject fistsPrefab;

    [Header("=== ССЫЛКИ ===")]
    [Tooltip("Ссылка на объект игрока (если не назначена — найдётся по тегу Player)")]
    public GameObject playerObject;
    public PlayerUpgrades playerUpgrades;
    public WeaponSwitcher weaponSwitcher;

    private UpgradeSpawner upgradeSpawner;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        // 🔥 Находим игрока
        FindPlayer();

        upgradeSpawner = FindObjectOfType<UpgradeSpawner>();
    }

    /// <summary>
    /// Находит объект Player и все нужные компоненты на нём
    /// </summary>
    void FindPlayer()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogError("[UpgradeManager] Объект Player НЕ НАЙДЕН! Назначь вручную или добавь тег Player.");
                return;
            }
        }

        if (playerUpgrades == null)
            playerUpgrades = playerObject.GetComponent<PlayerUpgrades>();
        if (weaponSwitcher == null)
            weaponSwitcher = playerObject.GetComponent<WeaponSwitcher>();

        Debug.Log($"[UpgradeManager] Player найден: {playerObject.name}");
    }

    public void ApplyUpgrade(UpgradeType type, float value)
    {
        // На всякий случай — проверяем что Player найден
        if (playerObject == null) FindPlayer();
        if (playerObject == null)
        {
            Debug.LogError("[UpgradeManager] Невозможно применить улучшение — Player не найден!");
            return;
        }

        Debug.Log($"[UpgradeManager] Применяем: {type}, значение: {value}");

        switch (type)
        {
            // === СТАТ-АПГРЕЙДЫ ===
            case UpgradeType.Speed:
            case UpgradeType.Damage:
            case UpgradeType.AttackSpeed:
            case UpgradeType.MaxHealth:
                if (playerUpgrades != null)
                    playerUpgrades.ApplyUpgrade(type, value);
                else
                    Debug.LogWarning("[UpgradeManager] PlayerUpgrades не найден на Player!");
                break;

            // === УНИКАЛЬНЫЕ СПОСОБНОСТИ ===
            case UpgradeType.RedAura:
                ApplyRedAura();
                MarkAbilityObtained(type);
                break;

            case UpgradeType.ElectricShock:
                ApplyElectricShock();
                MarkAbilityObtained(type);
                break;

            case UpgradeType.Shield:
                ApplyShield((int)value);
                MarkAbilityObtained(type);
                break;

            case UpgradeType.Fists:
                ApplyFists();
                MarkAbilityObtained(type);
                break;
        }
    }

    void MarkAbilityObtained(UpgradeType type)
    {
        if (upgradeSpawner == null) upgradeSpawner = FindObjectOfType<UpgradeSpawner>();
        if (upgradeSpawner != null) upgradeSpawner.MarkAsObtained(type);
    }

    // ==================== КРАСНАЯ АУРА ====================

    void ApplyRedAura()
    {
        // 🔥 Ищем на PLAYER, не на себе!
        RedAura existing = playerObject.GetComponent<RedAura>();

        if (existing != null)
        {
            existing.Upgrade(0.5f, 1f);
            Debug.Log("[UpgradeManager] Красная Аура улучшена!");
        }
        else
        {
            if (redAuraPrefab != null)
            {
                // Создаём как дочерний объект Player
                GameObject obj = Instantiate(redAuraPrefab, playerObject.transform);
                obj.transform.localPosition = Vector3.zero;
                Debug.Log("[UpgradeManager] Красная Аура создана из префаба на Player!");
            }
            else
            {
                // Добавляем компонент прямо на Player
                playerObject.AddComponent<RedAura>();
                Debug.Log("[UpgradeManager] Красная Аура добавлена на Player как компонент!");
            }
        }
    }

    // ==================== ЭЛЕКТРОШОК ====================

    void ApplyElectricShock()
    {
        ElectricShock existing = playerObject.GetComponent<ElectricShock>();

        if (existing != null)
        {
            existing.Upgrade(1, 1);
            Debug.Log("[UpgradeManager] Электрошок улучшен!");
        }
        else
        {
            if (electricShockPrefab != null)
            {
                GameObject obj = Instantiate(electricShockPrefab, playerObject.transform);
                obj.transform.localPosition = Vector3.zero;
                Debug.Log("[UpgradeManager] Электрошок создан из префаба на Player!");
            }
            else
            {
                playerObject.AddComponent<ElectricShock>();
                Debug.Log("[UpgradeManager] Электрошок добавлен на Player как компонент!");
            }
        }
    }

    // ==================== ЩИТ ====================

    void ApplyShield(int val)
    {
        // 🔥 Ищем на PLAYER!
        PlayerShield existing = playerObject.GetComponent<PlayerShield>();

        if (existing != null)
        {
            existing.Upgrade(val);
            Debug.Log("[UpgradeManager] Щит улучшен!");
        }
        else
        {
            // 🔥 Добавляем на PLAYER!
            PlayerShield shield = playerObject.AddComponent<PlayerShield>();
            shield.maxShield = val > 0 ? val : 1;
            shield.currentShield = shield.maxShield;
            Debug.Log($"[UpgradeManager] Щит добавлен на Player! maxShield={shield.maxShield}");
        }
    }

    // ==================== КУЛАКИ ====================

    void ApplyFists()
    {
        if (weaponSwitcher == null)
            weaponSwitcher = playerObject.GetComponent<WeaponSwitcher>();

        if (weaponSwitcher != null)
        {
            if (weaponSwitcher.fistsWeapon != null)
            {
                weaponSwitcher.UnlockFists();
                Debug.Log("[UpgradeManager] Кулаки разблокированы!");
            }
            else if (fistsPrefab != null)
            {
                GameObject obj = Instantiate(fistsPrefab, playerObject.transform);
                weaponSwitcher.fistsWeapon = obj;
                weaponSwitcher.UnlockFists();
                Debug.Log("[UpgradeManager] Кулаки созданы и разблокированы!");
            }
            else
            {
                Debug.LogWarning("[UpgradeManager] Префаб кулаков не назначен и fistsWeapon пустой!");
            }
        }
        else
        {
            Debug.LogWarning("[UpgradeManager] WeaponSwitcher не найден на Player!");
        }
    }

    // ==================== ПРОВЕРКА ====================

    public bool HasUpgrade(UpgradeType type)
    {
        if (playerObject == null) return false;

        switch (type)
        {
            case UpgradeType.RedAura: return playerObject.GetComponent<RedAura>() != null;
            case UpgradeType.ElectricShock: return playerObject.GetComponent<ElectricShock>() != null;
            case UpgradeType.Shield: return playerObject.GetComponent<PlayerShield>() != null;
            case UpgradeType.Fists: return weaponSwitcher != null && weaponSwitcher.fistsUnlocked;
            default: return false;
        }
    }
}
