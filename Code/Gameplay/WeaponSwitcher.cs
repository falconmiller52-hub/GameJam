using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;

/// <summary>
/// Система смены оружия.
/// Позволяет переключаться между катаной и кулаками.
/// </summary>
public class WeaponSwitcher : MonoBehaviour
{
    [Header("=== ОРУЖИЯ ===")]
    [Tooltip("Объект катаны (WeaponPivot)")]
    public GameObject katanaWeapon;
    
    [Tooltip("Объект кулаков")]
    public GameObject fistsWeapon;

    [Header("=== ТЕКУЩЕЕ ОРУЖИЕ ===")]
    public WeaponType currentWeapon = WeaponType.Katana;
    
    [Tooltip("Разблокированы ли кулаки?")]
    public bool fistsUnlocked = false;

    [Header("=== UI ПОДСКАЗКА ===")]
    [Tooltip("Текст подсказки")]
    public TextMeshProUGUI switchHintText;
    
    [Tooltip("CanvasGroup подсказки")]
    public CanvasGroup hintCanvasGroup;
    
    [Tooltip("Время показа подсказки после разблокировки")]
    public float hintDisplayTime = 5f;

    [Header("=== АУДИО ===")]
    public AudioClip switchSound;
    [Range(0f, 1f)]
    public float switchVolume = 0.6f;

    [Header("=== АНИМАЦИЯ СМЕНЫ ===")]
    public float switchDuration = 0.2f;

    // Приватные переменные
    private AudioSource audioSource;
    private bool isSwitching = false;
    private Coroutine hintCoroutine;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Начинаем с катаной
        SetWeapon(WeaponType.Katana, instant: true);

        // Скрываем подсказку
        if (hintCanvasGroup != null)
        {
            hintCanvasGroup.alpha = 0f;
            hintCanvasGroup.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Смена оружия на Q (только если кулаки разблокированы)
        if (fistsUnlocked && !isSwitching)
        {
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                SwitchWeapon();
            }
        }
    }

    /// <summary>
    /// Переключает на следующее оружие
    /// </summary>
    public void SwitchWeapon()
    {
        if (isSwitching) return;

        WeaponType newWeapon = currentWeapon == WeaponType.Katana ? WeaponType.Fists : WeaponType.Katana;
        StartCoroutine(SwitchRoutine(newWeapon));
    }

    IEnumerator SwitchRoutine(WeaponType newWeapon)
    {
        isSwitching = true;

        // Звук смены
        if (switchSound != null)
            audioSource.PlayOneShot(switchSound, switchVolume);

        // Скрываем текущее оружие
        GameObject currentWeaponObj = currentWeapon == WeaponType.Katana ? katanaWeapon : fistsWeapon;
        if (currentWeaponObj != null)
        {
            yield return StartCoroutine(FadeWeapon(currentWeaponObj, false));
        }

        // Меняем оружие
        currentWeapon = newWeapon;

        // Показываем новое оружие
        GameObject newWeaponObj = newWeapon == WeaponType.Katana ? katanaWeapon : fistsWeapon;
        if (newWeaponObj != null)
        {
            newWeaponObj.SetActive(true);
            yield return StartCoroutine(FadeWeapon(newWeaponObj, true));
        }

        // Обновляем активность оружий
        if (katanaWeapon != null)
            katanaWeapon.SetActive(currentWeapon == WeaponType.Katana);
        if (fistsWeapon != null)
            fistsWeapon.SetActive(currentWeapon == WeaponType.Fists);

        isSwitching = false;

        Debug.Log($"[WeaponSwitcher] Сменили оружие на: {currentWeapon}");
    }

    IEnumerator FadeWeapon(GameObject weaponObj, bool fadeIn)
    {
        SpriteRenderer[] sprites = weaponObj.GetComponentsInChildren<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < switchDuration)
        {
            float t = elapsed / switchDuration;
            float alpha = fadeIn ? t : (1f - t);

            foreach (SpriteRenderer sr in sprites)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Финальное значение
        foreach (SpriteRenderer sr in sprites)
        {
            Color c = sr.color;
            c.a = fadeIn ? 1f : 0f;
            sr.color = c;
        }

        if (!fadeIn)
            weaponObj.SetActive(false);
    }

    /// <summary>
    /// Устанавливает оружие напрямую
    /// </summary>
    public void SetWeapon(WeaponType type, bool instant = false)
    {
        currentWeapon = type;

        if (katanaWeapon != null)
            katanaWeapon.SetActive(type == WeaponType.Katana);
        if (fistsWeapon != null)
            fistsWeapon.SetActive(type == WeaponType.Fists);

        if (!instant)
        {
            if (switchSound != null)
                audioSource.PlayOneShot(switchSound, switchVolume);
        }
    }

    /// <summary>
    /// Разблокирует кулаки
    /// </summary>
    public void UnlockFists()
    {
        fistsUnlocked = true;
        Debug.Log("[WeaponSwitcher] Кулаки разблокированы!");

        // Показываем подсказку
        ShowSwitchHint();
    }

    void ShowSwitchHint()
    {
        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(ShowHintRoutine());
    }

    IEnumerator ShowHintRoutine()
    {
        if (switchHintText != null)
            switchHintText.text = "Нажмите Q чтобы сменить оружие";

        if (hintCanvasGroup != null)
        {
            hintCanvasGroup.gameObject.SetActive(true);

            // Плавное появление
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                hintCanvasGroup.alpha = elapsed / 0.3f;
                elapsed += Time.deltaTime;
                yield return null;
            }
            hintCanvasGroup.alpha = 1f;

            // Ждём
            yield return new WaitForSeconds(hintDisplayTime);

            // Плавное исчезновение
            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                hintCanvasGroup.alpha = 1f - (elapsed / 0.3f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            hintCanvasGroup.alpha = 0f;
            hintCanvasGroup.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Проверяет, какое оружие сейчас активно
    /// </summary>
    public bool IsKatanaActive() => currentWeapon == WeaponType.Katana;
    public bool IsFistsActive() => currentWeapon == WeaponType.Fists;
}

public enum WeaponType
{
    Katana,
    Fists
}
