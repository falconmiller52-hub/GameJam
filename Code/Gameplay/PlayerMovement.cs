using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public bool isDashing = false;

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
        // ✅ ВВОД РАБОТАЕТ ВСЕГДА (даже пауза)
        // ❌ УБРАЛИ: if (PauseMenu.isPaused) return;

        if (isDashing) return;

        // WASD ввод
        float x = 0f, y = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            
            // DASH ТОЛЬКО вне паузы
            if (!PauseMenu.isPaused && Keyboard.current.spaceKey.wasPressedThisFrame && canDash)
            {
                StartCoroutine(Dash());
            }
        }
        
        moveInput = new Vector2(x, y).normalized;

        // Анимация
        if (animator != null)
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        FlipSprite();
    }

    void FixedUpdate()
    {
        // ✅ ФИЗИКА БЛОКИРУЕТСЯ в паузе
        if (PauseMenu.isPaused || isDashing) return;

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
        sr.flipX = mouseWorldPos.x < transform.position.x;
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        Vector2 mousePos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 dashDir = (mousePos - (Vector2)transform.position).normalized;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int playerLayer = LayerMask.NameToLayer("Player");
        
        if (enemyLayer != -1 && playerLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        if (animator != null) animator.SetTrigger("Dash");

        rb.linearVelocity = dashDir * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        if (enemyLayer != -1 && playerLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);

        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
