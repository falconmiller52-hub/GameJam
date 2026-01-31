using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveData
{
    public string waveName = "Волна 1";
    public int waveTransitionIndex = 0;
    
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
    [Header("=== UI ЭЛЕМЕНТЫ ===")]
    [Tooltip("Текст предупреждения о волне")]
    public TextMeshProUGUI waveWarningText;
    public CanvasGroup warningCanvasGroup;
    
    [Tooltip("Текст 'Волна зачищена!'")]
    public TextMeshProUGUI waveClearedText;
    public CanvasGroup clearedCanvasGroup;
    
    [Tooltip("Изображение при зачистке волны (опционально)")]
    public Image waveClearedImage;
    
    [Tooltip("Текст таймера обратного отсчёта")]
    public TextMeshProUGUI countdownText;
    public CanvasGroup countdownCanvasGroup;
    
    [Tooltip("Текст подсказки 'Нажмите R чтобы пропустить'")]
    public TextMeshProUGUI skipHintText;

    [Header("=== ТАЙМИНГИ ===")]
    public float warningDuration = 3f;
    public float clearedDisplayDuration = 2f;
    public float breakDuration = 20f; // Пауза между волнами
    public float musicFadeDuration = 2f;

    [Header("=== АУДИО ===")]
    public AudioClip warningSound;
    public AudioClip waveClearedSound;
    public AudioSource musicSource;
    private float originalMusicVolume;

    [Header("=== АНИМАЦИИ КАРТЫ ===")]
    public Animator mapAnimator;
    public string idleTrigger = "Idle";
    public string[] waveTransitionTriggers = { 
        "Wave1ToWave2", "Wave2ToWave3", "Wave3ToIdle" 
    };

    [Header("=== МОНСТР НПС ===")]
    public Animator monsterAnimator;
    public string monsterCalmTrigger = "ToCalmForm";   // Нейтральная форма
    public string monsterWildTrigger = "ToWildForm";   // Дикая форма

    [Header("=== УЛУЧШЕНИЯ ===")]
    public UpgradeSpawner upgradeSpawner;

    [Header("=== СПАВН ===")]
    public Transform[] spawnPoints;
    public WaveData[] waves;
    public float timeBetweenSpawns = 1f;

    [Header("=== ОТЛАДКА ===")]
    public int currentWaveIndex = 0;
    public bool waveActive = false;
    public bool debugLogs = true;

    // Приватные переменные
    private AudioSource audioSource;
    private int enemiesRemaining = 0;
    private bool skipRequested = false;
    private bool isInBreak = false;
    private bool upgradePickedUp = false;

    // ===== СОБЫТИЯ (для других скриптов) =====
    public System.Action OnWaveCleared;
    public System.Action OnWaveStarted;
    public System.Action OnBreakStarted;
    public System.Action OnBreakEnded;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        // Автопоиск UI компонентов
        SetupUIReferences();
        
        // Автопоиск других компонентов
        if (mapAnimator == null)
        {
            GameObject mapBG = GameObject.Find("MapBackground");
            if (mapBG != null) mapAnimator = mapBG.GetComponent<Animator>();
        }

        if (monsterAnimator == null)
        {
            GameObject monster = GameObject.FindGameObjectWithTag("Monster");
            if (monster != null) monsterAnimator = monster.GetComponent<Animator>();
        }

        if (musicSource == null)
        {
            GameObject musicObj = GameObject.Find("LevelMusic");
            if (musicObj != null) musicSource = musicObj.GetComponent<AudioSource>();
        }

        if (musicSource != null)
            originalMusicVolume = musicSource.volume;

        if (upgradeSpawner == null)
            upgradeSpawner = FindObjectOfType<UpgradeSpawner>();

        // Старт с Idle
        if (mapAnimator != null)
            mapAnimator.SetTrigger(idleTrigger);

        // Скрываем UI
        HideAllUI();

        StartCoroutine(WaveSequence());
    }

    void Update()
    {
        // Проверка на пропуск паузы
        if (isInBreak && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            skipRequested = true;
            if (debugLogs) Debug.Log("[WaveSpawner] Игрок пропустил паузу!");
        }
    }

    void SetupUIReferences()
    {
        if (warningCanvasGroup == null && waveWarningText != null)
            warningCanvasGroup = waveWarningText.GetComponentInParent<CanvasGroup>();
        
        if (clearedCanvasGroup == null && waveClearedText != null)
            clearedCanvasGroup = waveClearedText.GetComponentInParent<CanvasGroup>();
        
        if (countdownCanvasGroup == null && countdownText != null)
            countdownCanvasGroup = countdownText.GetComponentInParent<CanvasGroup>();
    }

    void HideAllUI()
    {
        if (warningCanvasGroup != null) 
        {
            warningCanvasGroup.alpha = 0f;
            warningCanvasGroup.gameObject.SetActive(false);
        }
        if (clearedCanvasGroup != null)
        {
            clearedCanvasGroup.alpha = 0f;
            clearedCanvasGroup.gameObject.SetActive(false);
        }
        if (countdownCanvasGroup != null)
        {
            countdownCanvasGroup.alpha = 0f;
            countdownCanvasGroup.gameObject.SetActive(false);
        }
    }

    // ===== ГЛАВНАЯ ПОСЛЕДОВАТЕЛЬНОСТЬ ВОЛН =====

    IEnumerator WaveSequence()
    {
        while (currentWaveIndex < waves.Length)
        {
            // === ПЕРВАЯ ВОЛНА ===
            if (currentWaveIndex == 0)
            {
                yield return StartCoroutine(ShowWaveWarning());
            }
            else
            {
                // === ПЕРЕХОД МЕЖДУ ВОЛНАМИ ===
                yield return StartCoroutine(PlayMapTransitionAnimation(
                    waves[currentWaveIndex].waveTransitionIndex));
                yield return StartCoroutine(ShowWaveWarning());
            }

            // Монстр в дикую форму
            SetMonsterForm(wild: true);
            
            // Событие начала волны
            OnWaveStarted?.Invoke();

            // === СПАВН ВОЛНЫ ===
            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));

            // === ОЖИДАНИЕ ЗАЧИСТКИ ===
            float waveTimeout = 60f;
            while (enemiesRemaining > 0 && waveTimeout > 0f)
            {
                waveTimeout -= Time.deltaTime;
                yield return null;
            }

            if (waveTimeout <= 0f)
            {
                Debug.LogWarning($"Волна {currentWaveIndex + 1} завершилась по таймауту!");
                enemiesRemaining = 0;
            }

            if (debugLogs) Debug.Log($"Волна {currentWaveIndex + 1} зачищена!");

            // Событие зачистки
            OnWaveCleared?.Invoke();

            // === ПАУЗА МЕЖДУ ВОЛНАМИ ===
            // (только если это НЕ последняя волна)
            if (currentWaveIndex < waves.Length - 1)
            {
                yield return StartCoroutine(WaveBreakSequence());
            }

            currentWaveIndex++;
        }

        // Финал
        if (mapAnimator != null)
            mapAnimator.SetTrigger(idleTrigger);

        if (debugLogs) Debug.Log("ВСЕ ВОЛНЫ ЗАВЕРШЕНЫ!");
    }

    // ===== ПАУЗА МЕЖДУ ВОЛНАМИ =====

    IEnumerator WaveBreakSequence()
    {
        isInBreak = true;
        skipRequested = false;
        upgradePickedUp = false;
        
        OnBreakStarted?.Invoke();

        // 1. Монстр в спокойную форму
        SetMonsterForm(wild: false);

        // 2. Плавно приглушаем музыку
        StartCoroutine(FadeMusic(targetVolume: originalMusicVolume * 0.3f, duration: musicFadeDuration));

        // 3. Показываем "ВОЛНА ЗАЧИЩЕНА!"
        yield return StartCoroutine(ShowWaveCleared());

        // 4. Спавним улучшения из Монстра
        if (upgradeSpawner != null)
        {
            upgradeSpawner.SpawnUpgrades();
        }

        // 5. Показываем таймер и подсказку
        yield return StartCoroutine(ShowCountdown());

        // 6. Возвращаем музыку
        StartCoroutine(FadeMusic(targetVolume: originalMusicVolume, duration: 1f));

        isInBreak = false;
        OnBreakEnded?.Invoke();
    }

    // ===== UI КОРУТИНЫ =====

    IEnumerator ShowWaveWarning()
    {
        waveActive = false;
        enemiesRemaining = 0;

        if (warningSound != null)
            audioSource.PlayOneShot(warningSound, 0.3f);

        int waveNumber = currentWaveIndex + 1;
        
        if (waveWarningText != null)
            waveWarningText.text = $"ВОЛНА {waveNumber}";

        if (warningCanvasGroup != null)
        {
            warningCanvasGroup.gameObject.SetActive(true);
            warningCanvasGroup.alpha = 0f;

            // Плавное появление
            yield return StartCoroutine(FadeCanvasGroup(warningCanvasGroup, 0f, 1f, 0.3f));

            yield return new WaitForSecondsRealtime(warningDuration);

            // Плавное исчезновение
            yield return StartCoroutine(FadeCanvasGroup(warningCanvasGroup, 1f, 0f, 0.5f));

            warningCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(warningDuration);
        }
    }

    IEnumerator ShowWaveCleared()
    {
        if (waveClearedSound != null)
            audioSource.PlayOneShot(waveClearedSound, 0.5f);

        if (waveClearedText != null)
            waveClearedText.text = "ВОЛНА ЗАЧИЩЕНА!";

        if (clearedCanvasGroup != null)
        {
            clearedCanvasGroup.gameObject.SetActive(true);
            clearedCanvasGroup.alpha = 0f;

            // Плавное появление
            yield return StartCoroutine(FadeCanvasGroup(clearedCanvasGroup, 0f, 1f, 0.3f));

            yield return new WaitForSecondsRealtime(clearedDisplayDuration);

            // Плавное исчезновение
            yield return StartCoroutine(FadeCanvasGroup(clearedCanvasGroup, 1f, 0f, 0.5f));

            clearedCanvasGroup.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(clearedDisplayDuration);
        }
    }

    IEnumerator ShowCountdown()
    {
        if (countdownCanvasGroup != null)
        {
            countdownCanvasGroup.gameObject.SetActive(true);
            countdownCanvasGroup.alpha = 1f;
        }

        if (skipHintText != null)
            skipHintText.text = "Нажмите R, чтобы пропустить";

        float remainingTime = breakDuration;

        while (remainingTime > 0 && !skipRequested && !upgradePickedUp)
        {
            // Обновляем текст таймера
            if (countdownText != null)
            {
                int seconds = Mathf.CeilToInt(remainingTime);
                countdownText.text = $"Следующая волна через: {seconds}";
            }

            remainingTime -= Time.deltaTime;
            yield return null;
        }

        // Скрываем таймер
        if (countdownCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(countdownCanvasGroup, 1f, 0f, 0.3f));
            countdownCanvasGroup.gameObject.SetActive(false);
        }

        // Уничтожаем оставшиеся улучшения
        if (upgradeSpawner != null)
            upgradeSpawner.DestroyAllUpgrades();
    }

    // ===== АНИМАЦИИ КАРТЫ И МОНСТРА =====

    IEnumerator PlayMapTransitionAnimation(int transitionIndex)
    {
        if (mapAnimator == null || transitionIndex >= waveTransitionTriggers.Length)
            yield break;

        string triggerName = waveTransitionTriggers[transitionIndex];
        if (debugLogs) Debug.Log($"Переход карты: {triggerName}");

        mapAnimator.SetTrigger(triggerName);
        yield return new WaitForSeconds(1.5f);
        mapAnimator.SetTrigger(idleTrigger);
    }

    void SetMonsterForm(bool wild)
    {
        if (monsterAnimator == null) return;

        if (wild)
        {
            monsterAnimator.SetTrigger(monsterWildTrigger);
            if (debugLogs) Debug.Log("[WaveSpawner] Монстр → ДИКАЯ форма");
        }
        else
        {
            monsterAnimator.SetTrigger(monsterCalmTrigger);
            if (debugLogs) Debug.Log("[WaveSpawner] Монстр → СПОКОЙНАЯ форма");
        }
    }

    // ===== СПАВН ВРАГОВ =====

    IEnumerator SpawnWave(WaveData wave)
    {
        if (debugLogs) Debug.Log($"Спавним волну: {wave.waveName}");

        int totalEnemiesInWave = 0;
        foreach (var spawnData in wave.enemySpawns)
        {
            totalEnemiesInWave += spawnData.count;
        }
        enemiesRemaining = totalEnemiesInWave;
        waveActive = true;

        foreach (var spawnData in wave.enemySpawns)
        {
            for (int i = 0; i < spawnData.count; i++)
            {
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

    public void EnemyDied()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        if (debugLogs) Debug.Log($"Враг убит. Осталось: {enemiesRemaining}");

        if (enemiesRemaining <= 0)
        {
            waveActive = false;
            if (debugLogs) Debug.Log("Волна завершена!");
        }
    }

    // ===== ВЫЗЫВАЕТСЯ ИЗ UPGRADE PICKUP =====

    public void OnUpgradePickedUp()
    {
        upgradePickedUp = true;
        if (debugLogs) Debug.Log("[WaveSpawner] Улучшение подобрано! Запускаем следующую волну.");
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =====

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator FadeMusic(float targetVolume, float duration)
    {
        if (musicSource == null) yield break;

        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }
        musicSource.volume = targetVolume;
    }
}
