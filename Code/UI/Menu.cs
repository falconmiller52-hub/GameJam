using UnityEngine;
using UnityEngine.SceneManagement; // Обязательно для работы со сценами

public class MainMenu : MonoBehaviour
{
    // Эту функцию мы привяжем к кнопке
    public void PlayGame()
    {
        // Загружаем следующую сцену по индексу из Build Settings
        // Это удобнее, чем писать имя сцены строкой ("Level1"), так как имена могут меняться
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT!"); // Чтобы видеть работу в редакторе
        Application.Quit();
    }
}
