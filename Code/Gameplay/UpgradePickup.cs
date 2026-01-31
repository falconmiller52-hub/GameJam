using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Улучшение, которое игрок может подобрать.
/// Вылетает из Монстра после зачистки волны.
/// </summary>
public class UpgradePickup : MonoBehaviour
{
    [Header("=== ТИП УЛУЧШЕНИЯ ===")]
    public UpgradeType upgradeType = UpgradeType.Speed;
    
    [Tooltip("Значение улучшения (напр. 0.2 = +20% к скорости)")]
    public float upgradeValue = 0.2f;

    [Header("=== ОПИСАНИЕ ===")]
    [Tooltip("Название улучшения")]
    public string upgradeName = "Скорость";
    
    [Tooltip("Описание улучшения")]
    [TextArea(2, 4)]
    public string upgradeDescription = "+20% к скорости передвижения";

    [Header("=== UI ПОДСКАЗКИ ===")]
    [Tooltip("Канвас с подсказкой (создастся автоматически если не указан)")]
    public Canvas tooltipCanvas;
    
    [Tooltip("Текст названия")]
    public TextMeshProUGUI nameText;
    
    [Tooltip("Текст описания")]
    public TextMeshProUGUI descriptionText;
    
    [Tooltip("Текст кнопки")]
    public TextMeshProUGUI buttonHintText;
    
    [Tooltip("Изображение иконки")]
    public UnityEngine.UI.Image iconImage;

    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Спрайт иконки улучшения")]
    public Sprite upgradeIcon;
    
    [Tooltip("Цвет свечения")]
    public Color glowColor = Color.yellow;
    
    [Tooltip("Скорость покачивания")]
    public float bobSpeed = 2f;
    
    [Tooltip("Амплитуда покачивания")]
    public float bobAmount = 0.2f;

    [Header("=== РАДИУС ПОДБОРА ===")]
    public float pickupRadius = 1.5f;
    public float tooltipShowRadius = 2f;

    [Header("=== АУДИО ===")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float pickupVolume = 0.8f;

    [Header("=== АНИМАЦИЯ ВЫЛЕТА ===")]
    public float flyOutDuration = 0.8f;
    public float flyOutHeight = 2f;

    // Приватные переменные
    private Transform player;
    private bool isPlayerNearby = false;
    private bool isPickedUp = false;
    private Vector3 startPosition;
    private float bobOffset;
    private SpriteRenderer spriteRenderer;
    private WaveSpawner waveSpawner;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f); // Случайная фаза

        // Находим игрока
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Находим WaveSpawner
        waveSpawner = FindObjectOfType<WaveSpawner>();

        // Создаём UI подсказки если нет
        if (tooltipCanvas == null)
            CreateTooltipUI();
        else
            tooltipCanvas.gameObject.SetActive(false);

