using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameOverDirector : MonoBehaviour
{
    [Header("Actors")]
    public Transform player;           // Игрок
    public Transform monster;          // Монстр за картой
    public Camera mainCamera;          // Главная камера
    public MonoBehaviour cameraScript; // Скрипт DynamicCamera (чтобы его отключить)
    
    [Header("UI")]
    public IntroDialogue monsterDialogue; // Скрипт диалога монстра
    public GameObject dialogueVisuals;    // Визуал диалога
    public CanvasGroup fadePanel;         // Черная панель (добавьте компонент CanvasGroup на Panel)

    [Header("Settings")]
    public float cameraMoveSpeed = 2f;
    public float playerAbsorbSpeed = 5f;

    // Singleton (чтобы легко вызвать из PlayerHealth)
    public static GameOverDirector Instance;

    void Awake()
    {
        Instance = this;
    }

    // Эту функцию вызовет скрипт здоровья при смерти
    public void StartDeathSequence()
    {
        StartCoroutine(SequenceRoutine());
    }

    IEnumerator SequenceRoutine()
    {
        // 1. Отключаем управление игрока и физику
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        player.GetComponent<PlayerMovement>().enabled = false; // Откл. управление
        player.GetComponent<Collider2D>().enabled = false;     // Откл. стены (чтобы пролетел сквозь них)
        
        // Отключаем скрипт слежения камеры, теперь мы управляем ей вручную
        if (cameraScript != null) cameraScript.enabled = false;

        // 2. Камера летит к Монстру
        yield return MoveTransform(mainCamera.transform, new Vector3(monster.position.x, monster.position.y, -10), cameraMoveSpeed);

        // 3. Запускаем диалог
        dialogueVisuals.SetActive(true);
        monsterDialogue.BeginDialogue();

        // Ждем, пока диалог не закончится.
        // Для этого нам нужно добавить небольшое свойство в IntroDialogue (см. ниже Шаг 3)
        while (!monsterDialogue.IsFinished) 
        {
            yield return null;
        }

        dialogueVisuals.SetActive(false);

        // 4. Камера возвращается к Игроку
        yield return MoveTransform(mainCamera.transform, new Vector3(player.position.x, player.position.y, -10), cameraMoveSpeed);
        
        // Пауза для драматизма
        yield return new WaitForSeconds(0.5f);

        // 5. Игрок летит в пасть Монстра ("Поглощение")
        // Мы используем true в конце, чтобы объект исчез, когда долетит
        yield return MoveTransform(player, monster.position, playerAbsorbSpeed, true);

        // 6. Затемнение экрана
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            fadePanel.alpha = t;
            yield return null;
        }

        // 7. Ждем 2 секунды в темноте
        yield return new WaitForSeconds(2f);

        // 8. Загружаем сцену Game Over (убедитесь, что она есть в Build Settings)
        SceneManager.LoadScene("DeathDialogue");
    }

    // Универсальная корутина для плавного перемещения объектов
    IEnumerator MoveTransform(Transform target, Vector3 destination, float speed, bool destroyAtEnd = false)
    {
        while (Vector3.Distance(target.position, destination) > 0.1f)
        {
            target.position = Vector3.MoveTowards(target.position, destination, speed * Time.deltaTime);
            yield return null;
        }
        
        if (destroyAtEnd)
        {
            target.gameObject.SetActive(false); // Игрок исчез
        }
    }
}
