using UnityEngine;

public class PlaySoundOnEvent : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Эту функцию мы вызовем из анимации
    public void PlayAttackSound()
    {
        if (audioSource != null)
        {
            // Randomize pitch - чтобы каждый удар звучал немного по-разному (круто для ушей)
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.Play();
        }
    }
}
