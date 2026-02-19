using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

[System.Serializable]
public class WaveData
{
    public string waveName = "Волна 1";
    public int waveTransitionIndex = 0;
    [Header("Враги")] public EnemySpawnData[] enemySpawns;
}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject enemyPrefab;
    public int count = 5;
}

/// <summary>
/// Управляет последовательностью волн, паузами, спавном,
/// анимациями Бездны и сменой музыки.
/// 
/// ИСПРАВЛЕНО:
/// - Анимации Бездны: используются ТОЧНЫЕ имена триггеров из Animator
///   (ToCalmForm, ToWildForm, Idle) + сброс других триггеров перед установкой нового
/// - Смена музыки после настраиваемой волны
/// - Корректная последовательность: волна→зачистка→calm→пауза→wild→следующая волна
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("=== UI ===")]
    public TextMeshProUGUI waveWarningText;
    public CanvasGroup warningCanvasGroup;
    public TextMeshProUGUI waveClearedText;
    public CanvasGroup clearedCanvasGroup;
    public Image waveClearedImage;
    public TextMeshProUGUI countdownText;
    public CanvasGroup countdownCanvasGroup;
    public TextMeshProUGUI skipHintText;

    [Header("=== ТАЙМИНГИ ===")]
    public float warningDuration = 3f;
    public float clearedDisplayDuration = 2f;
    public float breakDuration = 20f;
    public float musicFadeDuration = 2f;

    [Header("=== АУДИО ===")]
    public AudioClip warningSound;
    public AudioClip waveClearedSound;
    public AudioSource musicSource;
    private float originalMusicVolume;

    [Header("=== СМЕНА МУЗЫКИ ===")]
    [Tooltip("После какой волны менять музыку (2 = после 3-й волны, индексация с 0)")]
    public int musicChangeAfterWave = 2;
    public AudioClip secondPhaseMusic;
    [Range(0f, 2f)] public float secondPhaseMusicVolumeMultiplier = 1f;
    private bool musicChanged = false;

    [Header("=== АНИМАЦИИ КАРТЫ ===")]
    public Animator mapAnimator;
    public string idleTrigger = "Idle";
    public string[] waveTransitionTriggers;

    [Header("=== БЕЗДНА (МОНСТР НПС) ===")]
    public Animator monsterAnimator;
    [Tooltip("Точные имена триггеров из Animator Controller Бездны")]
    public string monsterCalmTrigger = "ToCalmForm";
    public string monsterWildTrigger = "ToWildForm";
    public string monsterIdleTrigger = "Idle";

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

    private AudioSource audioSource;
    private int enemiesRemaining = 0;
    private bool skipRequested = false;
    private bool isInBreak = false;
    private bool upgradePickedUp = false;

    public System.Action OnWaveCleared;
    public System.Action OnWaveStarted;
    public System.Action OnBreakStarted;
    public System.Action OnBreakEnded;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (monsterAnimator == null)
        {
            GameObject m = GameObject.FindGameObjectWithTag("Monster");
            if (m != null) monsterAnimator = m.GetComponent<Animator>();
        }
        if (musicSource != null) originalMusicVolume = musicSource.volume;
        if (upgradeSpawner == null) upgradeSpawner = FindObjectOfType<UpgradeSpawner>();

        HideAllUI();
        StartCoroutine(WaveSequence());
    }

    void Update()
    {
        if (isInBreak && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            skipRequested = true;
            if (debugLogs) Debug.Log("[WaveSpawner] Пауза пропущена!");
        }
    }

    void HideAllUI()
    {
        SetCG(warningCanvasGroup, 0f, false);
        SetCG(clearedCanvasGroup, 0f, false);
        SetCG(countdownCanvasGroup, 0f, false);
    }

    void SetCG(CanvasGroup cg, float alpha, bool active)
    {
        if (cg != null) { cg.alpha = alpha; cg.gameObject.SetActive(active); }
    }

    // ==================== ГЛАВНАЯ ПОСЛЕДОВАТЕЛЬНОСТЬ ====================

    IEnumerator WaveSequence()
    {
        while (currentWaveIndex < waves.Length)
        {
            // --- Перед волной ---
            if (currentWaveIndex > 0)
                yield return StartCoroutine(PlayMapTransition(waves[currentWaveIndex].waveTransitionIndex));

            yield return StartCoroutine(ShowWaveWarning());

            // 🔥 Бездна → ЯРОСТЬ перед волной
            SetMonsterState(MonsterState.Wild);

            OnWaveStarted?.Invoke();

            // --- Спавн ---
            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));

            // --- Ждём зачистки ---
            float timeout = 120f;
            while (enemiesRemaining > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
            if (timeout <= 0f) { enemiesRemaining = 0; Debug.LogWarning("Волна завершилась по таймауту!"); }

            if (debugLogs) Debug.Log($"Волна {currentWaveIndex + 1} зачищена!");
            OnWaveCleared?.Invoke();

            // --- Пауза (если не последняя волна) ---
            if (currentWaveIndex < waves.Length - 1)
                yield return StartCoroutine(WaveBreak());

            currentWaveIndex++;
        }

        // 🔥 После всех волн — Idle
        SetMonsterState(MonsterState.Idle);
        if (debugLogs) Debug.Log("ВСЕ ВОЛНЫ ЗАВЕРШЕНЫ!");
    }

    // ==================== ПАУЗА МЕЖДУ ВОЛНАМИ ====================

    IEnumerator WaveBreak()
    {
        isInBreak = true;
        skipRequested = false;
        upgradePickedUp = false;
        OnBreakStarted?.Invoke();

        // 🔥 1. Бездна → СПОКОЙСТВИЕ
        SetMonsterState(MonsterState.Calm);

        // 🔥 2. Музыка: смена или приглушение
        if (!musicChanged && currentWaveIndex >= musicChangeAfterWave && secondPhaseMusic != null)
        {
            yield return StartCoroutine(FadeMusic(0f, musicFadeDuration));
            if (musicSource != null)
            {
                musicSource.clip = secondPhaseMusic;
                musicSource.volume = 0f;
                musicSource.Play();
                originalMusicVolume *= secondPhaseMusicVolumeMultiplier;
                musicChanged = true;
            }
            StartCoroutine(FadeMusic(originalMusicVolume * 0.3f, 1f));
        }
        else
        {
            StartCoroutine(FadeMusic(originalMusicVolume * 0.3f, musicFadeDuration));
        }

        // 3. "ВОЛНА ЗАЧИЩЕНА!"
        yield return StartCoroutine(ShowWaveCleared());

        // 4. Спавним улучшения
        if (upgradeSpawner != null) upgradeSpawner.SpawnUpgrades();

        // 5. Обратный отсчёт
        yield return StartCoroutine(ShowCountdown());

        // 6. Музыка обратно на полную
        StartCoroutine(FadeMusic(originalMusicVolume, 1f));

        isInBreak = false;
        OnBreakEnded?.Invoke();
    }

    // ==================== АНИМАЦИИ БЕЗДНЫ ====================

    enum MonsterState { Idle, Calm, Wild }

    /// <summary>
    /// 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: сбрасываем ВСЕ триггеры перед установкой нового.
    /// Без этого Animator мог "съедать" триггеры и не реагировать.
    /// </summary>
    void SetMonsterState(MonsterState state)
    {
        if (monsterAnimator == null) return;

        // Сбрасываем ВСЕ триггеры — критически важно!
        monsterAnimator.ResetTrigger(monsterCalmTrigger);
        monsterAnimator.ResetTrigger(monsterWildTrigger);
        monsterAnimator.ResetTrigger(monsterIdleTrigger);

        switch (state)
        {
            case MonsterState.Calm:
                monsterAnimator.SetTrigger(monsterCalmTrigger);
                if (debugLogs) Debug.Log("[WaveSpawner] Бездна → СПОКОЙСТВИЕ (ToCalmForm)");
                break;
            case MonsterState.Wild:
                monsterAnimator.SetTrigger(monsterWildTrigger);
                if (debugLogs) Debug.Log("[WaveSpawner] Бездна → ЯРОСТЬ (ToWildForm)");
                break;
            case MonsterState.Idle:
                monsterAnimator.SetTrigger(monsterIdleTrigger);
                if (debugLogs) Debug.Log("[WaveSpawner] Бездна → Idle");
                break;
        }
    }

    // ==================== UI ====================

    IEnumerator ShowWaveWarning()
    {
        if (warningSound != null) audioSource.PlayOneShot(warningSound, 0.3f);
        if (waveWarningText != null) waveWarningText.text = $"ВОЛНА {currentWaveIndex + 1}";

        if (warningCanvasGroup != null)
        {
            warningCanvasGroup.gameObject.SetActive(true);
            yield return FadeCG(warningCanvasGroup, 0f, 1f, 0.3f);
            yield return new WaitForSecondsRealtime(warningDuration);
            yield return FadeCG(warningCanvasGroup, 1f, 0f, 0.5f);
            warningCanvasGroup.gameObject.SetActive(false);
        }
        else yield return new WaitForSecondsRealtime(warningDuration);
    }

    IEnumerator ShowWaveCleared()
    {
        if (waveClearedSound != null) audioSource.PlayOneShot(waveClearedSound, 0.5f);
        if (waveClearedText != null) waveClearedText.text = "ВОЛНА ЗАЧИЩЕНА!";

        if (clearedCanvasGroup != null)
        {
            clearedCanvasGroup.gameObject.SetActive(true);
            yield return FadeCG(clearedCanvasGroup, 0f, 1f, 0.3f);
            yield return new WaitForSecondsRealtime(clearedDisplayDuration);
            yield return FadeCG(clearedCanvasGroup, 1f, 0f, 0.5f);
            clearedCanvasGroup.gameObject.SetActive(false);
        }
        else yield return new WaitForSecondsRealtime(clearedDisplayDuration);
    }

    IEnumerator ShowCountdown()
    {
        SetCG(countdownCanvasGroup, 1f, true);
        if (skipHintText != null) skipHintText.text = "Нажмите R, чтобы пропустить";

        float remaining = breakDuration;
        while (remaining > 0 && !skipRequested && !upgradePickedUp)
        {
            if (countdownText != null) countdownText.text = $"Следующая волна через: {Mathf.CeilToInt(remaining)}";
            remaining -= Time.deltaTime;
            yield return null;
        }

        if (countdownCanvasGroup != null)
        {
            yield return FadeCG(countdownCanvasGroup, 1f, 0f, 0.3f);
            countdownCanvasGroup.gameObject.SetActive(false);
        }
        if (upgradeSpawner != null) upgradeSpawner.DestroyAllUpgrades();
    }

    // ==================== СПАВН ====================

    IEnumerator SpawnWave(WaveData wave)
    {
        int total = 0;
        foreach (var s in wave.enemySpawns) total += s.count;
        enemiesRemaining = total;
        waveActive = true;

        foreach (var s in wave.enemySpawns)
            for (int i = 0; i < s.count; i++)
            {
                if (spawnPoints.Length > 0)
                {
                    int idx = Random.Range(0, spawnPoints.Length);
                    Instantiate(s.enemyPrefab, spawnPoints[idx].position, Quaternion.identity);
                }
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
    }

    public void EnemyDied()
    {
        enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
        if (enemiesRemaining <= 0) waveActive = false;
    }

    public void OnUpgradePickedUp()
    {
        upgradePickedUp = true;
    }

    // ==================== КАРТА ====================

    IEnumerator PlayMapTransition(int idx)
    {
        if (mapAnimator == null || waveTransitionTriggers == null || idx >= waveTransitionTriggers.Length)
            yield break;
        mapAnimator.SetTrigger(waveTransitionTriggers[idx]);
        yield return new WaitForSeconds(1.5f);
        if (!string.IsNullOrEmpty(idleTrigger)) mapAnimator.SetTrigger(idleTrigger);
    }

    // ==================== УТИЛИТЫ ====================

    IEnumerator FadeCG(CanvasGroup cg, float from, float to, float dur)
    {
        float e = 0f;
        while (e < dur) { e += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(from, to, e / dur); yield return null; }
        cg.alpha = to;
    }

    IEnumerator FadeMusic(float target, float dur)
    {
        if (musicSource == null) yield break;
        float start = musicSource.volume;
        float e = 0f;
        while (e < dur) { e += Time.deltaTime; musicSource.volume = Mathf.Lerp(start, target, e / dur); yield return null; }
        musicSource.volume = target;
    }
}
