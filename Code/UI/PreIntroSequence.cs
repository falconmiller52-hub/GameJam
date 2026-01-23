using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PreIntroSequence : MonoBehaviour
{
    [Header("UI Links")]
    public TextMeshProUGUI centerText;
    public IntroDialogue dialogueScript; 
    public GameObject dialogueVisuals; 

    [Header("Audio")]
    public AudioSource menuMusic; // НОВОЕ: Ссылка на музыку

    [Header("Settings")]
    public float waitTime = 3f;
    public float blinkSpeed = 5f;

    private int step = 0;
    private bool waitingForClick = false;
    private Color originalColor;

    void Start()
    {
        if(dialogueVisuals != null) 
            dialogueVisuals.SetActive(false);

        if (centerText != null) originalColor = centerText.color;

        ShowPhase(1);
    }

    void Update()
    {
        if (waitingForClick)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
            centerText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                NextPhase();
            }
        }
    }

    void ShowPhase(int phaseNum)
    {
        centerText.gameObject.SetActive(true);
        waitingForClick = true;

        switch (phaseNum)
        {
            case 1: centerText.text = "НАЖМИТЕ, ЧТОБЫ НАЧАТЬ"; break;
            case 2: centerText.text = "НАЖМИТЕ ЕЩЕ РАЗ, ЧТОБЫ НАЧАТЬ"; break;
            case 3: centerText.text = "НАЖМИТЕ СИЛЬНЕЕ, ЧТОБЫ НАЧАТЬ"; break;
        }
    }

    void NextPhase()
    {
        waitingForClick = false;
        centerText.gameObject.SetActive(false);
        centerText.color = originalColor; 
        
        step++;

        if (step < 3)
        {
            StartCoroutine(WaitAndShowNext(step + 1));
        }
        else
        {
            StartCoroutine(StartMainDialogue());
        }
    }

    IEnumerator WaitAndShowNext(int nextPhaseIndex)
    {
        yield return new WaitForSeconds(waitTime);
        ShowPhase(nextPhaseIndex);
    }

    IEnumerator StartMainDialogue()
    {
        yield return new WaitForSeconds(waitTime);

        centerText.text = "";
        centerText.gameObject.SetActive(false);

        if(dialogueVisuals != null) 
            dialogueVisuals.SetActive(true);
        
        // НОВОЕ: Запускаем музыку
        if (menuMusic != null)
        {
            menuMusic.Play();
        }
        
        yield return null; 
        
        dialogueScript.BeginDialogue();
        this.enabled = false;
    }
}
