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
        // 🔥 ИСПРАВЛЕНО: было "GameOver", такой сцены нет
        SceneManager.LoadScene("MainMenu"); 
    }
}
