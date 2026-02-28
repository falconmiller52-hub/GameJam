using UnityEngine;
using UnityEngine.SceneManagement; // Обязательно для работы со сценами

public class MainMenu : MonoBehaviour
{
    [Tooltip("Имя сцены для загрузки при нажатии Play")]
    public string gameSceneName = "Level1";

    public void PlayGame()
    {
        // 🔥 ИСПРАВЛЕНО: Грузим по имени, а не по buildIndex+1
        // (buildIndex+1 сломается после добавления SplashScreen)
        SceneManager.LoadScene(gameSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT!");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
