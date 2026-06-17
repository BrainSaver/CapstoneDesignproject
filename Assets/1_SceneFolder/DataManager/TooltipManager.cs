using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    public TMP_FontAsset fontAsset;

    private GameObject tooltipObject;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI descText;
    private RectTransform backgroundRect;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 폰트가 할당되지 않았다면 TopBarUIManager에서 다시 가져오기 시도
        if (fontAsset == null && TopBarUIManager.Instance != null)
        {
            fontAsset = TopBarUIManager.Instance.tooltipFont;
        }
        
        CreateTooltipUI();
    }

    private void CreateTooltipUI()
    {
        if (tooltipObject != null) return;

        // 1. 캔버스 생성
        GameObject canvasObj = new GameObject("TooltipCanvas");
        DontDestroyOnLoad(canvasObj);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. 툴팁 패널
        tooltipObject = new GameObject("TooltipPanel");
        tooltipObject.transform.SetParent(canvasObj.transform, false);
        backgroundRect = tooltipObject.AddComponent<RectTransform>();
        canvasGroup = tooltipObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Image bgImage = tooltipObject.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        bgImage.type = Image.Type.Sliced;

        // 레이아웃
        VerticalLayoutGroup layout = tooltipObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        ContentSizeFitter fitter = tooltipObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        backgroundRect.pivot = new Vector2(0, 1);
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.zero;

        // 3. 제목
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(tooltipObject.transform, false);
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.4f, 1f);
        if (fontAsset != null) titleText.font = fontAsset;

        // 4. 설명
        GameObject descObj = new GameObject("DescText");
        descObj.transform.SetParent(tooltipObject.transform, false);
        descText = descObj.AddComponent<TextMeshProUGUI>();
        descText.fontSize = 20;
        descText.color = Color.white;
        descText.lineSpacing = 15;
        if (fontAsset != null) descText.font = fontAsset;
        
        tooltipObject.SetActive(false);
    }

    private void Update()
    {
        if (tooltipObject != null && tooltipObject.activeSelf)
        {
            Vector2 mousePos = Vector2.zero;
            if (Mouse.current != null)
            {
                mousePos = Mouse.current.position.ReadValue();
            }
            
            float screenW = Screen.width;
            float screenH = Screen.height;

            // 화면 끝 검사하여 피벗 조정
            float pX = (mousePos.x > screenW * 0.75f) ? 1.1f : -0.1f;
            float pY = (mousePos.y < screenH * 0.25f) ? -0.1f : 1.1f;
            
            backgroundRect.pivot = new Vector2(pX > 0.5f ? 1 : 0, pY > 0.5f ? 1 : 0);
            
            // 위치 업데이트 (오프셋 추가)
            float offsetX = (pX > 0.5f) ? -20 : 20;
            float offsetY = (pY > 0.5f) ? -20 : 20;
            backgroundRect.position = mousePos + new Vector2(offsetX, offsetY);
        }
    }

    public void ShowTooltip(string title, string desc)
    {
        if (tooltipObject == null) CreateTooltipUI();

        if (titleText != null) 
        {
            titleText.text = title;
            if (fontAsset != null && titleText.font != fontAsset) titleText.font = fontAsset;
        }
        if (descText != null) 
        {
            descText.text = desc;
            if (fontAsset != null && descText.font != fontAsset) descText.font = fontAsset;
        }
        
        tooltipObject.SetActive(true);
        // 레이아웃 즉시 갱신 (멈춤 방지를 위해 한 번만)
        Canvas.ForceUpdateCanvases();
    }

    public void HideTooltip()
    {
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }
}