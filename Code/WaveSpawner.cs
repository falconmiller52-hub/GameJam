using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

[System.Serializable]
public class WaveData
{
    public string waveName = "Wave 1";
    public int waveTransitionIndex = 0;
    [Header("Enemies")] public EnemySpawnData[] enemySpawns;
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

    [Header("=== WAVE CLEARED ICON ===")]
    [Tooltip("Image above WaveClearedText that plays a sprite animation")]
    public Image waveClearedIcon;
    [Tooltip("Frames for wave cleared icon animation")]
    public Sprite[] waveClearedIconFrames;
    [Tooltip("Frames per second for icon animation")]
    public float iconAnimFPS = 8f;
    [Tooltip("Loop icon animation?")]
    public bool iconAnimLoop = true;

    [Header("=== TIMING ===")]
    public float warningDuration = 3f;
    public float clearedDisplayDuration = 2f;
    public float breakDuration = 20f;
    public float musicFadeDuration = 2f;

    [Header("=== AUDIO ===")]
    public AudioClip warningSound;
    [Range(0f, 1f)] public float warningVolume = 0.3f;
    public AudioClip waveClearedSound;
    [Range(0f, 1f)] public float waveClearedVolume = 0.4f;

    [Header("=== MUSIC (3 AudioSources) ===")]
    public AudioSource levelMusic1;
    public AudioSource levelMusic2;
    public AudioSource levelMusic3;
    public int musicChangeAfterWave = 2;
    private float music1Vol, music2Vol, music3Vol;
    private bool musicSwitched = false;

    [Header("=== MAP ===")]
    public Animator mapAnimator;
    public string idleTrigger = "Idle";
    public string[] waveTransitionTriggers;

    [Header("=== ABYSS (MONSTER) ===")]
    public Animator monsterAnimator;
    public Transform monsterTransform;
    public string monsterCalmTrigger = "ToCalmForm";
    public string monsterWildTrigger = "ToWildForm";
    public string monsterIdleTrigger = "Idle";

    [Header("=== UPGRADES ===")]
    public UpgradeSpawner upgradeSpawner;

    [Header("=== SPAWN ===")]
    public Transform[] spawnPoints;
    public WaveData[] waves;
    public float timeBetweenSpawns = 1f;

    [Header("=== ENDING: GENERAL ===")]
    public float endingDelay = 2f;
    public float cameraPanDuration = 2f;
    public float cameraReturnDuration = 1.5f;
    public GameObject weaponPivot;
    public Animator playerAnimator;
    public string endingDeathTrigger = "EndingDeath";
    public float deathAnimWaitTime = 2f;

    [Header("=== ENDING: DIALOGUE ===")]
    public TextMeshProUGUI endingDialogueTextUGUI;
    public TextMeshPro endingDialogueTextWorld;
    [TextArea(2, 5)] public string[] endingDialogueLines;
    public AudioClip voiceClip;
    [Range(0f, 1f)] public float voiceVolume = 0.7f;
    public float voicePitchVariation = 0.2f;
    public float typingSpeed = 0.05f;

    [Header("=== ENDING: MAGIC ===")]
    public GameObject magicEffectPrefab;
    public AudioClip magicSound;
    [Range(0f, 1f)] public float magicSoundVolume = 0.8f;
    public float magicDisplayDuration = 3f;

    [Header("=== ENDING: DEATH ===")]
    public AudioClip endingDeathSound;
    [Range(0f, 1f)] public float endingDeathVolume = 1f;

    [Header("=== ENDING: FADE ===")]
    public CanvasGroup endFadePanel;
    public float endFadeDuration = 2f;

    [Header("=== DEBUG ===")]
    public bool debugLogs = true;

    private AudioSource sfx;
    private int currentWaveIndex = 0;
    private int enemiesRemaining = 0;
    private bool waveActive, skipRequested, isInBreak, upgradePickedUp, gameEnded;
    private Coroutine iconAnimCoroutine;

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

