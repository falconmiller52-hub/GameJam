using UnityEngine;

public class DynamicCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform reticle;

    [Header("Settings")]
    [Range(0f, 1f)] public float bias = 0.25f;
    public float smoothSpeed = 5f;

    [Header("Limits")]
    public BoxCollider2D mapBounds; // Сюда перетащим нашу рамку

    private float camHalfHeight;
    private float camHalfWidth;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        // Вычисляем размеры камеры в мировых единицах
        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;
    }

    void FixedUpdate()
    {
        if (player == null || reticle == null) return;

        // 1. Стандартная логика слежения (как было раньше)
        Vector3 targetPos = Vector3.Lerp(player.position, reticle.position, bias);

        // 2. Ограничение (Clamping)
        if (mapBounds != null)
        {
            // Получаем границы нашего коллайдера
            Bounds bounds = mapBounds.bounds;

            // Математика:
            // minX = левая граница рамки + половина ширины камеры
            // maxX = правая граница рамки - половина ширины камеры
            // Это гарантирует, что КРАЙ камеры никогда не выйдет за КРАЙ рамки.

            float minX = bounds.min.x + camHalfWidth;
            float maxX = bounds.max.x - camHalfWidth;
            float minY = bounds.min.y + camHalfHeight;
            float maxY = bounds.max.y - camHalfHeight;

            // Mathf.Clamp(значение, мин, макс) держит число в рамках
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        // 3. Сохраняем Z и применяем
        targetPos.z = transform.position.z;
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.fixedDeltaTime);
    }
}
