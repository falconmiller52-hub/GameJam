using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns upgrades from the Monster NPC after wave cleared.
/// FIXED: Re-searches for monster if null when SpawnUpgrades is called (fixes post-GameOver).
/// </summary>
public class UpgradeSpawner : MonoBehaviour
{
    [Header("=== SOURCE ===")]
    [Tooltip("Monster that spawns upgrades")]
    public Transform monsterTransform;

    [Header("=== UPGRADE PREFABS ===")]
    public UpgradeData[] allUpgrades;

    [Header("=== SPAWN SETTINGS ===")]
    public int upgradesPerWave = 2;
    public float spawnDistance = 3f;
    public float spreadAngle = 60f;

    [Header("=== DEBUG ===")]
    public bool debugLogs = true;

    private List<GameObject> spawnedUpgrades = new List<GameObject>();
    private HashSet<UpgradeType> obtainedAbilities = new HashSet<UpgradeType>();

    void Start()
    {
        FindMonster();
    }

    void FindMonster()
    {
        if (monsterTransform != null) return;
        GameObject monster = GameObject.FindGameObjectWithTag("Monster");
        if (monster != null) monsterTransform = monster.transform;
    }

    /// <summary>
    /// Marks an ability as obtained so it won't spawn again.
    /// </summary>
    public void MarkAsObtained(UpgradeType type)
    {
        obtainedAbilities.Add(type);
        if (debugLogs) Debug.Log($"[UpgradeSpawner] Marked as obtained: {type}");
    }

    public void SpawnUpgrades()
    {
        // 🔥 Re-search for monster if null (fixes post-GameOver reload)
        if (monsterTransform == null)
        {
            FindMonster();
            if (monsterTransform == null)
            {
                Debug.LogError("[UpgradeSpawner] Monster not found! Make sure Monster has tag 'Monster'.");
                return;
            }
        }

        if (allUpgrades == null || allUpgrades.Length == 0)
        {
            Debug.LogError("[UpgradeSpawner] No upgrades configured!");
            return;
        }

        DestroyAllUpgrades();

        List<UpgradeData> selected = SelectRandomUpgrades(upgradesPerWave);
        if (debugLogs) Debug.Log($"[UpgradeSpawner] Spawning {selected.Count} upgrades");

        for (int i = 0; i < selected.Count; i++)
        {
            float angleOffset = (i - (selected.Count - 1) / 2f) * spreadAngle;
            float angle = -90f + angleOffset;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            Vector3 targetPos = monsterTransform.position + direction * spawnDistance;

            GameObject upgrade = Instantiate(selected[i].prefab, monsterTransform.position, Quaternion.identity);
            spawnedUpgrades.Add(upgrade);

            UpgradePickup pickup = upgrade.GetComponent<UpgradePickup>();
            if (pickup != null)
            {
                pickup.upgradeType = selected[i].type;
                pickup.upgradeValue = selected[i].value;
                pickup.upgradeName = selected[i].displayName;
                pickup.upgradeDescription = selected[i].description;
                pickup.upgradeIcon = selected[i].icon;
                pickup.FlyOut(monsterTransform.position, targetPos);
            }
        }
    }

    List<UpgradeData> SelectRandomUpgrades(int count)
    {
        List<UpgradeData> available = new List<UpgradeData>();

        // Filter out obtained abilities
        foreach (var ud in allUpgrades)
        {
            bool isAbility = ud.type == UpgradeType.RedAura || ud.type == UpgradeType.ElectricShock ||
                             ud.type == UpgradeType.Shield || ud.type == UpgradeType.Fists;

            if (isAbility && obtainedAbilities.Contains(ud.type))
                continue;

            available.Add(ud);
        }

        List<UpgradeData> selected = new List<UpgradeData>();
        count = Mathf.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, available.Count);
            selected.Add(available[idx]);
            available.RemoveAt(idx);
        }

        return selected;
    }

    public void DestroyAllUpgrades()
    {
        foreach (GameObject upgrade in spawnedUpgrades)
        {
            if (upgrade != null)
            {
                UpgradePickup pickup = upgrade.GetComponent<UpgradePickup>();
                if (pickup != null) pickup.CleanupAndDestroy();
                else Destroy(upgrade);
            }
        }
        spawnedUpgrades.Clear();
    }
}

[System.Serializable]
public class UpgradeData
{
    public GameObject prefab;
    public UpgradeType type;
    public float value = 0.2f;
    public string displayName = "Upgrade";
    [TextArea(2, 4)] public string description = "Upgrade description";
    public Sprite icon;
}
