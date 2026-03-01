using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Tutorial system. Shows typed hints under the player.
/// FIXED: English strings. Added ShowCustomMessage() for weapon switch hint.
/// </summary>
public class PlayerTutorial : MonoBehaviour
{
    [Header("UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tutorialText;
    public Canvas tutorialCanvas;

    [Header("Animation")]
    public float typingSpeed = 0.08f;
    public string tutorialMessage = "Нажмите Пробел чтобы сделать рывок";

    [Header("Position")]
    public Vector3 offsetFromPlayer = new Vector3(0, 2f, 0);

    [Header("Audio")]
    public AudioClip typeSound;

    [Header("Auto-hide")]
    [Tooltip("Auto-hide custom messages after this many seconds (0 = don't auto-hide)")]
    public float customMessageDuration = 5f;

    private CanvasScaler canvasScaler;
    private bool hasDashed = false;
    private AudioSource audioSource;
    private Coroutine typingCoroutine;
    private Coroutine autoHideCoroutine;

    void Start()
    {
        if (tooltipPanel == null) CreateTutorialUI();
        canvasScaler = tutorialCanvas?.GetComponent<CanvasScaler>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        tooltipPanel.SetActive(false);
        StartCoroutine(ShowTutorialAfterDelay(2f));
    }

    void Update()
    {
        if (!tooltipPanel.activeInHierarchy || tutorialCanvas == null) return;
        if (Camera.main == null) return;

        Vector3 worldPos = transform.position + offsetFromPlayer;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransform rect = tooltipPanel.GetComponent<RectTransform>();
        Vector2 canvasLocalPos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tutorialCanvas.GetComponent<RectTransform>(), screenPos, null, out canvasLocalPos))
        {
            rect.anchoredPosition = canvasLocalPos + new Vector2(20, -20);
        }
    }

    IEnumerator ShowTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!hasDashed) StartTutorialTyping();
    }

    void StartTutorialTyping()
    {
        if (hasDashed) return;
        tooltipPanel.SetActive(true);
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(tutorialMessage));
    }

    IEnumerator TypeText(string message)
    {
        tutorialText.text = "";
        foreach (char letter in message)
        {
            tutorialText.text += letter;
            if (typeSound != null)
            {
                audioSource.pitch = Random.Range(0.9f, 1.1f);
                audioSource.PlayOneShot(typeSound);
            }
            yield return new WaitForSeconds(typingSpeed);
        }
        typingCoroutine = null;
    }

    /// <summary>
    /// Called from PlayerMovement after first dash.
    /// </summary>
    public void OnFirstDash()
    {
        hasDashed = true;
        HideTooltip();
    }

    /// <summary>
    /// Shows a custom typed message (e.g. "Press Q to switch weapon").
    /// Auto-hides after customMessageDuration seconds.
    /// </summary>
    public void ShowCustomMessage(string message)
    {
        if (tooltipPanel == null) return;

        // Stop any existing typing/auto-hide
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);

        tooltipPanel.SetActive(true);
        typingCoroutine = StartCoroutine(TypeText(message));

        if (customMessageDuration > 0)
            autoHideCoroutine = StartCoroutine(AutoHide(customMessageDuration));
    }

    IEnumerator AutoHide(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideTooltip();
    }

    void HideTooltip()
    {
        if (typingCoroutine != null) { StopCoroutine(typingCoroutine); typingCoroutine = null; }
        if (autoHideCoroutine != null) { StopCoroutine(autoHideCoroutine); autoHideCoroutine = null; }
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    void CreateTutorialUI()
    {
        GameObject canvasGO = new GameObject("TutorialCanvas");
        tutorialCanvas = canvasGO.AddComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.sortingOrder = 100;
        canvasScaler = canvasGO.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        GameObject panelGO = new GameObject("TooltipPanel");
        panelGO.transform.SetParent(canvasGO.transform);
        tooltipPanel = panelGO;

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(400, 80);

        tutorialText = panelGO.AddComponent<TextMeshProUGUI>();
        tutorialText.fontSize = 24;
        tutorialText.alignment = TextAlignmentOptions.Center;
        tutorialText.color = Color.white;

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        ContentSizeFitter sizeFitter = panelGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}
