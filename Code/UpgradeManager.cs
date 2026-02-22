using UnityEngine;

/// <summary>
/// Менеджер применения улучшений. Все способности добавляются на объект PLAYER.
/// 
/// ИСПРАВЛЕНО: При создании PlayerShield через AddComponent — передаёт ему
/// ссылки на спрайты и ShieldIcon из заранее настроенного префаба.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("=== ПРЕФАБЫ ЭФФЕКТОВ ===")]
    public GameObject redAuraPrefab;
    public GameObject electricShockPrefab;
    public GameObject fistsPrefab;

    [Header("=== НАСТРОЙКИ ЩИТА ===")]
    [Tooltip("Префаб Shield из папки Prefabs/Upgrades (для копирования настроек)")]
    public GameObject shieldSettingsPrefab;

    [Header("=== ССЫЛКИ ===")]
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
        FindPlayer();
        upgradeSpawner = FindObjectOfType<UpgradeSpawner>();
    }

    void FindPlayer()
    {
        if (playerObject == null)
        {
            playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                Debug.LogError("[UpgradeManager] Player НЕ НАЙДЕН!");
                return;
            }
        }
        if (playerUpgrades == null) playerUpgrades = playerObject.GetComponent<PlayerUpgrades>();
        if (weaponSwitcher == null) weaponSwitcher = playerObject.GetComponent<WeaponSwitcher>();
    }

    public void ApplyUpgrade(UpgradeType type, float value)
    {
        if (playerObject == null) FindPlayer();
        if (playerObject == null) return;

        Debug.Log($"[UpgradeManager] Применяем: {type}, значение: {value}");

        switch (type)
        {
            case UpgradeType.Speed:
            case UpgradeType.Damage:
            case UpgradeType.AttackSpeed:
            case UpgradeType.MaxHealth:
                if (playerUpgrades != null) playerUpgrades.ApplyUpgrade(type, value);
                break;
            case UpgradeType.RedAura:
                ApplyRedAura(); MarkAbilityObtained(type); break;
            case UpgradeType.ElectricShock:
                ApplyElectricShock(); MarkAbilityObtained(type); break;
            case UpgradeType.Shield:
                ApplyShield((int)value); MarkAbilityObtained(type); break;
            case UpgradeType.Fists:
                ApplyFists(); MarkAbilityObtained(type); break;
        }
    }

    void MarkAbilityObtained(UpgradeType type)
    {
        if (upgradeSpawner == null) upgradeSpawner = FindObjectOfType<UpgradeSpawner>();
        if (upgradeSpawner != null) upgradeSpawner.MarkAsObtained(type);
    }

    void ApplyRedAura()
    {
        RedAura existing = playerObject.GetComponent<RedAura>();
        if (existing != null) { existing.Upgrade(0.5f, 1f); return; }

        if (redAuraPrefab != null)
        {
            GameObject obj = Instantiate(redAuraPrefab, playerObject.transform);
            obj.transform.localPosition = Vector3.zero;
        }
        else playerObject.AddComponent<RedAura>();
    }

    void ApplyElectricShock()
    {
        ElectricShock existing = playerObject.GetComponent<ElectricShock>();
        if (existing != null) { existing.Upgrade(1, 1); return; }

        if (electricShockPrefab != null)
        {
            GameObject obj = Instantiate(electricShockPrefab, playerObject.transform);
            obj.transform.localPosition = Vector3.zero;
        }
        else playerObject.AddComponent<ElectricShock>();
    }

    void ApplyShield(int val)
    {
        PlayerShield existing = playerObject.GetComponent<PlayerShield>();
        if (existing != null) { existing.Upgrade(val); return; }

        // 🔥 Добавляем компонент на Player
        PlayerShield shield = playerObject.AddComponent<PlayerShield>();
        shield.maxShield = val > 0 ? val : 1;
        shield.currentShield = shield.maxShield;

        // 🔥 КЛЮЧЕВОЕ: копируем настройки спрайтов из префаба
        if (shieldSettingsPrefab != null)
        {
            PlayerShield prefabShield = shieldSettingsPrefab.GetComponent<PlayerShield>();
            if (prefabShield != null)
            {
                shield.shieldFullSprite = prefabShield.shieldFullSprite;
                shield.shieldBrokenSprite = prefabShield.shieldBrokenSprite;
                shield.shockwaveEffectPrefab = prefabShield.shockwaveEffectPrefab;
                shield.shockwaveFrames = prefabShield.shockwaveFrames;
                shield.shockwaveFrameTime = prefabShield.shockwaveFrameTime;
                shield.shockwaveVisualScale = prefabShield.shockwaveVisualScale;
                shield.shockwaveRadius = prefabShield.shockwaveRadius;
                shield.knockbackForce = prefabShield.knockbackForce;
                shield.shockwaveDamage = prefabShield.shockwaveDamage;
                shield.regenTime = prefabShield.regenTime;
                shield.regenDelay = prefabShield.regenDelay;
                shield.shieldHitSound = prefabShield.shieldHitSound;
                shield.hitVolume = prefabShield.hitVolume;
                shield.shieldRegenSound = prefabShield.shieldRegenSound;
                shield.regenVolume = prefabShield.regenVolume;
                shield.shieldBreakSound = prefabShield.shieldBreakSound;
                shield.breakVolume = prefabShield.breakVolume;
                
                Debug.Log("[UpgradeManager] Настройки щита скопированы из префаба!");
            }
        }

        Debug.Log($"[UpgradeManager] Щит добавлен на Player! maxShield={shield.maxShield}");
    }

    void ApplyFists()
    {
        if (weaponSwitcher == null) weaponSwitcher = playerObject.GetComponent<WeaponSwitcher>();
        if (weaponSwitcher == null) return;

        if (weaponSwitcher.fistsWeapon != null)
            weaponSwitcher.UnlockFists();
        else if (fistsPrefab != null)
        {
            GameObject obj = Instantiate(fistsPrefab, playerObject.transform);
            weaponSwitcher.fistsWeapon = obj;
            weaponSwitcher.UnlockFists();
        }
    }

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
