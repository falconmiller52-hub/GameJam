using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ИСПРАВЛЕНО: Камера теперь в LateUpdate вместо FixedUpdate.
/// FixedUpdate вызывается 50 раз/сек, рендер — 60-144+.
/// Это вызывало рассинхрон и "дёрганье" спрайта.
/// LateUpdate вызывается каждый кадр ПОСЛЕ Update — идеально для камеры.
/// </summary>
public class DynamicCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;

    [Header("Mouse Tracking")]
    [Range(0f, 1f)] public float mouseBias = 0.25f;

    [Header("Settings")]
    public float smoothSpeed = 5f;

    [Header("Limits")]
    public BoxCollider2D mapBounds;

    private float camHalfHeight;
    private float camHalfWidth;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;
    }

    // 🔥 LateUpdate вместо FixedUpdate — убирает дёрганье
    void LateUpdate()
    {
        if (player == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f;

        Vector3 targetPos = Vector3.Lerp(player.position, mouseWorldPos, mouseBias);

        if (mapBounds != null)
        {
            Bounds bounds = mapBounds.bounds;
            float minX = bounds.min.x + camHalfWidth;
            float maxX = bounds.max.x - camHalfWidth;
            float minY = bounds.min.y + camHalfHeight;
            float maxY = bounds.max.y - camHalfHeight;

            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);
        }

        targetPos.z = transform.position.z;
        // 🔥 Time.deltaTime вместо Time.fixedDeltaTime
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}
