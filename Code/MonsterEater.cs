using UnityEngine;

/// <summary>
/// FIXED: Removed DontDestroyOnLoad — it was causing duplication on scene reload
/// and breaking Monster reference after GameOver.
/// </summary>
public class MonsterEater : MonoBehaviour
{
    public static MonsterEater Instance { get; private set; }

    [Header("Feeding")]
    public AudioClip eatSound;
    [Range(0f, 2f)] public float eatVolume = 1.0f;
    public ParticleSystem eatEffect;
    public float destroyDelay = 0.1f;

    private AudioSource audioSource;

    void Awake()
    {
        // Singleton WITHOUT DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.GetComponent<EnemyHealth>() != null)
            EatEnemy(other.gameObject);
    }

    void EatEnemy(GameObject enemy)
    {
        if (eatSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(eatSound, eatVolume);
        }

        if (eatEffect != null)
        {
            eatEffect.transform.position = enemy.transform.position;
            eatEffect.Play();
        }

        Destroy(enemy, destroyDelay);
    }
}
