using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ReticleUI : MonoBehaviour
{
    [Header("UI")]
    public Image reticleImage;
    public RectTransform rectTransform;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        Cursor.visible = false;

        // Автопривязка
        if (reticleImage == null) reticleImage = GetComponent<Image>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (mainCam == null || Mouse.current == null) return;

        // ScreenPoint → Canvas Position
        Vector2 mousePos = Mouse.current.position.ReadValue();
        rectTransform.position = mousePos;
    }
}
