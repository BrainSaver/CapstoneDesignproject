using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 씬 전환 시 화면 전체를 페이드 인/아웃하는 오버레이.
/// SceneFlowManager에서 LoadSceneWithFade() 호출 시 자동 생성된다.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("페이드 설정")]
    [SerializeField] private float fadeDuration = 0.5f; // 페이드 인/아웃 시간
    [SerializeField] private Color fadeColor = Color.black; // 페이드 색상

    private Image fadeImage;
    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupCanvas();
    }

    /// <summary>페이드용 Canvas와 Image를 동적으로 생성한다.</summary>
    private void SetupCanvas()
    {
        // Canvas 설정
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 최상단 렌더링

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // 페이드 Image 생성
        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(transform, false);

        fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.raycastTarget = false;

        // 화면 전체를 덮도록 RectTransform 설정
        var rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // CanvasGroup 추가
        canvasGroup = imgObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    /// <summary>화면을 서서히 어둡게 만든다 (씬 전환 전 호출).</summary>
    public IEnumerator FadeOut()
    {
        canvasGroup.blocksRaycasts = true;
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        yield return canvasGroup.DOFade(1f, fadeDuration)
                                .SetEase(Ease.InQuad)
                                .WaitForCompletion();
    }

    /// <summary>화면을 서서히 밝게 만든다 (씬 전환 후 호출).</summary>
    public IEnumerator FadeIn()
    {
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        canvasGroup.alpha = 1f;

        yield return canvasGroup.DOFade(0f, fadeDuration)
                                .SetEase(Ease.OutQuad)
                                .WaitForCompletion();

        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>씬 전환 후 새 씬의 최상단으로 올린다.</summary>
    public void BringToFront()
    {
        if (canvas != null)
            canvas.sortingOrder = 9999;
    }
}