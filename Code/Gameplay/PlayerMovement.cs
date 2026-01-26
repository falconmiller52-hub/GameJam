using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections; // Нужно для корутин

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;      // Скорость рывка (гораздо быстрее бега)
    public float dashDuration = 0.2f;  // Длительность рывка (очень короткая)
    public float dashCooldown = 1f;    // Время перезарядки
    public bool isDashing = false;     // Флаг, что мы в рывке

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer sr;
    
    private Vector2 moveInput;
    private Camera mainCam;
    private bool canDash = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        // Настройки физики
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Если мы в рывке, запрещаем менять направление или запускать новый
        if (isDashing) return;

        // 1. Считываем ввод (WASD)
        float x = 0f, y = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            
            // ПРОВЕРКА НА РЫВОК (SPACE)
            if (Keyboard.current.spaceKey.wasPressedThisFrame && canDash)
            {
                StartCoroutine(Dash());
            }
        }
        
        moveInput = new Vector2(x, y).normalized;

        // 2. Анимация бега
        if (animator != null)
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        // 3. Флип спрайта по мышке
        FlipSprite();
    }

    void FixedUpdate()
    {
        // Если мы в рывке, физикой управляет корутина Dash(), не вмешиваемся
        if (isDashing) return;

        if (rb != null)
        {
            rb.linearVelocity = moveInput * moveSpeed; // В Unity 6 linearVelocity, в старой velocity
        }
    }

    void FlipSprite()
    {
        if (sr == null || mainCam == null || Mouse.current == null) return;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);
        sr.flipX = mouseWorldPos.x < transform.position.x;
    }

    // --- ЛОГИКА РЫВКА ---
    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        // 1. Определяем направление рывка (к курсору мыши)
        Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dashDir = (mousePos - (Vector2)transform.position).normalized;

        // 2. Игнорируем коллизии с врагами (Layer 6 = Player, Layer 7 = Enemy - проверьте номера слоев!)
        // Лучше использовать Physics2D.IgnoreLayerCollision, но для этого надо знать ID слоев.
        // Простой способ: Игнорируем слой "Enemy"
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");
        
        if (enemyLayer != -1 && playerLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        // 3. Запускаем анимацию
        if (animator != null) animator.SetTrigger("Dash");

        // 4. Применяем скорость
        rb.linearVelocity = dashDir * dashSpeed;

        // 5. Ждем окончания рывка
        yield return new WaitForSeconds(dashDuration);

        // 6. Завершаем рывок
        if (enemyLayer != -1 && playerLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false); // Включаем коллизии обратно

        rb.linearVelocity = Vector2.zero; // Останавливаемся (опционально)
        isDashing = false;

        // 7. Кулдаун
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
