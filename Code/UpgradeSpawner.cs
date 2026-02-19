using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит улучшения из Бездны после зачистки волны.
/// Исключает уже полученные способности из пула.
/// Корректно уничтожает все улучшения с tooltip'ами.
/// </summary>
public class UpgradeSpawner : MonoBehaviour
{
    [Header("=== ИСТОЧНИК ===")]
    public Transform monsterTransform;

    [Header("=== ПРЕФАБЫ УЛУЧШЕНИЙ ===")]
    public UpgradeData[] allUpgrades;

    [Header("=== НАСТРОЙКИ СПАВНА ===")]
    public int upgradesPerWave = 2;
    public float spawnDistance = 5f;
    public float spreadAngle = 45f;
    public Vector2 spawnOffset = Vector2.zero;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private List<GameObject> spawnedUpgrades = new List<GameObject>();

    // 🔥 Список уже полученных УНИКАЛЬНЫХ способностей (не стат-апгрейдов)
    private HashSet<UpgradeType> obtainedAbilities = new HashSet<UpgradeType>();

    void Start()
    {
        if (monsterTransform == null)
        {
            GameObject monster = GameObject.FindGameObjectWithTag("Monster");
            if (monster != null) monsterTransform = monster.transform;
        }
    }

    /// <summary>
    /// Отмечает способность как полученную (вызывается из UpgradeManager)
    /// </summary>
    public void MarkAsObtained(UpgradeType type)
    {
        // Только уникальные способности исключаются
        if (type == UpgradeType.RedAura || type == UpgradeType.ElectricShock ||
            type == UpgradeType.Shield || type == UpgradeType.Fists)
        {
            obtainedAbilities.Add(type);
            if (debugLogs) Debug.Log($"[UpgradeSpawner] {type} помечена как полученная. Больше не выпадет.");
        }
    }

    public void SpawnUpgrades()
    {
        if (monsterTransform == null)
        {
            Debug.LogError("[UpgradeSpawner] Монстр не найден!");
            return;
        }
        if (allUpgrades == null || allUpgrades.Length == 0)
        {
            Debug.LogError("[UpgradeSpawner] Нет настроенных улучшений!");
            return;
        }

        DestroyAllUpgrades();

        List<UpgradeData> selected = SelectRandomUpgrades(upgradesPerWave);
        if (debugLogs) Debug.Log($"[UpgradeSpawner] Спавним {selected.Count} улучшений");

        // Направление к игроку
        Vector2 dir = Vector2.down;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            dir = ((Vector2)playerObj.transform.position - (Vector2)monsterTransform.position).normalized;

        for (int i = 0; i < selected.Count; i++)
        {
            float angleOffset = (i - (selected.Count - 1) / 2f) * spreadAngle;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float finalAngle = baseAngle + angleOffset;

            Vector2 direction = new Vector2(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                Mathf.Sin(finalAngle * Mathf.Deg2Rad));
            Vector3 targetPos = monsterTransform.position + (Vector3)(direction * spawnDistance) + (Vector3)spawnOffset;

            targetPos = FindSafePosition(targetPos, monsterTransform.position);

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

    Vector3 FindSafePosition(Vector3 targetPos, Vector3 origin)
    {
        Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.5f, ~LayerMask.GetMask("Player", "Enemy"));
        if (hit != null && !hit.isTrigger)
        {
            Vector2 dirToOrigin = ((Vector2)origin - (Vector2)targetPos).normalized;
            for (float offset = 1f; offset <= 4f; offset += 1f)
            {
                Vector3 newPos = targetPos + (Vector3)(dirToOrigin * offset);
                Collider2D check = Physics2D.OverlapCircle(newPos, 0.5f, ~LayerMask.GetMask("Player", "Enemy"));
                if (check == null || check.isTrigger) return newPos;
            }
            return transform.position;
        }
        return targetPos;
    }

    List<UpgradeData> SelectRandomUpgrades(int count)
    {
        // Фильтруем уже полученные уникальные способности
        List<UpgradeData> available = new List<UpgradeData>();
        foreach (var data in allUpgrades)
        {
            if (!obtainedAbilities.Contains(data.type))
                available.Add(data);
        }

        List<UpgradeData> selected = new List<UpgradeData>();
        count = Mathf.Min(count, available.Count);

        // Копируем для рандома без повторов
        List<UpgradeData> pool = new List<UpgradeData>(available);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            selected.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return selected;
    }

    /// <summary>
    /// Уничтожает все улучшения НА КАРТЕ вместе с tooltip'ами
    /// </summary>
    public void DestroyAllUpgrades()
    {
        foreach (GameObject upgrade in spawnedUpgrades)
        {
            if (upgrade != null)
            {
                UpgradePickup pickup = upgrade.GetComponent<UpgradePickup>();
                if (pickup != null)
                    pickup.CleanupAndDestroy();
                else
                    Destroy(upgrade);
            }
        }
        spawnedUpgrades.Clear();
        if (debugLogs) Debug.Log("[UpgradeSpawner] Все улучшения уничтожены");
    }
}

[System.Serializable]
public class UpgradeData
{
    public GameObject prefab;
    public UpgradeType type;
    public float value = 0.2f;
    public string displayName = "Улучшение";
    [TextArea(2, 4)]
    public string description = "Описание";
    public Sprite icon;
}
