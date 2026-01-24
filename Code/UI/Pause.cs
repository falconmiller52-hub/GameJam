using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI Reference")]
    public GameObject pauseMenuUI;

    [Header("Audio Settings")]
    public AudioClip pauseSFX;       // Звук "пшшш" при открытии/закрытии
    public AudioSource musicSource;  // Ссылка на музыку уровня (чтобы её останавливать)
    
    private AudioSource sfxSource;   // Источник для проигрывания SFX меню
    public static bool isPaused = false;

    void Start()
    {
        // Создаем AudioSource для звуков меню "на лету", если его нет
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // Ищем музыку уровня автоматически, если вы забыли перетащить её вручную
        if (musicSource == null)
        {
            // Ищем объект с именем "LevelMusic" (как мы называли ранее)
            GameObject musicObj = GameObject.Find("LevelMusic");
            if (musicObj != null)
                musicSource = musicObj.GetComponent<AudioSource>();
        }

        Resume();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        // 1. Воспроизводим звук меню (если он есть)
        if (pauseSFX != null) sfxSource.PlayOneShot(pauseSFX);

        // 2. Возвращаем музыку (UnPause)
        if (musicSource != null) musicSource.UnPause();
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // 1. Воспроизводим звук меню
        if (pauseSFX != null) sfxSource.PlayOneShot(pauseSFX);

        // 2. Ставим музыку на Паузу (не Stop, а Pause!)
        if (musicSource != null) musicSource.Pause();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
