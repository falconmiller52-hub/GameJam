using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveData
{
    public string waveName = "Волна 1";
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
    public float warningDuration = 3f;  // "ОНИ ИДУТ" 3 сек

    [Header("Audio")]
    public AudioClip warningSound;

    [Header("Spawning")]
    public Transform[] spawnPoints;  // 4 точки
    public WaveData[] waves;  // Массив волн
    public float timeBetweenSpawns = 1f;  // Задержка между врагами
    public float timeBetweenWaves = 5f;  // Пауза между волнами

    [Header("Debug")]
    public int currentWaveIndex = 0;
    public bool waveActive = false;

    private AudioSource audioSource;
    private int enemiesRemaining = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        if (warningCanvasGroup == null && waveWarningText != null)
            warningCanvasGroup = waveWarningText.GetComponentInParent<CanvasGroup>();
            
        StartCoroutine(WaveSequence());
    }

    IEnumerator WaveSequence()
    {
        while (currentWaveIndex < waves.Length)
        {
            // 1. Показать предупреждение
            yield return StartCoroutine(ShowWarning(waves[currentWaveIndex].waveName));
            
            // 2. Спавнить волну
            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            
            // 3. Ждать окончания волны
            while (waveActive && enemiesRemaining > 0)
            {
                yield return null;
            }
            
            // 4. Пауза между волнами
            yield return new WaitForSeconds(timeBetweenWaves);
            
            currentWaveIndex++;
        }
        
        Debug.Log("Все волны завершены!");
    }

    IEnumerator ShowWarning(string waveText)
    {
        waveActive = false;
        enemiesRemaining = 0;
        
        // Звук + показ текста
        if (warningSound != null)
            audioSource.PlayOneShot(warningSound);
        
        waveWarningText.text = $"ОНИ ИДУТ\n{waveText}";
        
        if (warningCanvasGroup != null)
        {
            warningCanvasGroup.alpha = 1f;
            warningCanvasGroup.gameObject.SetActive(true);
        }
        
        yield return new WaitForSeconds(warningDuration);
        
        // Исчезновение
        if (warningCanvasGroup != null)
        {
            float fadeTime = 0.5f;
            float elapsed = 0f;
            float startAlpha = warningCanvasGroup.alpha;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                warningCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTime);
                yield return null;
            }
            
            warningCanvasGroup.alpha = 0f;
            warningCanvasGroup.gameObject.SetActive(false);
        }
    }

    IEnumerator SpawnWave(WaveData wave)
    {
        waveActive = true;
        enemiesRemaining = 0;
        
        Debug.Log($"Спавним волну: {wave.waveName}");
        
        // Подсчитываем общее количество
        foreach (var spawnData in wave.enemySpawns)
        {
            enemiesRemaining += spawnData.count;
        }
        
        // Спавним всех врагов
        foreach (var spawnData in wave.enemySpawns)
        {
            for (int i = 0; i < spawnData.count; i++)
            {
                if (!waveActive) yield break;
                
                SpawnSingleEnemy(spawnData.enemyPrefab);
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
        }
    }

    void SpawnSingleEnemy(GameObject prefab)
    {
        if (spawnPoints.Length == 0) return;
        
        int randomSpawn = Random.Range(0, spawnPoints.Length);
        Instantiate(prefab, spawnPoints[randomSpawn].position, Quaternion.identity);
    }

    // Вызывается из EnemyHealth при смерти последнего врага
    public void EnemyDied()
    {
        enemiesRemaining--;
        Debug.Log($"Враг убит. Осталось: {enemiesRemaining}");
        
        if (enemiesRemaining <= 0)
        {
            waveActive = false;
            Debug.Log("Волна завершена!");
        }
    }
}