        // Устанавливаем иконку
        if (iconImage != null && upgradeIcon != null)
            iconImage.sprite = upgradeIcon;
    }

    void Update()
    {
        if (isPickedUp) return;

        // Покачивание
        float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Проверяем расстояние до игрока
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            // Показываем подсказку
            if (distance <= tooltipShowRadius && !isPlayerNearby)
            {
                ShowTooltip();
                isPlayerNearby = true;
            }
            else if (distance > tooltipShowRadius && isPlayerNearby)
            {
                HideTooltip();
                isPlayerNearby = false;
            }

            // Подбор улучшения
            if (isPlayerNearby && distance <= pickupRadius)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    PickUp();
                }
            }
        }

        // Обновляем позицию подсказки
        if (isPlayerNearby && tooltipCanvas != null)
        {
            UpdateTooltipPosition();
        }
    }

    void ShowTooltip()
    {
        if (tooltipCanvas != null)
        {
            tooltipCanvas.gameObject.SetActive(true);
            UpdateTooltipContent();
        }
    }

    void HideTooltip()
    {
        if (tooltipCanvas != null)
        {
            tooltipCanvas.gameObject.SetActive(false);
        }
    }

    void UpdateTooltipContent()
    {
        if (nameText != null)
            nameText.text = upgradeName;
        
        if (descriptionText != null)
            descriptionText.text = upgradeDescription;
        
        if (buttonHintText != null)
            buttonHintText.text = "[E] Подобрать";
    }

    void UpdateTooltipPosition()
    {
        if (tooltipCanvas == null) return;

        // Позиционируем над улучшением
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);
        tooltipCanvas.transform.position = screenPos;
    }

    void PickUp()
    {
        if (isPickedUp) return;
        isPickedUp = true;

        Debug.Log($"[UpgradePickup] Подобрано: {upgradeName}");

        // Применяем улучшение через UpgradeManager (для особых улучшений)
        // или через PlayerUpgrades (для обычных статов)
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.ApplyUpgrade(upgradeType, upgradeValue);
        }
        else if (PlayerUpgrades.Instance != null)
        {
            PlayerUpgrades.Instance.ApplyUpgrade(upgradeType, upgradeValue);
        }
        else
        {
            Debug.LogWarning("[UpgradePickup] Ни UpgradeManager, ни PlayerUpgrades не найдены!");
        }

        // Звук подбора
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        }

        // Уведомляем WaveSpawner
        if (waveSpawner != null)
        {
            waveSpawner.OnUpgradePickedUp();
        }

        // Уничтожаем все улучшения (включая второе)
        UpgradeSpawner spawner = FindObjectOfType<UpgradeSpawner>();
        if (spawner != null)
        {
            spawner.DestroyAllUpgrades();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Анимация вылета из точки (вызывается UpgradeSpawner)
    /// </summary>
    public void FlyOut(Vector3 fromPosition, Vector3 toPosition)
    {
        StartCoroutine(FlyOutRoutine(fromPosition, toPosition));
    }

    IEnumerator FlyOutRoutine(Vector3 from, Vector3 to)
    {
        transform.position = from;
        
        float elapsed = 0f;
        
        while (elapsed < flyOutDuration)
        {
            float t = elapsed / flyOutDuration;
            
            // Плавная интерполяция с дугой
            Vector3 currentPos = Vector3.Lerp(from, to, t);
            float heightOffset = Mathf.Sin(t * Mathf.PI) * flyOutHeight;
            currentPos.y += heightOffset;
            
            transform.position = currentPos;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
        startPosition = to; // Обновляем для покачивания
    }

    void CreateTooltipUI()
    {
        // Создаём простой Canvas для подсказки
        GameObject canvasObj = new GameObject("UpgradeTooltip");
        canvasObj.transform.SetParent(transform);
        
        tooltipCanvas = canvasObj.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 100;
        
        // Создаём панель
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform);
        
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(250, 100);
        
        UnityEngine.UI.Image panelBg = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelBg.color = new Color(0, 0, 0, 0.8f);

        // Название
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(panelObj.transform);
        
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.6f);
        nameRect.anchorMax = new Vector2(1, 1f);
        nameRect.offsetMin = Vector2.zero;
        nameRect.offsetMax = Vector2.zero;
        
        nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = upgradeName;
        nameText.fontSize = 18;
        nameText.fontStyle = FontStyles.Bold;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = glowColor;

        // Описание
        GameObject descObj = new GameObject("DescText");
        descObj.transform.SetParent(panelObj.transform);
        
        RectTransform descRect = descObj.AddComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0, 0.2f);
        descRect.anchorMax = new Vector2(1, 0.6f);
        descRect.offsetMin = Vector2.zero;
        descRect.offsetMax = Vector2.zero;
        
        descriptionText = descObj.AddComponent<TextMeshProUGUI>();
        descriptionText.text = upgradeDescription;
        descriptionText.fontSize = 14;
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.color = Color.white;

        // Подсказка кнопки
        GameObject hintObj = new GameObject("HintText");
        hintObj.transform.SetParent(panelObj.transform);
        
        RectTransform hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 0);
        hintRect.anchorMax = new Vector2(1, 0.2f);
        hintRect.offsetMin = Vector2.zero;
        hintRect.offsetMax = Vector2.zero;
        
        buttonHintText = hintObj.AddComponent<TextMeshProUGUI>();
        buttonHintText.text = "[E] Подобрать";
        buttonHintText.fontSize = 12;
        buttonHintText.alignment = TextAlignmentOptions.Center;
        buttonHintText.color = Color.gray;

        tooltipCanvas.gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        // Радиус подбора
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);

        // Радиус показа подсказки
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tooltipShowRadius);
    }
}
