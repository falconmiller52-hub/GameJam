using UnityEngine;
using System.Collections;

public class FloatingHP : MonoBehaviour
{
    [Header("HP Sprites")]
    public Sprite hpFull;
    public Sprite hpHalf; 
    public Sprite hpEmpty;

    [Header("Settings")]
    public int maxHP = 2; // У тебя на скрине 2, проверь что совпадает с PlayerHealth!
    public float showDuration = 0.4f; // Время показа после ЛЮБОГО изменения
    public float fadeSpeed = 5f;      // Скорость исчезновения

    private SpriteRenderer spriteRenderer;
    private PlayerHealth playerHealth;
    private int lastKnownHP;
    private Coroutine hideCoroutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerHealth = GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            maxHP = playerHealth.maxHealth; // Синхронизируем макс ХП
            lastKnownHP = playerHealth.currentHealth;
        }
        
        // Скрываем сразу (прозрачность 0)
        SetAlpha(0f);
    }

    void Update()
    {
        if (playerHealth == null) return;

        int currentHP = playerHealth.currentHealth;
        
        // 🔥 ЛЮБОЕ изменение здоровья триггерит показ
        if (currentHP != lastKnownHP)
        {
            UpdateSprite(currentHP);
            ShowIndicator();
            lastKnownHP = currentHP;
        }
    }

    void UpdateSprite(int hp)
    {
        float percent = (float)hp / maxHP;
        
        // Логика порогов (настрой под свои 2 сердечка)
        // Если 2 макс: 2/2 = 1.0 (Full), 1/2 = 0.5 (Half), 0/2 = 0 (Empty)
        
        if (percent >= 0.9f)       // Почти полный или полный
            spriteRenderer.sprite = hpFull;
        else if (percent > 0.1f)   // Где-то посередине
            spriteRenderer.sprite = hpHalf;
        else                       // Почти пустой или 0
            spriteRenderer.sprite = hpEmpty;
    }

    void ShowIndicator()
    {
        // Прерываем предыдущее скрытие, если оно шло
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        
        // Мгновенно показываем (Alpha = 1)
        SetAlpha(1f);
        
        // Запускаем новый таймер скрытия
        hideCoroutine = StartCoroutine(FadeOutAfterDelay());
    }

    IEnumerator FadeOutAfterDelay()
    {
        // 1. Ждем указанное время (0.4 сек), показывая спрайт полностью
        yield return new WaitForSeconds(showDuration);

        // 2. Плавно исчезаем
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);
    }

    void SetAlpha(float a)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = Mathf.Clamp01(a);
            spriteRenderer.color = c;
        }
    }
}
