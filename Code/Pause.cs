using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject pauseMenuUI;

    [Header("Audio Settings")]
    public AudioClip pauseSFX;
    public AudioClip buttonSFX;
    public float buttonVolume = 0.6f;
    public AudioSource musicSource;

    [Header("Gameplay")]
    public GameObject reticleObject;

    private AudioSource sfxSource;
    public static bool isPaused = false;

    void Start()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (musicSource == null)
        {
            GameObject musicObj = GameObject.Find("LevelMusic");
            if (musicObj != null) musicSource = musicObj.GetComponent<AudioSource>();
        }

        if (reticleObject == null)
        {
            reticleObject = GameObject.Find("Reticle");
        }

        Resume();
    }

    void Update()
    {
        if (!isPaused && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Pause();
        }
        else if (isPaused && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Resume();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        FixReticleOrder(10);

        if (pauseSFX != null) sfxSource.PlayOneShot(pauseSFX);
        if (musicSource != null) musicSource.UnPause();
        
        // В геймплее курсор обычно скрыт, если у тебя свой прицел
        Cursor.visible = false; 
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        FixReticleOrder(100);

        if (pauseSFX != null) sfxSource.PlayOneShot(pauseSFX);
        if (musicSource != null) musicSource.Pause();
        
        // В меню курсор должен быть виден, чтобы нажимать кнопки
        Cursor.visible = true;
    }

    private void FixReticleOrder(int order)
    {
        if (reticleObject != null)
        {
            SpriteRenderer sr = reticleObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingLayerName = "Player";
                sr.sortingOrder = order;
            }
        }
    }

    // Эта функция для кнопки "В Главное Меню"
    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(0); // Грузит сцену с индексом 0
    }

    // 🔥 Эту функцию привяжи к кнопке "Выход" (Quit)
    public void QuitToDesktop()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
    
    // Оставим старое имя для совместимости, если где-то уже привязано
    public void QuitGame()
    {
        QuitToDesktop();
    }

    public void PlayButtonSound()
    {
        if (buttonSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonSFX, buttonVolume);
        }
    }
}
