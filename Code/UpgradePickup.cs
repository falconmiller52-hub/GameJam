using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Улучшение для подбора. Мировой tooltip с кастомным шрифтом.
/// 
/// ИСПРАВЛЕНО:
/// - Поддержка кастомного TMP шрифта (tooltipFont)
/// - SpriteRenderer гарантированно видим после FlyOut (sortingOrder)
/// - Полная очистка при уничтожении
/// </summary>
public class UpgradePickup : MonoBehaviour
{
    [Header("=== ТИП УЛУЧШЕНИЯ ===")]
    public UpgradeType upgradeType = UpgradeType.Speed;
    public float upgradeValue = 0.2f;

    [Header("=== ОПИСАНИЕ ===")]
    public string upgradeName = "Скорость";
    [TextArea(2, 4)]
    public string upgradeDescription = "+20% к скорости";

    [Header("=== ВИЗУАЛ ===")]
    public Sprite upgradeIcon;
    public Color glowColor = Color.yellow;
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;

    [Header("=== ШРИФТ ===")]
    [Tooltip("Кастомный TMP шрифт для tooltip. Если не назначен — используется стандартный.")]
    public TMP_FontAsset tooltipFont;
    [Tooltip("Размер названия")]
    public float nameFontSize = 4f;
    [Tooltip("Размер описания")]
    public float descFontSize = 3f;
    [Tooltip("Размер подсказки [E]")]
    public float hintFontSize = 2.5f;

    [Header("=== РАДИУСЫ ===")]
    public float pickupRadius = 2.5f;
    public float tooltipShowRadius = 4f;

    [Header("=== АУДИО ===")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.8f;

    [Header("=== ВЫЛЕТ ===")]
    public float flyOutDuration = 0.8f;
    public float flyOutHeight = 2f;

    // Приватные
    private Transform player;
    private bool isPlayerNearby = false;
    private bool isPickedUp = false;
    private Vector3 startPosition;
    private float bobOffset;
    private SpriteRenderer spriteRenderer;
    private WaveSpawner waveSpawner;

    private GameObject tooltipRoot;
    private TextMeshPro worldNameText;
    private TextMeshPro worldDescText;
    private TextMeshPro worldHintText;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        // Гарантируем видимость спрайта
        if (spriteRenderer != null && spriteRenderer.sortingOrder < 5)
            spriteRenderer.sortingOrder = 5;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        waveSpawner = FindObjectOfType<WaveSpawner>();

        // 🔥 Ищем кастомный шрифт если не назначен
        if (tooltipFont == null)
        {
            // Попробуем найти через уже существующие TMP объекты на сцене
            TextMeshProUGUI existingTMP = FindObjectOfType<TextMeshProUGUI>();
            if (existingTMP != null && existingTMP.font != null)
                tooltipFont = existingTMP.font;
        }

        CreateWorldTooltip();
    }

    void Update()
    {
        if (isPickedUp) return;

        float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        if (tooltipRoot != null)
            tooltipRoot.transform.position = transform.position + Vector3.down * 0.8f;

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= tooltipShowRadius && !isPlayerNearby)
        {
            isPlayerNearby = true;
            if (tooltipRoot != null) tooltipRoot.SetActive(true);
        }
        else if (distance > tooltipShowRadius && isPlayerNearby)
        {
            isPlayerNearby = false;
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }

        if (isPlayerNearby && distance <= pickupRadius)
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                PickUp();
        }
    }

    void PickUp()
    {
        if (isPickedUp) return;
        isPickedUp = true;

        Debug.Log($"[UpgradePickup] Подобрано: {upgradeName} ({upgradeType})");

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ApplyUpgrade(upgradeType, upgradeValue);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        if (waveSpawner != null)
            waveSpawner.OnUpgradePickedUp();

        UpgradeSpawner spawner = FindObjectOfType<UpgradeSpawner>();
        if (spawner != null)
            spawner.DestroyAllUpgrades();
        else
            CleanupAndDestroy();
    }

    public void CleanupAndDestroy()
    {
        if (tooltipRoot != null)
        {
            Destroy(tooltipRoot);
            tooltipRoot = null;
        }
        Destroy(gameObject);
    }

    public void FlyOut(Vector3 from, Vector3 to)
    {
        StartCoroutine(FlyOutRoutine(from, to));
    }

    IEnumerator FlyOutRoutine(Vector3 from, Vector3 to)
    {
        transform.position = from;

        // Гарантируем видимость во время полёта
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        float elapsed = 0f;
        while (elapsed < flyOutDuration)
        {
            float t = elapsed / flyOutDuration;
            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * flyOutHeight;
            transform.position = pos;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = to;
        startPosition = to;
    }

    void CreateWorldTooltip()
    {
        tooltipRoot = new GameObject($"Tooltip_{upgradeName}");
        tooltipRoot.transform.position = transform.position + Vector3.down * 0.8f;

        worldNameText = CreateTMPChild("Name", Vector3.zero, nameFontSize, FontStyles.Bold, glowColor);
        worldDescText = CreateTMPChild("Desc", Vector3.down * 0.45f, descFontSize, FontStyles.Normal, Color.white);
        worldHintText = CreateTMPChild("Hint", Vector3.down * 0.85f, hintFontSize, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));

        worldNameText.text = upgradeName;
        worldDescText.text = upgradeDescription;
        worldHintText.text = "[E] Подобрать";

        tooltipRoot.SetActive(false);
    }

    TextMeshPro CreateTMPChild(string name, Vector3 localPos, float fontSize, FontStyles style, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(tooltipRoot.transform);
        obj.transform.localPosition = localPos;

        TextMeshPro tmp = obj.AddComponent<TextMeshPro>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.sortingOrder = 100;

        // 🔥 Кастомный шрифт
        if (tooltipFont != null)
            tmp.font = tooltipFont;

        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 100;

        return tmp;
    }

    void OnDestroy()
    {
        if (tooltipRoot != null)
            Destroy(tooltipRoot);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, tooltipShowRadius);
    }
}
