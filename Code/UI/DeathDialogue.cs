using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathDialogueManager : MonoBehaviour
{
    public IntroDialogue dialogueScript; // Ссылка на скрипт диалога
    public GameObject fadePanel;         // Черная панель для ухода в затемнение

    void Start()
    {
        // Сразу запускаем диалог при старте сцены
        dialogueScript.BeginDialogue();
        StartCoroutine(CheckForEnd());
    }

    IEnumerator CheckForEnd()
    {
        // Ждем, пока флаг IsFinished в скрипте диалога не станет true
        while (!dialogueScript.IsFinished)
        {
            yield return null;
        }

        // Диалог кончился. Начинаем затемнение.
        if (fadePanel != null) fadePanel.SetActive(true);
        
        yield return new WaitForSeconds(2f); // Ждем 2 секунды в темноте

        // Переход к анимации
        SceneManager.LoadScene("DeathAnimationScene");
    }
}
