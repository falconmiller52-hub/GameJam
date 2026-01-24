using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverClick : MonoBehaviour
{
    public void LoadMenu()
    {
        // Загружаем сцену 0 (Главное меню)
        SceneManager.LoadScene(0);
    }
}
