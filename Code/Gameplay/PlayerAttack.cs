using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    [Header("Settings")]
    public Animator weaponAnimator; // Ссылка на АНИМАТОР КАТАНЫ (не игрока!)
    public float attackRate = 0.5f; // Задержка между ударами (секунды)

    private float nextAttackTime = 0f;

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
        if (weaponAnimator != null)
        {
            // Запускаем триггер, который мы создали в Шаге 2
            weaponAnimator.SetTrigger("Attack");
        }
    }
}
