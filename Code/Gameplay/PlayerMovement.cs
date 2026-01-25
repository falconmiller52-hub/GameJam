using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))] // Гарантирует, что RB есть
public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;

    [Header("References (Auto-filled)")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer sr;
    
    private Vector2 moveInput;
    private Camera mainCam;

    void Awake()
    {
        // Получаем компоненты в Awake (раньше, чем Start)
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        // ПРИНУДИТЕЛЬНАЯ НАСТРОЙКА ФИЗИКИ
        // Это лечит "залипание" игрока
        rb.gravityScale = 0f; 
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Плавность
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Только вращение лочим
    }

    void Start()
    {
        // Страховка от остановки времени
        Time.timeScale = 1f; 
    }

    void Update()
    {
        // 1. Считываем ввод (New Input System)
        if (Keyboard.current != null)
        {
            float x = 0f;
            float y = 0f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;

            // Нормализуем, чтобы по диагонали не было быстрее
            moveInput = new Vector2(x, y).normalized;
        }
        else
        {
            moveInput = Vector2.zero;
        }

        // 2. Обновляем Аниматор (БЕЗОПАСНО)
        if (animator != null)
        {
            // Используем magnitude, это надежнее
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        // 3. Флип спрайта по мышке
        FlipSprite();
    }

    void FixedUpdate()
    {
        // ФИЗИКА ТОЛЬКО ЗДЕСЬ
        // Простое и надежное движение через velocity
        if (rb != null)
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
    }

    void FlipSprite()
    {
        if (sr == null || mainCam == null || Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);

        // Простая логика: мышь слева -> флип
        sr.flipX = mouseWorldPos.x < transform.position.x;
    }
}
