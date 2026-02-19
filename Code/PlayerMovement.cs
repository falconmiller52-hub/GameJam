using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
// 🔥 ДОБАВЛЕНО: Гарантируем, что AudioSource тоже будет на объекте
[RequireComponent(typeof(AudioSource))] 
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public bool isDashing = false;

    [Header("Audio")] // 🔥 ДОБАВЛЕНО
    public AudioClip dashSound; 
    public float dashVolume = 0.8f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SpriteRenderer sr;
    
    [Header("Tutorial")]
    public PlayerTutorial tutorial;

    private Vector2 moveInput;
    private Camera mainCam;
    private bool canDash = true;
    private bool hasDashed = false;
    private AudioSource audioSource; // 🔥 ДОБАВЛЕНО

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        mainCam = Camera.main;

        // 🔥 ДОБАВЛЕНО: Инициализация аудио
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

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
        if (isDashing) return;

        float x = 0f, y = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            
            if (!PauseMenu.isPaused && Keyboard.current.spaceKey.wasPressedThisFrame && canDash)
            {
                StartCoroutine(Dash());
            }
        }
        
        moveInput = new Vector2(x, y).normalized;

        if (animator != null)
        {
            bool isMoving = moveInput.sqrMagnitude > 0.01f;
            animator.SetFloat("Speed", isMoving ? 1f : 0f);
        }

        FlipSprite();
    }

    void FixedUpdate()
    {
        if (PauseMenu.isPaused || isDashing || Time.timeScale == 0f) return;

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

    public void NotifyTutorialDashComplete()
    {
        Debug.Log("Dash завершен! Уведомляем туториал.");
        hasDashed = true;
        
        if (tutorial != null)
        {
            tutorial.OnFirstDash();
            Debug.Log("Туториал уведомлен!");
        }
        else
        {
            Debug.LogWarning("PlayerTutorial не назначен в инспекторе!");
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        // 🔥 ДОБАВЛЕНО: Воспроизведение звука
        if (dashSound != null && audioSource != null)
        {
            // Немного меняем питч, чтобы звук не казался монотонным
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(dashSound, dashVolume);
        }

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

        NotifyTutorialDashComplete();

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}
