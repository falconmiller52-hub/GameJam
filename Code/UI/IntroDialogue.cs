using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class IntroDialogue : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI textDisplay;
    public GameObject startButton;  // FIGHT кнопка
    public float typingSpeed = 0.05f;

    [Header("Content")]
    [TextArea(3, 10)]
    public string[] sentences;

    [Header("Monster Visibility")]
public SpriteRenderer monsterSpriteRenderer; // Перетащи SpriteRenderer Монстра


    [Header("Undertale Voice")]
    public AudioClip voiceClip;
    public float voicePitchVariation = 0.2f;
    public float voiceVolume = 0.7f;

    [Header("Землетрясение (вступление)")]
    public AudioClip earthquakeSound;
    public Transform background;
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.3f;

    [Header("Fight Button Sound")]
    public AudioClip fightSound;
    public float fightVolume = 1.2f;

    [Header("Monster Animation (Fallback)")]
    public Transform monsterTransform;
    public float monsterPulseSpeed = 3f;
    public float monsterPulseScale = 1.2f;

    [Header("Monster Animator (Primary)")]
    public Animator monsterAnimator;
    public string fightTriggerName = "FightReady";

    [Header("Fade Transition")]
    public CanvasGroup fadePanel;
    public float fadeDuration = 1f;
    public string nextSceneName = "MainLevel";

    public bool IsFinished = false;

    private int index;
    private bool isTyping;
    private bool isDialogueActive = false;
    private AudioSource audioSource;
    private bool isMonsterAnimating = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    // 🔥 СКРЫВАЕМ МОНСТРА В НАЧАЛЕ
    if (monsterSpriteRenderer != null)
    {
        monsterSpriteRenderer.enabled = false;
    }
        if (startButton != null) 
        {
            startButton.SetActive(false);
            Button fightBtn = startButton.GetComponent<Button>();
            if (fightBtn != null)
            {
                fightBtn.onClick.AddListener(() => PlayFightSound());
            }
        }
        
        textDisplay.text = "";
        isDialogueActive = false;
        IsFinished = false;
    }

    void PlayFightSound()
    {
        if (fightSound != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(fightSound, fightVolume);
        }
        
        // Затемнение + загрузка уровня!
        StartCoroutine(FadeToLevel());
    }

    IEnumerator FadeToLevel()
    {
        if (fadePanel == null)
        {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadePanel.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    public void BeginDialogue()
    {
        index = 0;
        isDialogueActive = true;
        IsFinished = false;
        textDisplay.text = "";
        StartCoroutine(Type());
    }

    public void PlayIntroClickEffect()
    {
        if (earthquakeSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(earthquakeSound);
        }
        if (background != null) StartCoroutine(ShakeBackground());
    }

    void Update()
    {
        if (!isDialogueActive) return;

        if (isMonsterAnimating && monsterTransform != null)
        {
            float pulse = Mathf.Sin(Time.time * monsterPulseSpeed) * 0.1f;
            monsterTransform.localScale = Vector3.one * (1f + pulse);
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                StopAllCoroutines();
                textDisplay.text = sentences[index];
                isTyping = false;
            }
            else
            {
                NextSentence();
            }
        }
    }

    IEnumerator Type()
    {
        isTyping = true;
        textDisplay.text = "";

        foreach (char letter in sentences[index].ToCharArray())
        {
            textDisplay.text += letter;
            
            if (voiceClip != null)
            {
                audioSource.pitch = 1f + Random.Range(-voicePitchVariation, voicePitchVariation);
                audioSource.PlayOneShot(voiceClip, voiceVolume);
            }
            
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    IEnumerator ShakeBackground()
    {
        Vector3 originalPos = background.localPosition;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            background.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        background.localPosition = originalPos;
    }

    void NextSentence()
{
    if (index < sentences.Length - 1)
    {
        index++;
        
        // 🔥 ПОКАЗЫВАЕМ МОНСТРА НА 3-Й РЕПЛИКЕ (index == 2)
        // if (index == 1 && monsterSpriteRenderer != null)
        
        textDisplay.text = "";
        StartCoroutine(Type());
    }
    else
    {
        textDisplay.text = "";
        if (startButton != null) 
        {
            startButton.SetActive(true);
            
            // Animator триггер
            if (monsterAnimator != null)
            {
                monsterAnimator.SetTrigger(fightTriggerName);
            }
            else if (monsterTransform != null)
            {
                isMonsterAnimating = true;
            }
        }
        isDialogueActive = false;
        IsFinished = true;
    }
}

}
