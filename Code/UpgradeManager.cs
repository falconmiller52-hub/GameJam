using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("=== EFFECT PREFABS ===")]
    public GameObject redAuraPrefab;
    public GameObject electricShockPrefab;
    public GameObject fistsPrefab;

    [Header("=== SHIELD SETTINGS ===")]
    public GameObject shieldSettingsPrefab;

    [Header("=== REFERENCES ===")]
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
            if (playerObject == null) { Debug.LogError("[UpgradeManager] Player not found!"); return; }
        }
        if (playerUpgrades == null) playerUpgrades = playerObject.GetComponent<PlayerUpgrades>();
        if (weaponSwitcher == null) weaponSwitcher = playerObject.GetComponent<WeaponSwitcher>();
    }

    public void ApplyUpgrade(UpgradeType type, float value)
    {
        if (playerObject == null) FindPlayer();
        if (playerObject == null) return;

        Debug.Log($"[UpgradeManager] Applying: {type}, value: {value}");

        switch (type)
        {
            case UpgradeType.Speed:
            case UpgradeType.Damage:
            case UpgradeType.AttackSpeed:
            case UpgradeType.MaxHealth:
                if (playerUpgrades != null) playerUpgrades.ApplyUpgrade(type, value);
                break;
            case UpgradeType.RedAura:
                ApplyRedAura(); MarkAbility(type); break;
            case UpgradeType.ElectricShock:
                ApplyElectricShock(); MarkAbility(type); break;
            case UpgradeType.Shield:
                ApplyShield((int)value); MarkAbility(type); break;
            case UpgradeType.Fists:
                ApplyFists(); MarkAbility(type); break;
        }
    }

    void MarkAbility(UpgradeType type)
    {
        if (upgradeSpawner == null) upgradeSpawner = FindObjectOfType<UpgradeSpawner>();
        if (upgradeSpawner != null) upgradeSpawner.MarkAsObtained(type);
    }

    void ApplyRedAura()
    {
        RedAura existing = playerObject.GetComponent<RedAura>();
        if (existing != null) { existing.Upgrade(0.5f, 1f); return; }
        if (redAuraPrefab != null) { GameObject o = Instantiate(redAuraPrefab, playerObject.transform); o.transform.localPosition = Vector3.zero; }
        else playerObject.AddComponent<RedAura>();
    }

    void ApplyElectricShock()
    {
        ElectricShock existing = playerObject.GetComponent<ElectricShock>();
        if (existing != null) { existing.Upgrade(1, 1); return; }
        if (electricShockPrefab != null) { GameObject o = Instantiate(electricShockPrefab, playerObject.transform); o.transform.localPosition = Vector3.zero; }
        else playerObject.AddComponent<ElectricShock>();
    }

    void ApplyShield(int val)
    {
        PlayerShield existing = playerObject.GetComponent<PlayerShield>();
        if (existing != null) { existing.Upgrade(val); return; }

        PlayerShield shield = playerObject.AddComponent<PlayerShield>();
        shield.maxShield = val > 0 ? val : 1;
        shield.currentShield = shield.maxShield;

        if (shieldSettingsPrefab != null)
        {
            PlayerShield ps = shieldSettingsPrefab.GetComponent<PlayerShield>();
            if (ps != null)
            {
                shield.shieldFullSprite = ps.shieldFullSprite;
                shield.shieldBrokenSprite = ps.shieldBrokenSprite;
                shield.shockwaveEffectPrefab = ps.shockwaveEffectPrefab;
                shield.shockwaveFrames = ps.shockwaveFrames;
                shield.shockwaveFrameTime = ps.shockwaveFrameTime;
                shield.shockwaveVisualScale = ps.shockwaveVisualScale;
                shield.shockwaveRadius = ps.shockwaveRadius;
                shield.knockbackForce = ps.knockbackForce;
                shield.shockwaveDamage = ps.shockwaveDamage;
                shield.regenTime = ps.regenTime;
                shield.regenDelay = ps.regenDelay;
                shield.shieldHitSound = ps.shieldHitSound;
                shield.hitVolume = ps.hitVolume;
                shield.shieldRegenSound = ps.shieldRegenSound;
                shield.regenVolume = ps.regenVolume;
                shield.shieldBreakSound = ps.shieldBreakSound;
                shield.breakVolume = ps.breakVolume;
            }
        }

        Debug.Log($"[UpgradeManager] Shield added! maxShield={shield.maxShield}");
    }

    [Header("=== ЛОКАЛИЗАЦИЯ ===")]
    [Tooltip("Подсказка при разблокировке перчаток")]
    public string fistsUnlockHint = "Нажмите Q для смены оружия";

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

        PlayerTutorial tutorial = playerObject.GetComponent<PlayerTutorial>();
        if (tutorial != null)
        {
            tutorial.ShowCustomMessage(fistsUnlockHint);
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
