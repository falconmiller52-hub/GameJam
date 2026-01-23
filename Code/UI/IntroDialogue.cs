using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class IntroDialogue : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI textDisplay;
    public GameObject startButton;
    public float typingSpeed = 0.05f;

    [Header("Content")]
    [TextArea(3, 10)]
    public string[] sentences;

    private int index;
    private bool isTyping;
    private bool isDialogueActive = false; // Главный рубильник

    void Start()
    {
        // На старте скрипт должен молчать и ничего не делать
        startButton.SetActive(false);
        textDisplay.text = "";
        isDialogueActive = false; 
    }

    // Этот метод мы вызовем из другого скрипта, когда придет время
    public void BeginDialogue()
    {
        index = 0;
        isDialogueActive = true;
        StartCoroutine(Type());
    }

    void Update()
    {
        // Если диалог не активен — игнорируем любые клики здесь
        if (!isDialogueActive) return;

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
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    void NextSentence()
    {
        if (index < sentences.Length - 1)
        {
            index++;
            textDisplay.text = "";
            StartCoroutine(Type());
        }
        else
        {
            textDisplay.text = "";
            startButton.SetActive(true);
            isDialogueActive = false; // Диалог окончен, клики больше не нужны
        }
    }
}
