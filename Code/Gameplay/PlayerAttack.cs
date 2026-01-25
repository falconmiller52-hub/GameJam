using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    public Animator weaponAnimator; // Ссылка на АНИМАТОР КАТАНЫ (дочерний объект)
    public float attackRate = 0.5f; // Задержка между ударами

    [Header("Audio")]
    public AudioClip attackSound;   // Звук "Вжух"
    private AudioSource audioSource; // Компонент для проигрывания

    private float nextAttackTime = 0f;

    void Start()
    {
        // Пытаемся найти AudioSource на игроке
        audioSource = GetComponent<AudioSource>();
        
        // Если забыли добавить в редакторе — добавляем программно, чтобы не было ошибок
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        // Проверяем клик (LMB) + кулдаун
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + attackRate;
            }
        }
    }

    void Attack()
    {
        // 1. Запускаем анимацию (Ваш старый код)
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Attack");
        }

        // 2. Проигрываем звук (Новый код)
        if (attackSound != null && audioSource != null)
        {
            // Используем PlayOneShot, чтобы звуки могли накладываться
            // Random.Range меняет высоту тона для разнообразия (опционально)
            audioSource.pitch = Random.Range(0.9f, 1.1f); 
            audioSource.PlayOneShot(attackSound);
        }
    }
}
