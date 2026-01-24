using UnityEngine;
using UnityEngine.SceneManagement;

public class AnimationEndListener : MonoBehaviour
{
    [Tooltip("Сколько длится анимация засасывания в секундах")]
    public float animationDuration = 5f; 

    void Start()
    {
        // Запускаем таймер сразу при старте сцены
        Invoke("LoadGameOverScreen", animationDuration);
    }

    void LoadGameOverScreen()
    {
        // Загружаем сцену с надписью "ВЫ СЪЕДЕНЫ"
        // Убедитесь, что сцена называется именно так
        SceneManager.LoadScene("GameOver"); 
    }
}