        if (levelMusic1 != null) music1Vol = levelMusic1.volume;
        if (levelMusic2 != null) { music2Vol = levelMusic2.volume; levelMusic2.volume = 0f; levelMusic2.Stop(); }
        if (levelMusic3 != null) { music3Vol = levelMusic3.volume; levelMusic3.volume = 0f; levelMusic3.Stop(); }

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
        if (waveClearedIcon != null) waveClearedIcon.gameObject.SetActive(false);
        if (endingDialogueTextUGUI != null) endingDialogueTextUGUI.text = "";
        if (endingDialogueTextWorld != null) endingDialogueTextWorld.text = "";
        if (endFadePanel != null) { endFadePanel.alpha = 0f; endFadePanel.gameObject.SetActive(false); }
    }

    void SetCG(CanvasGroup cg, float a, bool on) { if (cg != null) { cg.alpha = a; cg.gameObject.SetActive(on); } }

    // ==================== WAVES ====================

    IEnumerator WaveSequence()
    {
        while (currentWaveIndex < waves.Length)
        {
            if (currentWaveIndex > 0)
                yield return StartCoroutine(PlayMapTransition(waves[currentWaveIndex].waveTransitionIndex));

            yield return StartCoroutine(ShowWaveWarning());
            SetMonsterState(2);
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
        SetMonsterState(1);

        if (!musicSwitched && currentWaveIndex >= musicChangeAfterWave && levelMusic2 != null)
            StartCoroutine(CrossfadeMusic(levelMusic1, levelMusic2, music2Vol * 0.3f));
        else
            StartCoroutine(FadeAS(GetActiveMusic(), GetActiveVol() * 0.3f, musicFadeDuration));

        yield return StartCoroutine(ShowWaveCleared());
        if (upgradeSpawner != null) upgradeSpawner.SpawnUpgrades();
        yield return StartCoroutine(ShowCountdown());
        StartCoroutine(FadeAS(GetActiveMusic(), GetActiveVol(), 1f));
        isInBreak = false;
    }

    // ==================== MUSIC ====================

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
    float GetActiveVol() => musicSwitched ? music2Vol : music1Vol;

    IEnumerator FadeAS(AudioSource s, float target, float dur)
    { if (s == null) yield break; float st = s.volume; float e = 0f; while (e < dur) { e += Time.deltaTime; s.volume = Mathf.Lerp(st, target, e / dur); yield return null; } s.volume = target; }

    // ==================== MONSTER ====================

    void SetMonsterState(int state)
    {
        if (monsterAnimator == null) return;
        monsterAnimator.ResetTrigger(monsterCalmTrigger);
        monsterAnimator.ResetTrigger(monsterWildTrigger);
        monsterAnimator.ResetTrigger(monsterIdleTrigger);
        if (state == 0) monsterAnimator.SetTrigger(monsterIdleTrigger);
        else if (state == 1) monsterAnimator.SetTrigger(monsterCalmTrigger);
        else monsterAnimator.SetTrigger(monsterWildTrigger);
    }

    // ==================== WAVE CLEARED ICON ANIMATION ====================

    void StartIconAnimation()
    {
        if (waveClearedIcon == null || waveClearedIconFrames == null || waveClearedIconFrames.Length == 0) return;
        waveClearedIcon.gameObject.SetActive(true);
        if (iconAnimCoroutine != null) StopCoroutine(iconAnimCoroutine);
        iconAnimCoroutine = StartCoroutine(AnimateIcon());
    }

    void StopIconAnimation()
    {
        if (iconAnimCoroutine != null) { StopCoroutine(iconAnimCoroutine); iconAnimCoroutine = null; }
        if (waveClearedIcon != null) waveClearedIcon.gameObject.SetActive(false);
    }

    IEnumerator AnimateIcon()
    {
        int frame = 0;
        float interval = 1f / Mathf.Max(iconAnimFPS, 1f);

        while (true)
        {
            waveClearedIcon.sprite = waveClearedIconFrames[frame];
            frame++;
            if (frame >= waveClearedIconFrames.Length)
            {
                if (iconAnimLoop) frame = 0;
                else { yield return new WaitForSecondsRealtime(interval); break; }
            }
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    // ==================== ENDING ====================

    IEnumerator EndingSequence()
    {
        gameEnded = true;
        SetMonsterState(1);
        yield return new WaitForSeconds(endingDelay);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            var pm = playerObj.GetComponent<PlayerMovement>(); if (pm != null) pm.enabled = false;
            var pa = playerObj.GetComponent<PlayerAttack>(); if (pa != null) pa.enabled = false;
            var fw = playerObj.GetComponentInChildren<FistsWeapon>(); if (fw != null) fw.enabled = false;
            var rb2 = playerObj.GetComponent<Rigidbody2D>(); if (rb2 != null) rb2.linearVelocity = Vector2.zero;
        }

        StartCoroutine(FadeAS(GetActiveMusic(), 0f, musicFadeDuration));
        if (levelMusic3 != null) { levelMusic3.volume = 0f; levelMusic3.Play(); StartCoroutine(FadeAS(levelMusic3, music3Vol, musicFadeDuration)); }

        DynamicCamera dynCam = FindObjectOfType<DynamicCamera>();
        Camera mainCam = Camera.main;
        if (dynCam != null) dynCam.enabled = false;
        if (mainCam != null && monsterTransform != null)
            yield return StartCoroutine(PanCamera(mainCam.transform, monsterTransform.position, cameraPanDuration));

        if (endingDialogueLines != null && endingDialogueLines.Length > 0)
            yield return StartCoroutine(PlayEndingDialogue());

        if (magicEffectPrefab != null && monsterTransform != null)
        {
            GameObject magic = Instantiate(magicEffectPrefab, monsterTransform.position + Vector3.up * 0.5f, Quaternion.identity);
            SpriteRenderer msr = magic.GetComponent<SpriteRenderer>();
            if (msr != null) msr.sortingOrder = 100;
            Destroy(magic, magicDisplayDuration + 2f);
        }
        if (magicSound != null) sfx.PlayOneShot(magicSound, magicSoundVolume);
        yield return new WaitForSeconds(magicDisplayDuration);

        if (mainCam != null && playerObj != null)
            yield return StartCoroutine(PanCamera(mainCam.transform, playerObj.transform.position, cameraReturnDuration));

        if (weaponPivot != null) weaponPivot.SetActive(false);
        if (endingDeathSound != null) sfx.PlayOneShot(endingDeathSound, endingDeathVolume);
        if (playerAnimator != null) playerAnimator.SetTrigger(endingDeathTrigger);
        yield return new WaitForSeconds(deathAnimWaitTime);

        if (levelMusic3 != null) StartCoroutine(FadeAS(levelMusic3, 0f, endFadeDuration));
        if (endFadePanel != null)
        {
            endFadePanel.gameObject.SetActive(true);
            float el = 0f;
            while (el < endFadeDuration) { el += Time.unscaledDeltaTime; endFadePanel.alpha = Mathf.Clamp01(el / endFadeDuration); yield return null; }
            endFadePanel.alpha = 1f;
        }
    }

    IEnumerator PanCamera(Transform cam, Vector3 target, float dur)
    { Vector3 s = cam.position; target.z = s.z; float e = 0f; while (e < dur) { e += Time.deltaTime; float t = e / dur; t = t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f; cam.position = Vector3.Lerp(s, target, t); yield return null; } cam.position = target; }

    IEnumerator PlayEndingDialogue()
    {
        for (int i = 0; i < endingDialogueLines.Length; i++)
        {
            SetDT(""); 
            foreach (char c in endingDialogueLines[i].ToCharArray())
            {
                SetDT(GetDT() + c);
                if (voiceClip != null && c != ' ') { sfx.pitch = 1f + Random.Range(-voicePitchVariation, voicePitchVariation); sfx.PlayOneShot(voiceClip, voiceVolume); }
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) { SetDT(endingDialogueLines[i]); break; }
                yield return new WaitForSeconds(typingSpeed);
            }
            yield return null;
            while (true) { if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break; yield return null; }
        }
        SetDT("");
    }

    void SetDT(string t) { if (endingDialogueTextUGUI != null) endingDialogueTextUGUI.text = t; if (endingDialogueTextWorld != null) endingDialogueTextWorld.text = t; }
    string GetDT() { if (endingDialogueTextUGUI != null) return endingDialogueTextUGUI.text; if (endingDialogueTextWorld != null) return endingDialogueTextWorld.text; return ""; }

    // ==================== UI ====================

    IEnumerator ShowWaveWarning()
    {
        if (warningSound != null) sfx.PlayOneShot(warningSound, warningVolume);
        if (waveWarningText != null) waveWarningText.text = $"WAVE {currentWaveIndex + 1}";
        if (warningCanvasGroup != null)
        { warningCanvasGroup.gameObject.SetActive(true); yield return FadeCG(warningCanvasGroup, 0f, 1f, 0.3f); yield return new WaitForSecondsRealtime(warningDuration); yield return FadeCG(warningCanvasGroup, 1f, 0f, 0.5f); warningCanvasGroup.gameObject.SetActive(false); }
        else yield return new WaitForSecondsRealtime(warningDuration);
    }

    IEnumerator ShowWaveCleared()
    {
        if (waveClearedSound != null) sfx.PlayOneShot(waveClearedSound, waveClearedVolume);
        if (waveClearedText != null) waveClearedText.text = "WAVE CLEARED!";

        // Icon animation
        StartIconAnimation();

        if (clearedCanvasGroup != null)
        { clearedCanvasGroup.gameObject.SetActive(true); yield return FadeCG(clearedCanvasGroup, 0f, 1f, 0.3f); yield return new WaitForSecondsRealtime(clearedDisplayDuration); yield return FadeCG(clearedCanvasGroup, 1f, 0f, 0.5f); clearedCanvasGroup.gameObject.SetActive(false); }
        else yield return new WaitForSecondsRealtime(clearedDisplayDuration);

        StopIconAnimation();
    }

    IEnumerator ShowCountdown()
    {
        SetCG(countdownCanvasGroup, 1f, true);
        if (skipHintText != null) skipHintText.text = "Press R to skip";
        float remaining = breakDuration;
        while (remaining > 0 && !skipRequested && !upgradePickedUp)
        { if (countdownText != null) countdownText.text = $"Next wave in: {Mathf.CeilToInt(remaining)}"; remaining -= Time.deltaTime; yield return null; }
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
