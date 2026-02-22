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

public class WaveSpawner : MonoBehaviour
{
    [Header("=== UI ===")]
    public TextMeshProUGUI waveWarningText;
    public CanvasGroup warningCanvasGroup;
    public TextMeshProUGUI waveClearedText;
    public CanvasGroup clearedCanvasGroup;
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
    [Range(0f, 1f)] public float warningVolume = 0.3f;
    public AudioClip waveClearedSound;
    [Range(0f, 1f)] public float waveClearedVolume = 0.4f;

    [Header("=== МУЗЫКА (три AudioSource) ===")]
    public AudioSource levelMusic1;
    public AudioSource levelMusic2;
    [Tooltip("Музыка концовки")]
    public AudioSource levelMusic3;
    public int musicChangeAfterWave = 2;
    private float music1OrigVol, music2OrigVol, music3OrigVol;
    private bool musicSwitched = false;

    [Header("=== КАРТА ===")]
    public Animator mapAnimator;
    public string idleTrigger = "Idle";
    public string[] waveTransitionTriggers;

    [Header("=== БЕЗДНА ===")]
    public Animator monsterAnimator;
    public Transform monsterTransform;
    public string monsterCalmTrigger = "ToCalmForm";
    public string monsterWildTrigger = "ToWildForm";
    public string monsterIdleTrigger = "Idle";

    [Header("=== УЛУЧШЕНИЯ ===")]
    public UpgradeSpawner upgradeSpawner;

    [Header("=== СПАВН ===")]
    public Transform[] spawnPoints;
    public WaveData[] waves;
    public float timeBetweenSpawns = 1f;

    [Header("=== КОНЦОВКА: ОБЩЕЕ ===")]
    public float endingDelay = 2f;
    public float cameraPanDuration = 2f;
    public float cameraReturnDuration = 1.5f;
    public GameObject weaponPivot;
    public Animator playerAnimator;
    [Tooltip("Триггер ОСОБОЙ анимации смерти (не Die, а другой)")]
    public string endingDeathTrigger = "EndingDeath";
    public float deathAnimWaitTime = 2f;

    [Header("=== КОНЦОВКА: ДИАЛОГ ===")]
    [Tooltip("TextMeshPro (мировой, НЕ UGUI) под Бездной. Или UGUI на отдельном Canvas.")]
    public TextMeshProUGUI endingDialogueTextUGUI;
    [Tooltip("Альтернатива: мировой TextMeshPro (если UGUI не работает)")]
    public TextMeshPro endingDialogueTextWorld;
    [TextArea(2, 5)] public string[] endingDialogueLines;
    public AudioClip voiceClip;
    [Range(0f, 1f)] public float voiceVolume = 0.7f;
    public float voicePitchVariation = 0.2f;
    public float typingSpeed = 0.05f;

    [Header("=== КОНЦОВКА: МАГИЯ ===")]
    public GameObject magicEffectPrefab;
    public AudioClip magicSound;
    [Range(0f, 1f)] public float magicSoundVolume = 0.8f;
    public float magicDisplayDuration = 3f;

    [Header("=== КОНЦОВКА: ЗВУК СМЕРТИ ===")]
    public AudioClip endingDeathSound;
    [Range(0f, 1f)] public float endingDeathVolume = 1f;

    [Header("=== КОНЦОВКА: ЗАТЕМНЕНИЕ ===")]
    public CanvasGroup endFadePanel;
    public float endFadeDuration = 2f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private AudioSource sfx;
    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;
    private bool waveActive, skipRequested, isInBreak, upgradePickedUp, gameEnded;

    public System.Action OnWaveCleared, OnWaveStarted;

