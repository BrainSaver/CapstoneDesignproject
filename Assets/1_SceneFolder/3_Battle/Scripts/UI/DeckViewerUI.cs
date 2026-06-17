using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 덱 보기 버튼과 연결된 UI 컨트롤러.
/// </summary>
public class DeckViewerUI : MonoBehaviour
{
    [Header("창 (Window)")]
    [SerializeField] private GameObject DeckViewer;
    [SerializeField] private GameObject playerUI;

    [Header("스크롤 & 그리드")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private CardDisplay cardUIPrefab;

    [Header("타이틀 텍스트 (선택)")]
    [SerializeField] private TMPro.TextMeshProUGUI titleText;

    private readonly List<CardDisplay> pool = new();
    private bool isOpen;

    public void OpenDeck() => OpenViewer("Deck");
    public void OpenDrawPile() => OpenViewer("DrawPile");
    public void OpenDiscardPile() => OpenViewer("DiscardPile");
    public void OpenExhaustPile() => OpenViewer("ExhaustPile");

    public void OpenViewer(string mode = "Deck")
    {
        isOpen = true;
        if (DeckViewer) DeckViewer.SetActive(true);
        if (playerUI) playerUI.SetActive(false);

        if (titleText != null) titleText.text = GetTitle(mode);

        // 열 때마다 풀 초기화 후 새로 채우기
        ClearPool();
        RefreshCardList(GetCardList(mode));

        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void CloseViewer()
    {
        isOpen = false;
        if (DeckViewer) DeckViewer.SetActive(false);
        if (playerUI) playerUI.SetActive(true);

        // 닫을 때 풀 초기화
        ClearPool();
    }

    /// <summary>풀의 모든 카드를 파괴하고 초기화한다.</summary>
    private void ClearPool()
    {
        foreach (var item in pool)
            if (item != null) Destroy(item.gameObject);
        pool.Clear();
    }

    private List<Card> GetCardList(string mode)
    {
        var result = mode switch
        {
            "Deck" => PlayerDeck.Instance?.GetDeck() ?? new List<Card>(),
            "DrawPile" => DeckManager.Instance?.GetDrawCards() ?? new List<Card>(),
            "DiscardPile" => DeckManager.Instance?.GetDiscardCards() ?? new List<Card>(),
            "ExhaustPile" => ExhaustPile.GetDeck(),
            _ => PlayerDeck.Instance?.GetDeck() ?? new List<Card>()
        };
        return result;
    }

    private string GetTitle(string mode) => mode switch
    {
        "Deck" => "전체 덱",
        "DrawPile" => "드로우 더미",
        "DiscardPile" => "버린 카드",
        "ExhaustPile" => "소멸 묘지",
        _ => "덱"
    };

    private void RefreshCardList(List<Card> deck)
    {

        if (contentRoot == null || cardUIPrefab == null)
        {
            Logger.LogError("[DeckViewerUI] contentRoot 또는 cardUIPrefab이 없습니다.");
            return;
        }

        Vector2 cardSize = cardUIPrefab.GetComponent<RectTransform>().sizeDelta;
        foreach (var rt2 in cardUIPrefab.GetComponentsInChildren<RectTransform>(true))
        {
            if (rt2.gameObject.name == "Card Background")
            {
                cardSize = rt2.sizeDelta;
                break;
            }
        }

        foreach (var card in deck)
        {
            var item = Instantiate(cardUIPrefab, contentRoot);
            item.transform.localScale = Vector3.one;

            var rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = cardSize;
            }

            var mv = item.GetComponent<CardMovement>();
            if (mv) mv.enabled = false;
            var img = item.GetComponent<Image>();
            if (img) img.raycastTarget = false;

            item.SetData(card);
            pool.Add(item);
        }
    }

    private void Update()
    {
        if (isOpen && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            CloseViewer();
    }
}