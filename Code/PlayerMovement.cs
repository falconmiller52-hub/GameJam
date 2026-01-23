using UnityEngine;
using UnityEngine.InputSystem; // Важно! Добавьте эту строку

public class PlayerMovement : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // ВАРИАНТ ДЛЯ НОВОЙ СИСТЕМЫ (без Action Asset)
        // Считываем клавиатуру напрямую
        var keyboard = Keyboard.current;
        if (keyboard == null) return; // Если клавиатуры нет, выходим

        float moveX = 0f;
        float moveY = 0f;

        // Эквивалент GetAxisRaw - проверяем нажатие клавиш вручную
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveY += 1;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveY -= 1;

        movement = new Vector2(moveX, moveY);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement.normalized * moveSpeed;
    }
}
