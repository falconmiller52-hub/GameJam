using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Атака катаной. 
/// ИСПРАВЛЕНО: атакует ТОЛЬКО когда катана активна (проверяет WeaponSwitcher).
/// Это предотвращает звук катаны при ударе кулаками.
/// </summary>
public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    public Animator weaponAnimator;
    public float attackRate = 0.5f;

    [Header("Audio")]
    public AudioClip attackSound;
    public float attackVolume = 0.5f;
    private AudioSource audioSource;

    private float nextAttackTime = 0f;
    private SwordDamage swordDamageScript;
    private WeaponSwitcher weaponSwitcher; // 🔥 НОВОЕ

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        swordDamageScript = GetComponentInChildren<SwordDamage>();
        if (swordDamageScript == null && weaponAnimator != null)
            swordDamageScript = weaponAnimator.GetComponent<SwordDamage>();

        // 🔥 Ищем WeaponSwitcher
        weaponSwitcher = GetComponent<WeaponSwitcher>();
        if (weaponSwitcher == null)
            weaponSwitcher = GetComponentInChildren<WeaponSwitcher>();
    }

    void Update()
    {
        if (PauseMenu.isPaused) return;

        // 🔥 Если кулаки активны — не атакуем катаной!
        if (weaponSwitcher != null && weaponSwitcher.IsFistsActive())
            return;

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
        if (swordDamageScript != null)
        {
            swordDamageScript.ResetAttack();
        }
        else
        {
            swordDamageScript = GetComponentInChildren<SwordDamage>();
            if (swordDamageScript != null) swordDamageScript.ResetAttack();
        }

        if (weaponAnimator != null)
            weaponAnimator.SetTrigger("Attack");

        if (attackSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.volume = attackVolume;
            audioSource.PlayOneShot(attackSound);
        }
    }
}
