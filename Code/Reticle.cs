using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Reticle : MonoBehaviour
{
    private RectTransform rectTransform;

    void Start()
    {
        Cursor.visible = false;
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            rectTransform.position = mousePos;
        }
    }
}
