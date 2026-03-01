using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class UpgradePickup : MonoBehaviour
{
    [Header("=== UPGRADE TYPE ===")]
    public UpgradeType upgradeType = UpgradeType.Speed;
    public float upgradeValue = 0.2f;

    [Header("=== DESCRIPTION ===")]
    public string upgradeName = "Speed";
    [TextArea(2, 4)] public string upgradeDescription = "+20% speed";

    [Header("=== VISUAL ===")]
    public Sprite upgradeIcon;
    public Color glowColor = Color.yellow;
    public float bobSpeed = 2f;
    public float bobAmount = 0.2f;

    [Header("=== FONT ===")]
    public TMP_FontAsset tooltipFont;
    public float nameFontSize = 4f;
    public float descFontSize = 3f;
    public float hintFontSize = 2.5f;

    [Header("=== RADIUS ===")]
    public float pickupRadius = 2.5f;
    public float tooltipShowRadius = 4f;

    [Header("=== AUDIO ===")]
    public AudioClip pickupSound;
    [Range(0f, 1f)] public float pickupVolume = 0.8f;

    [Header("=== FLY OUT ===")]
    public float flyOutDuration = 0.8f;
    public float flyOutHeight = 2f;

    private Transform player;
    private bool isPlayerNearby, isPickedUp;
    private Vector3 startPosition;
    private float bobOffset;
    private SpriteRenderer spriteRenderer;
    private WaveSpawner waveSpawner;
    private GameObject tooltipRoot;
    private TextMeshPro worldNameText, worldDescText, worldHintText;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        if (spriteRenderer != null && spriteRenderer.sortingOrder < 5) spriteRenderer.sortingOrder = 5;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        waveSpawner = FindObjectOfType<WaveSpawner>();

        if (tooltipFont == null)
        { TextMeshProUGUI existing = FindObjectOfType<TextMeshProUGUI>(); if (existing != null && existing.font != null) tooltipFont = existing.font; }

        CreateWorldTooltip();
    }

    void Update()
    {
        if (isPickedUp) return;
        float newY = startPosition.y + Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobAmount;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        if (tooltipRoot != null) tooltipRoot.transform.position = transform.position + Vector3.down * 0.8f;
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= tooltipShowRadius && !isPlayerNearby) { isPlayerNearby = true; if (tooltipRoot != null) tooltipRoot.SetActive(true); }
        else if (dist > tooltipShowRadius && isPlayerNearby) { isPlayerNearby = false; if (tooltipRoot != null) tooltipRoot.SetActive(false); }

        if (isPlayerNearby && dist <= pickupRadius && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            PickUp();
    }

    void PickUp()
    {
        if (isPickedUp) return;
        isPickedUp = true;
        
        // 📊 АНАЛИТИКА: игрок выбрал апгрейд
        if (GameAnalyticsManager.Instance != null)
            GameAnalyticsManager.Instance.TrackUpgradePicked(upgradeType.ToString(), upgradeName);
        
        if (UpgradeManager.Instance != null) UpgradeManager.Instance.ApplyUpgrade(upgradeType, upgradeValue);
        if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);
        if (waveSpawner != null) waveSpawner.OnUpgradePickedUp();
        UpgradeSpawner spawner = FindObjectOfType<UpgradeSpawner>();
        if (spawner != null) spawner.DestroyAllUpgrades(); else CleanupAndDestroy();
    }

    public void CleanupAndDestroy() { if (tooltipRoot != null) { Destroy(tooltipRoot); tooltipRoot = null; } Destroy(gameObject); }

    public void FlyOut(Vector3 from, Vector3 to) { StartCoroutine(FlyOutRoutine(from, to)); }

    IEnumerator FlyOutRoutine(Vector3 from, Vector3 to)
    {
        transform.position = from;
        if (spriteRenderer != null) { spriteRenderer.enabled = true; Color c = spriteRenderer.color; c.a = 1f; spriteRenderer.color = c; }
        float elapsed = 0f;
        while (elapsed < flyOutDuration)
        { float t = elapsed / flyOutDuration; Vector3 pos = Vector3.Lerp(from, to, t); pos.y += Mathf.Sin(t * Mathf.PI) * flyOutHeight; transform.position = pos; elapsed += Time.deltaTime; yield return null; }
        transform.position = to; startPosition = to;
    }

    void CreateWorldTooltip()
    {
        tooltipRoot = new GameObject($"Tooltip_{upgradeName}");
        tooltipRoot.transform.position = transform.position + Vector3.down * 0.8f;
        worldNameText = CreateTMP("Name", Vector3.zero, nameFontSize, FontStyles.Bold, glowColor);
        worldDescText = CreateTMP("Desc", Vector3.down * 0.45f, descFontSize, FontStyles.Normal, Color.white);
        worldHintText = CreateTMP("Hint", Vector3.down * 0.85f, hintFontSize, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
        worldNameText.text = upgradeName;
        worldDescText.text = upgradeDescription;
        worldHintText.text = "[E] Pick up";
        tooltipRoot.SetActive(false);
    }

    TextMeshPro CreateTMP(string name, Vector3 lp, float fs, FontStyles style, Color col)
    {
        GameObject o = new GameObject(name); o.transform.SetParent(tooltipRoot.transform); o.transform.localPosition = lp;
        TextMeshPro t = o.AddComponent<TextMeshPro>(); t.fontSize = fs; t.fontStyle = style; t.alignment = TextAlignmentOptions.Center; t.color = col; t.sortingOrder = 100;
        if (tooltipFont != null) t.font = tooltipFont;
        MeshRenderer mr = o.GetComponent<MeshRenderer>(); if (mr != null) mr.sortingOrder = 100;
        return t;
    }

    void OnDestroy() { if (tooltipRoot != null) Destroy(tooltipRoot); }

    void OnDrawGizmosSelected()
    { Gizmos.color = Color.green; Gizmos.DrawWireSphere(transform.position, pickupRadius); Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, tooltipShowRadius); }
}
