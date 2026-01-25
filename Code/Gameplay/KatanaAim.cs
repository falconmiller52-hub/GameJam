using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponAim : MonoBehaviour
{
    private Camera mainCam;
    public Transform weaponContainer; // Ссылка на WeaponContainer (ребенок)

    void Start()
    {
        mainCam = Camera.main;
        // Автопоиск, если забыли привязать
        if (weaponContainer == null && transform.childCount > 0)
             weaponContainer = transform.GetChild(0); 
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // 1. Вращение ПИВОТА за мышкой
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector3 direction = mousePos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 2. Флип (Отражение) КОНТЕЙНЕРА
        // Мы отражаем не спрайт, а контейнер по оси Y. 
        // Это позволяет анимации работать корректно в любую сторону.
        if (weaponContainer != null)
        {
            Vector3 scale = weaponContainer.localScale;
            
            // Если угол смотрит влево (от 90 до 180 или от -90 до -180)
            if (Mathf.Abs(angle) > 90)
            {
                scale.y = -1f; // Переворачиваем "вверх ногами" локально, чтобы лезвие смотрело правильно
            }
            else
            {
                scale.y = 1f;
            }
            weaponContainer.localScale = scale;
        }
    }
}
