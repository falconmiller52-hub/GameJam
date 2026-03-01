using UnityEngine;
using UnityEngine.SceneManagement; // Обязательно для работы со сценами

public class MainMenu : MonoBehaviour
{
    [Tooltip("Имя сцены для загрузки при нажатии Play")]
    public string gameSceneName = "Level1";

    public void PlayGame()
    {
        // 📊 АНАЛИТИКА: игрок начал игру
        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.TrackGameStarted();

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
