#define UNITY_ANALYTICS_ENABLED
using UnityEngine;
using System.Collections.Generic;

#if UNITY_ANALYTICS_ENABLED
using Unity.Services.Core;
using Unity.Services.Analytics;
#endif

// ============================================================================
// UNITY ANALYTICS 6.x — СБОРЩИК ИГРОВОЙ СТАТИСТИКИ
// ============================================================================
// 
// Если пакет НЕ установлен и код не компилируется:
//   → Закомментируй первую строку: // #define UNITY_ANALYTICS_ENABLED
//
// НАСТРОЙКА:
// 1. Edit → Project Settings → Services → привяжи проект
// 2. Window → Package Manager → установи Analytics (6.x)
// 3. Создай GameObject "AnalyticsManager" в первой сцене
// 4. Повесь этот скрипт
// ============================================================================

public class GameAnalyticsManager : MonoBehaviour
{
    public static GameAnalyticsManager Instance { get; private set; }

    private float sessionStartTime;
    private float gameStartTime;
    private float waveStartTime;
    private int currentWave = 0;
    private int totalDeaths = 0;
    private int totalUpgradesPicked = 0;
    private Dictionary<string, int> upgradePickCounts = new Dictionary<string, int>();
    private string lastDamageSource = "unknown";
    private bool gameCompleted = false;
    private bool isInitialized = false;

    [Header("=== DEBUG ===")]
    [Tooltip("Показывать события в Console?")]
    public bool debugMode = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        sessionStartTime = Time.realtimeSinceStartup;
    }

    async void Start()
    {
#if UNITY_ANALYTICS_ENABLED
        try
        {
            await UnityServices.InitializeAsync();
            isInitialized = true;
            LogDebug("Unity Analytics 6.x инициализирован!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Analytics] Ошибка инициализации: {e.Message}");
        }
#else
        isInitialized = false;
        LogDebug("Analytics отключён (#define закомментирован)");
#endif
        SendEvent("session_start", new Dictionary<string, object>
        {
            { "platform", Application.platform.ToString() },
            { "resolution", $"{Screen.width}x{Screen.height}" }
        });
    }

    void OnApplicationQuit() { TrackGameQuit(); }

    // ==================== ПУБЛИЧНЫЕ МЕТОДЫ ====================

    public void TrackGameStarted()
    {
        gameStartTime = Time.realtimeSinceStartup;
        currentWave = 0; totalDeaths = 0; totalUpgradesPicked = 0;
        upgradePickCounts.Clear(); gameCompleted = false;
        SendEvent("game_started", new Dictionary<string, object>
        { { "session_time", GetSessionTime() } });
    }

    public void TrackWaveStarted(int waveNumber)
    {
        currentWave = waveNumber;
        waveStartTime = Time.realtimeSinceStartup;
        SendEvent("wave_started", new Dictionary<string, object>
        { { "wave_number", waveNumber }, { "game_time", GetGameTime() } });
    }

    public void TrackWaveCleared(int waveNumber)
    {
        float dur = Time.realtimeSinceStartup - waveStartTime;
        SendEvent("wave_cleared", new Dictionary<string, object>
        { { "wave_number", waveNumber }, { "wave_duration_sec", Mathf.RoundToInt(dur) }, { "game_time", GetGameTime() } });
    }

    public void SetLastDamageSource(string enemyType) { lastDamageSource = enemyType; }

    public void TrackPlayerDied()
    {
        totalDeaths++;
        SendEvent("player_died", new Dictionary<string, object>
        { { "wave_number", currentWave }, { "killed_by", lastDamageSource },
          { "game_time", GetGameTime() }, { "total_upgrades", totalUpgradesPicked }, { "death_number", totalDeaths } });
        lastDamageSource = "unknown";
    }

    public void TrackUpgradePicked(string upgradeType, string upgradeName)
    {
        totalUpgradesPicked++;
        if (!upgradePickCounts.ContainsKey(upgradeType)) upgradePickCounts[upgradeType] = 0;
        upgradePickCounts[upgradeType]++;
        SendEvent("upgrade_picked", new Dictionary<string, object>
        { { "upgrade_type", upgradeType }, { "upgrade_name", upgradeName },
          { "wave_number", currentWave }, { "times_picked", upgradePickCounts[upgradeType] }, { "total_upgrades", totalUpgradesPicked } });
    }

    public void TrackGameCompleted()
    {
        gameCompleted = true;
        SendEvent("game_completed", new Dictionary<string, object>
        { { "game_time", GetGameTime() }, { "total_upgrades", totalUpgradesPicked }, { "total_deaths", totalDeaths } });
    }

    public void TrackEndingDialogueStart()
    {
        SendEvent("ending_dialogue_start", new Dictionary<string, object>
        { { "game_time", GetGameTime() } });
    }

    public void TrackCreditsReached()
    {
        SendEvent("credits_reached", new Dictionary<string, object>
        { { "game_time", GetGameTime() }, { "total_upgrades", totalUpgradesPicked } });
    }

    public void TrackGameQuit()
    {
        SendEvent("game_quit", new Dictionary<string, object>
        { { "wave_number", currentWave }, { "game_time", GetGameTime() }, { "session_time", GetSessionTime() },
          { "game_completed", gameCompleted }, { "total_upgrades", totalUpgradesPicked }, { "total_deaths", totalDeaths } });
    }

    // ==================== ОТПРАВКА ====================

    void SendEvent(string eventName, Dictionary<string, object> parameters)
    {
#if UNITY_ANALYTICS_ENABLED
        if (isInitialized)
        {
            try
            {
                // Unity Analytics 6.x API
                CustomEvent evt = new CustomEvent(eventName);
                foreach (var kv in parameters)
                {
                    if      (kv.Value is int i)    evt.Add(kv.Key, i);
                    else if (kv.Value is float f)  evt.Add(kv.Key, (double)f);
                    else if (kv.Value is bool b)   evt.Add(kv.Key, b);
                    else if (kv.Value is long l)   evt.Add(kv.Key, l);
                    else if (kv.Value is double d) evt.Add(kv.Key, d);
                    else                           evt.Add(kv.Key, kv.Value?.ToString() ?? "");
                }
                AnalyticsService.Instance.RecordEvent(evt);
                AnalyticsService.Instance.Flush();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] Ошибка отправки '{eventName}': {e.Message}");
            }
        }
#endif
        if (debugMode)
        {
            string p = "";
            foreach (var kv in parameters) p += $"  {kv.Key} = {kv.Value}\n";
            Debug.Log($"[Analytics] 📊 {eventName}\n{p}");
        }
    }

    float GetSessionTime() => Mathf.Round(Time.realtimeSinceStartup - sessionStartTime);
    float GetGameTime() => gameStartTime <= 0 ? 0 : Mathf.Round(Time.realtimeSinceStartup - gameStartTime);
    void LogDebug(string msg) { if (debugMode) Debug.Log($"[Analytics] {msg}"); }
}
