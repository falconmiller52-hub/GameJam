using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PlayerTutorial : MonoBehaviour
{
    [Header("UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tutorialText;
    public Canvas tutorialCanvas;

    [Header("Animation")]
    public float typingSpeed = 0.08f;
    public string tutorialMessage = "Нажмите Пробел, чтобы сделать рывок";
    
    [Header("Position")]
    public Vector3 offsetFromPlayer = new Vector3(0, 2f, 0);

    [Header("Audio (опционально)")]
    public AudioClip typeSound;

    private CanvasScaler canvasScaler;
    private bool hasDashed = false;
    private AudioSource audioSource;
    private Coroutine typingCoroutine;

    void Start()
    {
        // Автонастройка UI
        if (tooltipPanel == null)
        {
            CreateTutorialUI();
        }
        
        canvasScaler = tutorialCanvas?.GetComponent<CanvasScaler>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        tooltipPanel.SetActive(false);
        StartCoroutine(ShowTutorialAfterDelay(2f));
    }

    void Update()
    {
        if (!tooltipPanel.activeInHierarchy || tutorialCanvas == null) return;

        // ✅ ИСПРАВЛЕННОЕ позиционирование
        Vector3 worldPos = transform.position + offsetFromPlayer;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        RectTransform rect = tooltipPanel.GetComponent<RectTransform>();
        Vector2 canvasLocalPos;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tutorialCanvas.GetComponent<RectTransform>(), 
            screenPos, 
            null,
            out canvasLocalPos
        );

        if (success)
        {
            rect.anchoredPosition = canvasLocalPos + new Vector2(20, -20);
        }
    }

    IEnumerator ShowTutorialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartTutorialTyping();
    }

    void StartTutorialTyping()
    {
        if (hasDashed) {
            Debug.Log("Рывок уже сделан, туториал пропускаем.");
            return;
        }
        
        Debug.Log("Запускаем туториал рывка.");
        tooltipPanel.SetActive(true);
        typingCoroutine = StartCoroutine(TypeTutorialText());
    }

    IEnumerator TypeTutorialText()
    {
        tutorialText.text = "";
        
        foreach (char letter in tutorialMessage)
        {
            if (hasDashed) yield break;  // ← Быстрый выход при даше
            
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

    // ✅ Вызывается из PlayerMovement!
public void OnFirstDash()
{
    Debug.Log("OnFirstDash вызван!");
    hasDashed = true;
    
    if (tooltipPanel != null)
    {
        tooltipPanel.SetActive(false);
        Debug.Log("Tooltip отключен!");
    }
    
    if (typingCoroutine != null)
    {
        StopCoroutine(typingCoroutine);
        typingCoroutine = null;
    }
    StopAllCoroutines();
    
    // 🔥 УДАЛИ ЭТУ СТРОКУ!
    // tutorialCanvas.enabled = false;
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
        
        // ✅ Авто-размер
        ContentSizeFitter sizeFitter = panelGO.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}