    void Start()
    {
        sfx = GetComponent<AudioSource>();
        if (sfx == null) sfx = gameObject.AddComponent<AudioSource>();

        if (monsterAnimator == null)
        {
            GameObject m = GameObject.FindGameObjectWithTag("Monster");
            if (m != null) { monsterAnimator = m.GetComponent<Animator>(); monsterTransform = m.transform; }
        }
        if (upgradeSpawner == null) upgradeSpawner = FindObjectOfType<UpgradeSpawner>();

        if (levelMusic1 != null) music1OrigVol = levelMusic1.volume;
        if (levelMusic2 != null) { music2OrigVol = levelMusic2.volume; levelMusic2.volume = 0f; levelMusic2.Stop(); }
        if (levelMusic3 != null) { music3OrigVol = levelMusic3.volume; levelMusic3.volume = 0f; levelMusic3.Stop(); }

        // Автопоиск
        if (weaponPivot == null)
        { GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) { Transform wp = p.transform.Find("WeaponPivot"); if (wp != null) weaponPivot = wp.gameObject; } }
        if (playerAnimator == null)
        { GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) playerAnimator = p.GetComponent<Animator>(); }

        HideAllUI();
        StartCoroutine(WaveSequence());
    }

    void Update()
    {
        if (isInBreak && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) skipRequested = true;
    }

    void HideAllUI()
    {
        SetCG(warningCanvasGroup, 0f, false);
        SetCG(clearedCanvasGroup, 0f, false);
        SetCG(countdownCanvasGroup, 0f, false);
        if (endingDialogueTextUGUI != null) endingDialogueTextUGUI.text = "";
        if (endingDialogueTextWorld != null) endingDialogueTextWorld.text = "";
        if (endFadePanel != null) { endFadePanel.alpha = 0f; endFadePanel.gameObject.SetActive(false); }
    }

    void SetCG(CanvasGroup cg, float a, bool on)
    { if (cg != null) { cg.alpha = a; cg.gameObject.SetActive(on); } }

    // ==================== ВОЛНЫ ====================

    IEnumerator WaveSequence()
    {
        while (currentWaveIndex < waves.Length)
        {
            if (currentWaveIndex > 0)
                yield return StartCoroutine(PlayMapTransition(waves[currentWaveIndex].waveTransitionIndex));

            yield return StartCoroutine(ShowWaveWarning());
            SetMonsterState(2); // Wild
            OnWaveStarted?.Invoke();

            yield return StartCoroutine(SpawnWave(waves[currentWaveIndex]));
            float timeout = 120f;
            while (enemiesRemaining > 0 && timeout > 0f) { timeout -= Time.deltaTime; yield return null; }
            if (timeout <= 0f) enemiesRemaining = 0;
            OnWaveCleared?.Invoke();

            if (currentWaveIndex >= waves.Length - 1)
            { yield return StartCoroutine(EndingSequence()); yield break; }

            yield return StartCoroutine(WaveBreak());
            currentWaveIndex++;
        }
    }

    IEnumerator WaveBreak()
    {
        isInBreak = true; skipRequested = false; upgradePickedUp = false;
        SetMonsterState(1); // Calm

        if (!musicSwitched && currentWaveIndex >= musicChangeAfterWave && levelMusic2 != null)
            StartCoroutine(CrossfadeMusic(levelMusic1, levelMusic2, music2OrigVol * 0.3f));
        else
            StartCoroutine(FadeAS(GetActiveMusic(), GetActiveVol() * 0.3f, musicFadeDuration));

        yield return StartCoroutine(ShowWaveCleared());
        if (upgradeSpawner != null) upgradeSpawner.SpawnUpgrades();
        yield return StartCoroutine(ShowCountdown());
        StartCoroutine(FadeAS(GetActiveMusic(), GetActiveVol(), 1f));
        isInBreak = false;
    }

    // ==================== МУЗЫКА ====================

    IEnumerator CrossfadeMusic(AudioSource from, AudioSource to, float targetVol)
    {
        musicSwitched = true;
        if (to != null && !to.isPlaying) { to.volume = 0f; to.Play(); }
        float fromVol = from != null ? from.volume : 0f;
        float e = 0f;
        while (e < musicFadeDuration)
        {
            e += Time.deltaTime; float t = e / musicFadeDuration;
            if (from != null) from.volume = Mathf.Lerp(fromVol, 0f, t);
            if (to != null) to.volume = Mathf.Lerp(0f, targetVol, t);
            yield return null;
        }
        if (from != null) { from.volume = 0f; from.Stop(); from.gameObject.SetActive(false); }
        if (to != null) to.volume = targetVol;
    }

    AudioSource GetActiveMusic() => musicSwitched ? levelMusic2 : levelMusic1;
    float GetActiveVol() => musicSwitched ? music2OrigVol : music1OrigVol;

    IEnumerator FadeAS(AudioSource s, float target, float dur)
    { if (s == null) yield break; float start = s.volume; float e = 0f; while (e < dur) { e += Time.deltaTime; s.volume = Mathf.Lerp(start, target, e / dur); yield return null; } s.volume = target; }

    // ==================== БЕЗДНА ====================

    void SetMonsterState(int state) // 0=idle 1=calm 2=wild
    {
        if (monsterAnimator == null) return;
        monsterAnimator.ResetTrigger(monsterCalmTrigger);
        monsterAnimator.ResetTrigger(monsterWildTrigger);
        monsterAnimator.ResetTrigger(monsterIdleTrigger);
        if (state == 0) monsterAnimator.SetTrigger(monsterIdleTrigger);
        else if (state == 1) monsterAnimator.SetTrigger(monsterCalmTrigger);
        else monsterAnimator.SetTrigger(monsterWildTrigger);
    }

    // ==================== КОНЦОВКА ====================

    IEnumerator EndingSequence()
    {
        gameEnded = true;
        SetMonsterState(1); // Calm

        yield return new WaitForSeconds(endingDelay);

        // Блокировка управления
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var pm = playerObj.GetComponent<PlayerMovement>(); if (pm != null) pm.enabled = false;
            var pa = playerObj.GetComponent<PlayerAttack>(); if (pa != null) pa.enabled = false;
            var fw = playerObj.GetComponentInChildren<FistsWeapon>(); if (fw != null) fw.enabled = false;
            var rb = playerObj.GetComponent<Rigidbody2D>(); if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        // 🔥 Затухание боевой музыки + запуск музыки концовки
        StartCoroutine(FadeAS(GetActiveMusic(), 0f, musicFadeDuration));
        if (levelMusic3 != null)
        {
            levelMusic3.volume = 0f;
            levelMusic3.Play();
            StartCoroutine(FadeAS(levelMusic3, music3OrigVol, musicFadeDuration));
        }

        // Камера к Бездне
        DynamicCamera dynCam = FindObjectOfType<DynamicCamera>();
        Camera mainCam = Camera.main;
        if (dynCam != null) dynCam.enabled = false;
        if (mainCam != null && monsterTransform != null)
            yield return StartCoroutine(PanCamera(mainCam.transform, monsterTransform.position, cameraPanDuration));

        // Диалог
        if (endingDialogueLines != null && endingDialogueLines.Length > 0)
            yield return StartCoroutine(PlayEndingDialogue());

        // Магия + звук
        if (magicEffectPrefab != null && monsterTransform != null)
        {
            GameObject magic = Instantiate(magicEffectPrefab, monsterTransform.position + Vector3.up * 0.5f, Quaternion.identity);
            SpriteRenderer msr = magic.GetComponent<SpriteRenderer>();
            if (msr != null) msr.sortingOrder = 100;
            Destroy(magic, magicDisplayDuration + 2f);
        }

        if (magicSound != null) sfx.PlayOneShot(magicSound, magicSoundVolume);

        yield return new WaitForSeconds(magicDisplayDuration);

        // Камера к игроку
        if (mainCam != null && playerObj != null)
            yield return StartCoroutine(PanCamera(mainCam.transform, playerObj.transform.position, cameraReturnDuration));

        // Оружие исчезает
        if (weaponPivot != null) weaponPivot.SetActive(false);

        // Звук смерти
        if (endingDeathSound != null) sfx.PlayOneShot(endingDeathSound, endingDeathVolume);

        // Особая анимация смерти
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(endingDeathTrigger);
        }
        yield return new WaitForSeconds(deathAnimWaitTime);

        // Затухание музыки + экрана
        if (levelMusic3 != null) StartCoroutine(FadeAS(levelMusic3, 0f, endFadeDuration));

        if (endFadePanel != null)
        {
            endFadePanel.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < endFadeDuration)
            { elapsed += Time.unscaledDeltaTime; endFadePanel.alpha = Mathf.Clamp01(elapsed / endFadeDuration); yield return null; }
            endFadePanel.alpha = 1f;
        }
    }

    IEnumerator PanCamera(Transform cam, Vector3 target, float dur)
    {
        Vector3 start = cam.position; target.z = start.z;
        float e = 0f;
        while (e < dur) { e += Time.deltaTime; cam.position = Vector3.Lerp(start, target, EaseIO(e / dur)); yield return null; }
        cam.position = target;
    }

    float EaseIO(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

    IEnumerator PlayEndingDialogue()
    {
        for (int i = 0; i < endingDialogueLines.Length; i++)
        {
            SetDialogueText("");
            bool typing = true;

            foreach (char c in endingDialogueLines[i].ToCharArray())
            {
                SetDialogueText(GetDialogueText() + c);

                if (voiceClip != null && c != ' ')
                {
                    sfx.pitch = 1f + Random.Range(-voicePitchVariation, voicePitchVariation);
                    sfx.PlayOneShot(voiceClip, voiceVolume);
                }

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                { SetDialogueText(endingDialogueLines[i]); typing = false; break; }

                yield return new WaitForSeconds(typingSpeed);
            }
            typing = false;

            // Ждём клик
            yield return null; // Пропускаем кадр чтобы не "проскочить"
            while (true)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break;
                yield return null;
            }
        }
        SetDialogueText("");
    }

    // 🔥 Поддержка UGUI и World-space текста
    void SetDialogueText(string text)
    {
        if (endingDialogueTextUGUI != null) endingDialogueTextUGUI.text = text;
        if (endingDialogueTextWorld != null) endingDialogueTextWorld.text = text;
    }

    string GetDialogueText()
    {
        if (endingDialogueTextUGUI != null) return endingDialogueTextUGUI.text;
        if (endingDialogueTextWorld != null) return endingDialogueTextWorld.text;
        return "";
    }

    // ==================== UI ====================

    IEnumerator ShowWaveWarning()
    {
        if (warningSound != null) sfx.PlayOneShot(warningSound, warningVolume);
        if (waveWarningText != null) waveWarningText.text = $"ВОЛНА {currentWaveIndex + 1}";
        if (warningCanvasGroup != null)
        { warningCanvasGroup.gameObject.SetActive(true); yield return FadeCG(warningCanvasGroup, 0f, 1f, 0.3f); yield return new WaitForSecondsRealtime(warningDuration); yield return FadeCG(warningCanvasGroup, 1f, 0f, 0.5f); warningCanvasGroup.gameObject.SetActive(false); }
        else yield return new WaitForSecondsRealtime(warningDuration);
    }

    IEnumerator ShowWaveCleared()
    {
        if (waveClearedSound != null) sfx.PlayOneShot(waveClearedSound, waveClearedVolume);
        if (waveClearedText != null) waveClearedText.text = "ВОЛНА ЗАЧИЩЕНА!";
        if (clearedCanvasGroup != null)
        { clearedCanvasGroup.gameObject.SetActive(true); yield return FadeCG(clearedCanvasGroup, 0f, 1f, 0.3f); yield return new WaitForSecondsRealtime(clearedDisplayDuration); yield return FadeCG(clearedCanvasGroup, 1f, 0f, 0.5f); clearedCanvasGroup.gameObject.SetActive(false); }
        else yield return new WaitForSecondsRealtime(clearedDisplayDuration);
    }

    IEnumerator ShowCountdown()
    {
        SetCG(countdownCanvasGroup, 1f, true);
        if (skipHintText != null) skipHintText.text = "Нажмите R, чтобы пропустить";
        float remaining = breakDuration;
        while (remaining > 0 && !skipRequested && !upgradePickedUp)
        { if (countdownText != null) countdownText.text = $"Следующая волна через: {Mathf.CeilToInt(remaining)}"; remaining -= Time.deltaTime; yield return null; }
        if (countdownCanvasGroup != null) { yield return FadeCG(countdownCanvasGroup, 1f, 0f, 0.3f); countdownCanvasGroup.gameObject.SetActive(false); }
        if (upgradeSpawner != null) upgradeSpawner.DestroyAllUpgrades();
    }

    IEnumerator SpawnWave(WaveData wave)
    {
        int total = 0; foreach (var s in wave.enemySpawns) total += s.count;
        enemiesRemaining = total; waveActive = true;
        foreach (var s in wave.enemySpawns)
            for (int i = 0; i < s.count; i++)
            { if (spawnPoints.Length > 0) Instantiate(s.enemyPrefab, spawnPoints[Random.Range(0, spawnPoints.Length)].position, Quaternion.identity); yield return new WaitForSeconds(timeBetweenSpawns); }
    }

    public void EnemyDied() { enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1); if (enemiesRemaining <= 0) waveActive = false; }
    public void OnUpgradePickedUp() { upgradePickedUp = true; }

    IEnumerator PlayMapTransition(int idx)
    { if (mapAnimator == null || waveTransitionTriggers == null || idx >= waveTransitionTriggers.Length) yield break; mapAnimator.SetTrigger(waveTransitionTriggers[idx]); yield return new WaitForSeconds(1.5f); if (!string.IsNullOrEmpty(idleTrigger)) mapAnimator.SetTrigger(idleTrigger); }

    IEnumerator FadeCG(CanvasGroup cg, float from, float to, float dur)
    { float e = 0f; while (e < dur) { e += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(from, to, e / dur); yield return null; } cg.alpha = to; }
}
