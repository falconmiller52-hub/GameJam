using UnityEngine;
using TMPro;

/// <summary>
/// Всплывающие числа урона над врагами.
/// 
/// КАК ИСПОЛЬЗОВАТЬ:
/// 1. Создай префаб: пустой GameObject с этим скриптом + TextMeshPro компонент
/// 2. Назначь префаб в DamagePopup.prefab (статическая ссылка) — или —
/// 3. Просто вызови DamagePopup.Create(position, damage) — он создаст текст автоматически
///
/// НАСТРОЙКА ПРЕФАБА (если хочешь свой):
/// - Создай пустой GameObject
/// - Добавь TextMeshPro (НЕ UI, а обычный World Space)
/// - Добавь этот скрипт
/// - Сохрани как префаб в Prefabs/
/// - Назначь в любом объекте на сцене через DamagePopupConfig
///
/// Если префаб не назначен, скрипт создаст текст программно.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("=== ANIMATION ===")]
    [Tooltip("Скорость полёта вверх")]
    public float floatSpeed = 1.5f;
    
    [Tooltip("Время жизни попапа")]
    public float lifetime = 0.8f;
    
    [Tooltip("Случайный разброс по X при спавне")]
    public float randomOffsetX = 0.5f;

    [Header("=== VISUAL ===")]
    [Tooltip("Начальный масштаб (эффект 'выскакивания')")]
    public float startScale = 0.5f;
    
    [Tooltip("Максимальный масштаб в пике")]
    public float peakScale = 1.2f;
    
    [Tooltip("Конечный масштаб перед исчезновением")]
    public float endScale = 0.8f;

    [Header("=== COLORS ===")]
    public Color normalColor = Color.white;
    public Color criticalColor = Color.yellow;  // Для будущих критов
    public Color reducedColor = new Color(0.7f, 0.7f, 0.7f); // Сниженный урон

    // ===== СТАТИЧЕСКИЕ ПОЛЯ =====
    private static GameObject _prefab;
    private static TMP_FontAsset _cachedFont;

    // ===== ПРИВАТНЫЕ =====
    private TextMeshPro textMesh;
    private float elapsed;
    private Color baseColor;
    private Vector3 baseScale;

    /// <summary>
    /// (Опционально) Назначь префаб для использования вместо программного создания
    /// </summary>
    public static void SetPrefab(GameObject prefab)
    {
        _prefab = prefab;
    }

    /// <summary>
    /// Создаёт попап урона в указанной позиции.
    /// Вызывай из любого скрипта: DamagePopup.Create(enemy.position, damage);
    /// </summary>
    /// <param name="position">Мировая позиция (обычно transform.position врага)</param>
    /// <param name="amount">Количество урона</param>
    /// <param name="isReduced">Сниженный урон? (серый цвет)</param>
    public static DamagePopup Create(Vector3 position, int amount, bool isReduced = false)
    {
        GameObject obj;

        if (_prefab != null)
        {
            obj = Instantiate(_prefab, position, Quaternion.identity);
        }
        else
        {
            obj = CreateDefaultPopup(position);
        }

        DamagePopup popup = obj.GetComponent<DamagePopup>();
        if (popup == null)
            popup = obj.AddComponent<DamagePopup>();

        popup.Setup(amount, isReduced);
        return popup;
    }

    /// <summary>
    /// Программное создание попапа (если нет префаба)
    /// </summary>
    static GameObject CreateDefaultPopup(Vector3 position)
    {
        GameObject obj = new GameObject("DamagePopup");
        obj.transform.position = position;

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 5f;
        tmp.sortingOrder = 200; // Поверх всего

        // Пробуем найти шрифт из существующих UI элементов
        if (_cachedFont == null)
        {
            TextMeshProUGUI existingUI = Object.FindObjectOfType<TextMeshProUGUI>();
            if (existingUI != null && existingUI.font != null)
                _cachedFont = existingUI.font;
        }
        if (_cachedFont != null)
            tmp.font = _cachedFont;

        // Устанавливаем sortingOrder через MeshRenderer
        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.sortingOrder = 200;

        DamagePopup popup = obj.AddComponent<DamagePopup>();
        return obj;
    }

    void Setup(int amount, bool isReduced)
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null) return;

        textMesh.text = amount.ToString();

        // Цвет в зависимости от типа урона
        if (isReduced)
            baseColor = reducedColor;
        else
            baseColor = normalColor;

        textMesh.color = baseColor;

        // Случайный сдвиг по X, чтобы цифры не накладывались
        float offsetX = Random.Range(-randomOffsetX, randomOffsetX);
        transform.position += new Vector3(offsetX, 0.5f, 0f);

        // Начальный масштаб
        transform.localScale = Vector3.one * startScale;
        baseScale = Vector3.one;

        elapsed = 0f;

        // Самоуничтожение
        Destroy(gameObject, lifetime + 0.1f);
    }

    void Update()
    {
        if (textMesh == null) return;

        elapsed += Time.deltaTime;
        float t = elapsed / lifetime; // 0 → 1

        // Движение вверх (с замедлением)
        float speedMod = 1f - (t * 0.5f); // Замедляется к концу
        transform.position += Vector3.up * floatSpeed * speedMod * Time.deltaTime;

        // Масштаб: быстро вырастает → плавно уменьшается
        float scale;
        if (t < 0.2f)
        {
            // Фаза роста (0 → 0.2): startScale → peakScale
            scale = Mathf.Lerp(startScale, peakScale, t / 0.2f);
        }
        else
        {
            // Фаза уменьшения (0.2 → 1): peakScale → endScale
            scale = Mathf.Lerp(peakScale, endScale, (t - 0.2f) / 0.8f);
        }
        transform.localScale = baseScale * scale;

        // Прозрачность: видим → исчезает в конце
        float alpha;
        if (t < 0.6f)
            alpha = 1f;
        else
            alpha = Mathf.Lerp(1f, 0f, (t - 0.6f) / 0.4f);

        Color c = baseColor;
        c.a = alpha;
        textMesh.color = c;
    }
}

/// <summary>
/// Вспомогательный компонент для назначения префаба DamagePopup через Inspector.
/// Повесь на GameManager или любой объект на сцене Level1.
/// </summary>
public class DamagePopupConfig : MonoBehaviour
{
    [Tooltip("Префаб попапа урона (опционально). Если не назначен — создастся автоматически.")]
    public GameObject damagePopupPrefab;

    void Awake()
    {
        if (damagePopupPrefab != null)
            DamagePopup.SetPrefab(damagePopupPrefab);
    }
}
