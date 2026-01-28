using UnityEngine;

public class MonsterEater : MonoBehaviour
{
    public static MonsterEater Instance { get; private set; }  // ✅ Singleton
    
    [Header("Feeding")]
    public AudioClip eatSound;
    
    // 🔥 ДОБАВЛЕНО: Ползунок громкости (от 0 до 2, где 1 = 100%)
    [Range(0f, 2f)] 
    public float eatVolume = 1.0f; 

    public ParticleSystem eatEffect;
    public float destroyDelay = 0.1f;

    private AudioSource audioSource;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // Опционально
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy") || other.GetComponent<EnemyHealth>() != null)
        {
            EatEnemy(other.gameObject);
        }
    }

    void EatEnemy(GameObject enemy)
    {
        Debug.Log("Монстр съел: " + enemy.name);

        if (eatSound != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            // 🔥 ИЗМЕНЕНО: используем переменную вместо числа 1.2f
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
