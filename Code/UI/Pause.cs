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

        // Reticle нормальный порядок
        FixReticleOrder(10);

        if (pauseSFX != null) sfxSource.PlayOneShot(pauseSFX);
        if (musicSource != null) musicSource.UnPause();
            Cursor.visible = true; 
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        // Reticle ПОВЕРХ Dimmer (Player layer)
        FixReticleOrder(100);

        if (pauseSFX != null) sfxSource.PlayOneShot(pauseSFX);
        if (musicSource != null) musicSource.Pause();
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

    public void PlayButtonSound()
    {
        if (buttonSFX != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonSFX, buttonVolume);
        }
    }
}
