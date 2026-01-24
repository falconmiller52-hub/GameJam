using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer sr; // Вернули SpriteRenderer
    private Camera mainCam; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>(); // Не забудьте получить компонент
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. Ввод движения
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float x = 0; float y = 0;
            if (keyboard.dKey.isPressed) x = 1;
            if (keyboard.aKey.isPressed) x = -1;
            if (keyboard.wKey.isPressed) y = 1;
            if (keyboard.sKey.isPressed) y = -1;
            movement = new Vector2(x, y);
        }

        // 2. Анимация бега
        animator.SetFloat("Speed", movement.sqrMagnitude);

        // 3. Поворот (Flip) по мышке
        FlipTowardsMouse();
    }

    void FlipTowardsMouse()
    {
        if (Mouse.current == null) return;

        // Получаем позицию мыши в мире
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);

        // Сравниваем X координаты
        // Если мышь правее игрока (mouse.x > transform.x) -> смотрим вправо (flipX = false)
        // Если мышь левее игрока (mouse.x < transform.x) -> смотрим влево (flipX = true)
        
        if (mouseWorldPos.x < transform.position.x)
        {
            sr.flipX = true;  // Отразить (смотрим влево)
        }
        else
        {
            sr.flipX = false; // Не отражать (смотрим вправо, как в оригинале)
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement.normalized * moveSpeed;
    }
}
