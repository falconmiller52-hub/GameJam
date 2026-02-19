using UnityEngine;
using UnityEngine.InputSystem;

public class DynamicCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;

    [Header("Mouse Tracking")]
    [Range(0f, 1f)] public float mouseBias = 0.25f; // Вместо reticle bias

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

    void FixedUpdate()
    {
        if (player == null) return;

        // 🔥 НОВАЯ ЛОГИКА: Игрок + позиция мыши
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = 0f; // Важно для 2D!

        // Интерполируем между игроком и мышью
        Vector3 targetPos = Vector3.Lerp(player.position, mouseWorldPos, mouseBias);

        // Ограничения (как было)
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
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.fixedDeltaTime);
    }
}
