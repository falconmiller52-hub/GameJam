using UnityEngine;

/// <summary>
/// Менеджер применения улучшений.
/// Создаёт и настраивает компоненты улучшений на игроке.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("=== ПРЕФАБЫ ЭФФЕКТОВ ===")]
    public GameObject redAuraPrefab;
    public GameObject electricShockPrefab;
    public GameObject fistsPrefab;

    [Header("=== ССЫЛКИ ===")]
    public PlayerUpgrades playerUpgrades;
    public WeaponSwitcher weaponSwitcher;
    public PlayerShield playerShield;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (playerUpgrades == null)
            playerUpgrades = GetComponent<PlayerUpgrades>();
        
        if (weaponSwitcher == null)
            weaponSwitcher = GetComponent<WeaponSwitcher>();
    }

    /// <summary>
    /// Применяет улучшение по типу
    /// </summary>
    public void ApplyUpgrade(UpgradeType type, float value)
    {
        switch (type)
        {
            case UpgradeType.Speed:
            case UpgradeType.Damage:
            case UpgradeType.AttackSpeed:
            case UpgradeType.MaxHealth:
                // Обычные улучшения — через PlayerUpgrades
                if (playerUpgrades != null)
                    playerUpgrades.ApplyUpgrade(type, value);
                break;

            case UpgradeType.RedAura:
                ApplyRedAura();
                break;

            case UpgradeType.ElectricShock:
                ApplyElectricShock();
                break;

            case UpgradeType.Shield:
                ApplyShield((int)value);
                break;

            case UpgradeType.Fists:
                ApplyFists();
                break;
        }
    }

    void ApplyRedAura()
    {
        // Проверяем, есть ли уже аура
        RedAura existingAura = GetComponent<RedAura>();
        
        if (existingAura != null)
        {
            // Улучшаем существующую
            existingAura.Upgrade(0.5f, 1f);
            Debug.Log("[UpgradeManager] Красная Аура улучшена!");
        }
        else
        {
            // Создаём новую
            if (redAuraPrefab != null)
            {
                GameObject auraObj = Instantiate(redAuraPrefab, transform);
                auraObj.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Добавляем компонент напрямую
                gameObject.AddComponent<RedAura>();
            }
            Debug.Log("[UpgradeManager] Красная Аура добавлена!");
        }
    }

    void ApplyElectricShock()
    {
        ElectricShock existingShock = GetComponent<ElectricShock>();
        
        if (existingShock != null)
        {
            existingShock.Upgrade(1, 1);
            Debug.Log("[UpgradeManager] Электрошок улучшен!");
        }
        else
        {
            if (electricShockPrefab != null)
            {
                GameObject shockObj = Instantiate(electricShockPrefab, transform);
                shockObj.transform.localPosition = Vector3.zero;
            }
            else
            {
                gameObject.AddComponent<ElectricShock>();
            }
            Debug.Log("[UpgradeManager] Электрошок добавлен!");
        }
    }

    void ApplyShield(int shieldValue)
    {
        if (playerShield == null)
            playerShield = GetComponent<PlayerShield>();

        if (playerShield != null)
        {
            playerShield.Upgrade(shieldValue);
            Debug.Log("[UpgradeManager] Щит улучшен!");
        }
        else
        {
            playerShield = gameObject.AddComponent<PlayerShield>();
            playerShield.maxShield = shieldValue > 0 ? shieldValue : 3;
            playerShield.currentShield = playerShield.maxShield;
            Debug.Log("[UpgradeManager] Щит добавлен!");
        }
    }

    void ApplyFists()
    {
        if (weaponSwitcher == null)
            weaponSwitcher = GetComponent<WeaponSwitcher>();

        if (weaponSwitcher != null)
        {
            // Активируем кулаки
            if (weaponSwitcher.fistsWeapon != null)
            {
                weaponSwitcher.UnlockFists();
                Debug.Log("[UpgradeManager] Кулаки разблокированы!");
            }
            else
            {
                // Создаём кулаки если префаб есть
                if (fistsPrefab != null)
                {
                    GameObject fistsObj = Instantiate(fistsPrefab, transform);
                    weaponSwitcher.fistsWeapon = fistsObj;
                    weaponSwitcher.UnlockFists();
                    Debug.Log("[UpgradeManager] Кулаки созданы и разблокированы!");
                }
                else
                {
                    Debug.LogWarning("[UpgradeManager] Префаб кулаков не назначен!");
                }
            }
        }
        else
        {
            Debug.LogWarning("[UpgradeManager] WeaponSwitcher не найден!");
        }
    }

    /// <summary>
    /// Проверяет, есть ли у игрока определённое улучшение
    /// </summary>
    public bool HasUpgrade(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.RedAura:
                return GetComponent<RedAura>() != null;
            
            case UpgradeType.ElectricShock:
                return GetComponent<ElectricShock>() != null;
            
            case UpgradeType.Shield:
                return GetComponent<PlayerShield>() != null;
            
            case UpgradeType.Fists:
                return weaponSwitcher != null && weaponSwitcher.fistsUnlocked;
            
            default:
                return false;
        }
    }
}
