using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    public Animator weaponAnimator; // Ссылка на АНИМАТОР КАТАНЫ
    public float attackRate = 0.5f; // Задержка между ударами

[Header("Audio")]
public AudioClip attackSound;   
public float attackVolume = 0.5f; // ← НОВОЕ ПОЛЕ!
private AudioSource audioSource; 


    private float nextAttackTime = 0f;
    private SwordDamage swordDamageScript; // Ссылка на скрипт урона

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // --- НОВОЕ: Ищем скрипт урона один раз при старте ---
        // Ищем в дочерних объектах (так как скрипт висит на Катане)
        swordDamageScript = GetComponentInChildren<SwordDamage>();
        
        if (swordDamageScript == null && weaponAnimator != null)
        {
             // Если не нашли сразу, попробуем поискать на объекте аниматора
             swordDamageScript = weaponAnimator.GetComponent<SwordDamage>();
        }
    }

void Update()
{
    // ← БЛОКИРУЕМ АТАКУ В ПАУЗЕ
    if (PauseMenu.isPaused) return;
    
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
        // --- НОВОЕ: Сбрасываем список ударенных врагов перед новым ударом ---
        if (swordDamageScript != null)
        {
            swordDamageScript.ResetAttack();
        }
        else
        {
            // На всякий случай пробуем найти еще раз, если при старте меча не было
            swordDamageScript = GetComponentInChildren<SwordDamage>();
            if (swordDamageScript != null) swordDamageScript.ResetAttack();
        }

        // Запускаем анимацию
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Attack");
        }

        // Проигрываем звук
        if (attackSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f); 
            audioSource.volume = attackVolume; // ← Применяем громкость
            audioSource.PlayOneShot(attackSound);
        }
    }
}
