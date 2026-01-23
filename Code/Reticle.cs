using UnityEngine;
using UnityEngine.InputSystem; // Если используете New Input System

public class ReticleController : MonoBehaviour
{
    void Start()
    {
        // Скрываем системный курсор, так как у нас есть свой
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Получаем позицию мыши на экране (в пикселях)
        // Для старой системы: Vector2 mousePos = Input.mousePosition;
        // Для новой системы (раз у вас была ошибка, скорее всего активна она):
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2. Переводим экранные координаты (пиксели) в мировые (Unity Units)
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        // 3. Обнуляем Z, чтобы курсор не улетал вглубь камеры
        worldPos.z = 0;

        // 4. Применяем позицию
        transform.position = worldPos;
    }
}
