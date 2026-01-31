using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит улучшения из Монстра НПС после зачистки волны.
/// </summary>
public class UpgradeSpawner : MonoBehaviour
{
    [Header("=== ИСТОЧНИК ===")]
    [Tooltip("Монстр, из которого вылетают улучшения")]
    public Transform monsterTransform;

    [Header("=== ПРЕФАБЫ УЛУЧШЕНИЙ ===")]
    [Tooltip("Список всех возможных улучшений")]
    public UpgradeData[] allUpgrades;

    [Header("=== НАСТРОЙКИ СПАВНА ===")]
    [Tooltip("Сколько улучшений спавнить за раз")]
    public int upgradesPerWave = 2;
    
    [Tooltip("Расстояние от Монстра")]
    public float spawnDistance = 3f;
    
    [Tooltip("Угол разброса между улучшениями (градусы)")]
    public float spreadAngle = 60f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    // Список текущих улучшений на сцене
    private List<GameObject> spawnedUpgrades = new List<GameObject>();

    void Start()
    {
        // Автопоиск Монстра
        if (monsterTransform == null)
        {
            GameObject monster = GameObject.FindGameObjectWithTag("Monster");
            if (monster != null)
                monsterTransform = monster.transform;
        }
    }

    /// <summary>
    /// Спавнит улучшения из Монстра
    /// </summary>
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

        // Очищаем старые улучшения
        DestroyAllUpgrades();

        // Выбираем случайные улучшения (без повторов)
        List<UpgradeData> selectedUpgrades = SelectRandomUpgrades(upgradesPerWave);

        if (debugLogs) Debug.Log($"[UpgradeSpawner] Спавним {selectedUpgrades.Count} улучшений");

        // Спавним улучшения по дуге от Монстра
        for (int i = 0; i < selectedUpgrades.Count; i++)
        {
            // Вычисляем угол для этого улучшения
            float angleOffset = (i - (selectedUpgrades.Count - 1) / 2f) * spreadAngle;
            float angle = -90f + angleOffset; // -90 = вниз от Монстра (к игроку)
            
            // Вычисляем позицию
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.right;
            Vector3 targetPos = monsterTransform.position + direction * spawnDistance;

            // Создаём улучшение
            GameObject upgrade = Instantiate(selectedUpgrades[i].prefab, monsterTransform.position, Quaternion.identity);
            spawnedUpgrades.Add(upgrade);

            // Настраиваем компонент UpgradePickup
            UpgradePickup pickup = upgrade.GetComponent<UpgradePickup>();
            if (pickup != null)
            {
                pickup.upgradeType = selectedUpgrades[i].type;
                pickup.upgradeValue = selectedUpgrades[i].value;
                pickup.upgradeName = selectedUpgrades[i].displayName;
                pickup.upgradeDescription = selectedUpgrades[i].description;
                pickup.upgradeIcon = selectedUpgrades[i].icon;

                // Запускаем анимацию вылета
                pickup.FlyOut(monsterTransform.position, targetPos);
            }

            if (debugLogs) Debug.Log($"[UpgradeSpawner] Создано: {selectedUpgrades[i].displayName}");
        }
    }

    /// <summary>
    /// Выбирает случайные улучшения без повторов
    /// </summary>
    List<UpgradeData> SelectRandomUpgrades(int count)
    {
        List<UpgradeData> available = new List<UpgradeData>(allUpgrades);
        List<UpgradeData> selected = new List<UpgradeData>();

        count = Mathf.Min(count, available.Count);

        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, available.Count);
            selected.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        return selected;
    }

    /// <summary>
    /// Уничтожает все текущие улучшения
    /// </summary>
    public void DestroyAllUpgrades()
    {
        foreach (GameObject upgrade in spawnedUpgrades)
        {
            if (upgrade != null)
                Destroy(upgrade);
        }
        spawnedUpgrades.Clear();

        if (debugLogs) Debug.Log("[UpgradeSpawner] Все улучшения уничтожены");
    }
}

/// <summary>
/// Данные об улучшении
/// </summary>
[System.Serializable]
public class UpgradeData
{
    [Tooltip("Префаб улучшения")]
    public GameObject prefab;
    
    [Tooltip("Тип улучшения")]
    public UpgradeType type;
    
    [Tooltip("Значение улучшения")]
    public float value = 0.2f;
    
    [Tooltip("Отображаемое название")]
    public string displayName = "Улучшение";
    
    [Tooltip("Описание")]
    [TextArea(2, 4)]
    public string description = "Описание улучшения";
    
    [Tooltip("Иконка")]
    public Sprite icon;
}
