using UnityEngine;
using UnityEngine.InputSystem;

public class Reticle : MonoBehaviour
{
    private Camera mainCam;
    
    void Start()
    {
        mainCam = Camera.main;
        Cursor.visible = false;  // СКРЫВАЕМ системный
    }

    void Update()
    {
        // Следует за мышкой ВСЕГДА (даже в паузе)
        if (mainCam != null && Mouse.current != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 worldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
            worldPos.z = 0f;
            transform.position = worldPos;
        }
    }
}
