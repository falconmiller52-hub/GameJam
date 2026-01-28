using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveData
{
    public string waveName = "Волна 1";
    public int waveTransitionIndex = 0;  // ✅ Индекс анимации ПЕРЕХОДА (0=Wave1ToWave2, 1=Wave2ToWave3...)
    
    [Header("Количество врагов по типам")]
    public EnemySpawnData[] enemySpawns;
}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject enemyPrefab;
    public int count = 5;
}


public class WaveSpawner : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI waveWarningText;
    public CanvasGroup warningCanvasGroup;
    public float warningDuration = 3f;

    [Header("Audio")]
    public AudioClip warningSound;

    [Header("Map Animations")]
    public Animator mapAnimator;
    public string idleTrigger = "Idle";  // ✅ Idle состояние
    public string[] waveTransitionTriggers = { 
        "Wave1ToWave2", "Wave2ToWave3", "Wave3ToIdle" 
    };  // ✅ Переходы МЕЖДУ волнами
    
    [Header("Spawning")]
    public Transform[] spawnPoints;
    public WaveData[] waves;
    public float timeBetweenSpawns = 1f;
    public float timeBetweenWaves = 5f;

    [Header("Debug")]
    public int currentWaveIndex = 0;
    public bool waveActive = false;

    private AudioSource audioSource;
    private int enemiesRemaining = 0;
    private bool firstWaveStarted = false;  // ✅ Флаг первой волны

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        if (warningCanvasGroup == null && waveWarningText != null)
            warningCanvasGroup = waveWarningText.GetComponentInParent<CanvasGroup>();
            
        if (mapAnimator == null)
        {
            GameObject mapBG = GameObject.Find("MapBackground");
            if (mapBG != null) mapAnimator = mapBG.GetComponent<Animator>();
        }
        
        // ✅ Старт с Idle
        if (mapAnimator != null)
        {
            mapAnimator.SetTrigger(idleTrigger);
        }
        
        StartCoroutine(WaveSequence());
    }

IEnumerator WaveSequence()
{
    while (currentWaveIndex < waves.Length)
    {
        // 🔥 ПЕРВАЯ ВОЛНА (index 0) — БЕЗ ПЕРЕХОДА КАРТЫ
        if (currentWaveIndex == 0)
        {
            yield return StartCoroutine(ShowWarning(""));
        }
        else
        {
            // ВСЕ ОСТАЛЬНЫЕ ВОЛНЫ — С ПЕРЕХОДОМ КАРТЫ
            yield return StartCoroutine(PlayMapTransitionAnimation(
                waves[currentWaveIndex].waveTransitionIndex));
            yield return StartCoroutine(ShowWarning(""));
        }
        
        // Спавним волну
        yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        
        // 🔥 ЖДЕМ ОКОНЧАНИЯ ВОЛНЫ С ТИМАУТОМ (30 секунд)
        float waveTimeout = 30f;
        while (enemiesRemaining > 0 && waveTimeout > 0f)
        {
            waveTimeout -= Time.deltaTime;
            yield return null;
        }
        
        if (waveTimeout <= 0f)
        {
            Debug.LogWarning($"Волна {currentWaveIndex + 1} завершилась по таймауту! Осталось врагов: {enemiesRemaining}");
            enemiesRemaining = 0; // Принудительно завершаем
        }
        else
        {
            Debug.Log($"Волна {currentWaveIndex + 1} завершена нормально");
        }
        
        // Пауза между волнами
        yield return new WaitForSeconds(timeBetweenWaves);
        
        currentWaveIndex++;
    }
    
    // Финальный Idle
    if (mapAnimator != null)
    {
        mapAnimator.SetTrigger(idleTrigger);
    }
    
    Debug.Log("Все волны завершены!");
}




    // ✅ АНИМАЦИЯ ПЕРЕХОДА между волнами
    IEnumerator PlayMapTransitionAnimation(int transitionIndex)
    {
        if (mapAnimator == null || transitionIndex >= waveTransitionTriggers.Length)
        {
            Debug.LogWarning("MapAnimator или триггер перехода не настроены!");
            yield break;
        }
        
        string triggerName = waveTransitionTriggers[transitionIndex];
        Debug.Log($"Переход карты: {triggerName}");
        
        mapAnimator.SetTrigger(triggerName);
        
        // Ждем окончания анимации перехода
        yield return new WaitForSeconds(1.5f);  // Настройте под длительность клипа
        
        // Возвращаемся в Idle
        mapAnimator.SetTrigger(idleTrigger);
    }

IEnumerator ShowWarning(string waveText)
{
    // 🔥 СБРОС: все волны независимы
    waveActive = false;
    enemiesRemaining = 0;
    
    if (warningSound != null)
        audioSource.PlayOneShot(warningSound, 0.3f);
    
    // 🔥 АВТОМАТИЧЕСКИЙ ТЕКСТ
    int waveNumber = currentWaveIndex + 1;
    waveWarningText.text = $"ВОЛНА {waveNumber}";
    
    if (warningCanvasGroup != null)
    {
        warningCanvasGroup.alpha = 1f;
        warningCanvasGroup.gameObject.SetActive(true);
        warningCanvasGroup.blocksRaycasts = true; // Блокируем клики
        warningCanvasGroup.interactable = true;
    }
    
    // 🔥 Используем unscaledDeltaTime, чтобы предупреждение работало на паузе
    yield return new WaitForSecondsRealtime(warningDuration); // Не зависает при паузе
    
    if (warningCanvasGroup != null)
    {
        float fadeTime = 0.5f;
        float elapsed = 0f;
        float startAlpha = warningCanvasGroup.alpha;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime; // 🔥 Работает на паузе
            warningCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTime);
            yield return null;
        }
        
        warningCanvasGroup.alpha = 0f;
        warningCanvasGroup.gameObject.SetActive(false);
        warningCanvasGroup.blocksRaycasts = false;
        warningCanvasGroup.interactable = false;
    }
}


IEnumerator SpawnWave(WaveData wave)
{
    Debug.Log($"Спавним волну: {wave.waveName}");
    
    // 🔥 ПОДСЧИТАЕМ ОБЩЕЕ КОЛИЧЕСТВО ДО СПАВНА
    int totalEnemiesInWave = 0;
    foreach (var spawnData in wave.enemySpawns)
    {
        totalEnemiesInWave += spawnData.count;
    }
    enemiesRemaining = totalEnemiesInWave; // ← ЗАДАЕМ ДОНАЧАЛО
    
    waveActive = true;
    
    foreach (var spawnData in wave.enemySpawns)
    {
        for (int i = 0; i < spawnData.count; i++)
        {
            SpawnSingleEnemy(spawnData.enemyPrefab);
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }
    
    // 🔥 НЕ ПРЕРЫВАЕМ! ДОВАРИВАЕМ ВСЕХ
}


    void SpawnSingleEnemy(GameObject prefab)
    {
        if (spawnPoints.Length == 0) return;
        
        int randomSpawn = Random.Range(0, spawnPoints.Length);
        Instantiate(prefab, spawnPoints[randomSpawn].position, Quaternion.identity);
    }

public void EnemyDied()
{
    enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1); // ← Никогда не уйдет в минус!
    Debug.Log($"Враг убит. Осталось: {enemiesRemaining}");
    
    if (enemiesRemaining <= 0)
    {
        waveActive = false;
        Debug.Log("Волна завершена!");
    }
}

}
