using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // Для работы с новой системой ввода

public class IntroDialogue : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI textDisplay; // Ссылка на текстовое поле
    public GameObject startButton;      // Кнопка, появляющаяся в конце (может быть null)
    public float typingSpeed = 0.05f;   // Скорость печати букв

    [Header("Content")]
    [TextArea(3, 10)]
    public string[] sentences;          // Массив фраз

    // Публичный флаг, чтобы другие скрипты (например, GameOverDirector) знали, что диалог кончился
    public bool IsFinished = false;

    private int index;
    private bool isTyping;
    private bool isDialogueActive = false; // "Рубильник", разрешающий обработку кликов

    void Start()
    {
        // При старте уровня диалог должен молчать и ждать команды
        if (startButton != null) 
            startButton.SetActive(false);
        
        textDisplay.text = "";
        isDialogueActive = false;
        IsFinished = false;
    }

    // Метод для внешнего запуска диалога (из PreIntroSequence или GameOverDirector)
    public void BeginDialogue()
    {
        index = 0;
        isDialogueActive = true;
        IsFinished = false;
        
        // Очищаем текст на всякий случай
        textDisplay.text = "";
        
        // Запускаем печать первой фразы
        StartCoroutine(Type());
    }

    void Update()
    {
        // Если диалог не активен — игнорируем любые действия
        if (!isDialogueActive) return;

        // Проверка клика левой кнопкой мыши (New Input System)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                // Если кликнули во время печати -> показываем всю фразу мгновенно
                StopAllCoroutines();
                textDisplay.text = sentences[index];
                isTyping = false;
            }
            else
            {
                // Если фраза уже напечатана -> переходим к следующей
                NextSentence();
            }
        }
    }

    IEnumerator Type()
    {
        isTyping = true;
        textDisplay.text = "";

        // Посимвольный вывод текста
        foreach (char letter in sentences[index].ToCharArray())
        {
            textDisplay.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void NextSentence()
    {
        // Если это не последняя фраза в списке
        if (index < sentences.Length - 1)
        {
            index++;
            textDisplay.text = "";
            StartCoroutine(Type());
        }
        else
        {
            // КОНЕЦ ДИАЛОГА
            textDisplay.text = ""; // Очищаем текст (или можно оставить последнюю фразу)
            
            // Если есть кнопка (например, "Играть" в меню) — показываем её
            if (startButton != null) 
                startButton.SetActive(true);
            
            // Выключаем активность диалога, чтобы клики больше не обрабатывались
            isDialogueActive = false;
            
            // Ставим флаг завершения (для катсцен)
            IsFinished = true;
        }
    }
}
