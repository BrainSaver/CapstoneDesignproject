using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

/// <summary>
/// 드로우 더미와 버린 카드 더미를 관리하고 카드 드로우 애니메이션을 처리하는 매니저.
/// </summary>
public class DeckManager : SceneSingleton<DeckManager>
{
    [Header("덱 설정")]
    [Tooltip("드로우 더미 카드 목록.")]
    [SerializeField] private List<Card> drawPile = new List<Card>();

    [Tooltip("버린 카드 더미 목록.")]
    [SerializeField] private List<Card> discardPile = new List<Card>();

    [Tooltip("카드 UI 프리팹.")]
    [SerializeField] private GameObject cardPrefab;

    [Tooltip("드로우 더미 UI 앵커.")]
    public Transform drawPileAnchor;

    [Tooltip("버린 카드 더미 UI 위치 (HandManager 애니메이션 연동용)")]
    public Transform discardPileTransform;

    private bool isDrawingSafeLock = false;

    public List<Card> GetDrawCards() => new List<Card>(drawPile);
    public List<Card> GetDiscardCards() => new List<Card>(discardPile);

    public static event Action OnDrawPileChanged;
    public static event Action OnDiscardPileChanged;
    public static event System.Action<Card> OnDiscardCard;

    public void InitializeDeck()
    {
        drawPile = new List<Card>(PlayerDeck.Instance.GetDeck());
        discardPile.Clear();
        Logger.Log($"[DeckManager] 초기화: drawPile={drawPile.Count}, discard={discardPile.Count}", this);
    }

    public void ShuffleDeck()
    {
        System.Random rng = new System.Random();
        int n = drawPile.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (drawPile[k], drawPile[n]) = (drawPile[n], drawPile[k]);
        }
        Logger.Log("[DeckManager] 셔플 완료.", this);
        RelicManager.Instance?.OnDeckShuffled();
    }

    public async Task DrawCardAsync()
    {
        if (isDrawingSafeLock) return;

        if (HandManager.Instance != null && HandManager.Instance.HasCurseEffectInHand(CardEffectType.CurseNoDraw))
        {
            Debug.Log("[저주 제한 발동] 손패에 드로우 금지 저주 카드가 잔류하고 있어 '추가 드로우 효과'가 취소되었습니다.");
            return;
        }

        await ExecuteActualDrawLogic();
    }

    public async Task DrawCardIgnoreCurseAsync()
    {
        if (isDrawingSafeLock) return;
        await ExecuteActualDrawLogic();
    }

    private async Task ExecuteActualDrawLogic()
    {
        if (drawPile.Count == 0)
            ReshuffleDiscardPile();

        if (drawPile.Count > 0 && HandManager.Instance.CurrentHandSize < HandManager.Instance.MaxHandSize)
        {
            isDrawingSafeLock = true;

            Card drawnCard = drawPile[0];
            drawPile.RemoveAt(0);

            GameObject newCardObject = Instantiate(cardPrefab);
            RectTransform cardRect = newCardObject.GetComponent<RectTransform>();
            newCardObject.transform.SetParent(HandManager.Instance.handTransform, false);

            Vector2 uiStartPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                HandManager.Instance.handTransform as RectTransform,
                RectTransformUtility.WorldToScreenPoint(null, drawPileAnchor.position),
                null,
                out uiStartPos
            );

            AudioManager.Instance?.PlaySFX("Card_Draw");

            cardRect.anchoredPosition = uiStartPos;
            cardRect.localScale = Vector3.one * 0.5f;
            cardRect.SetAsLastSibling();

            // ★ [광기 코스트 무작위 하이재킹 연산 구역]
            // 1. 이미 손패에 광기 카드가 있거나, 지금 드로우 된 카드 자체가 광기 저주라면 무조건 1~3 사이로 강제 조정 (0코 대박 차단)
            if ((HandManager.Instance != null && HandManager.Instance.HasMadnessCurseInHand()) ||
                drawnCard.GetCardEffects().Exists(e => e.effectType == CardEffectType.CurseMadness))
            {
                drawnCard.energyCost = UnityEngine.Random.Range(1, 4); // 1, 2, 3 중 무작위 선택
                Debug.Log($"[광기 디버프] '{drawnCard.cardName}' 카드가 이펙트 오염으로 강제 변환되었습니다 (코스트: {drawnCard.energyCost})");
            }
            // 2. 광기 상태는 없는데 기존 스네코 영구 효과가 걸려있다면 0~3 무작위 변환 실행
            else if (drawnCard.isSnecko)
            {
                drawnCard.energyCost = UnityEngine.Random.Range(0, 4); // 0, 1, 2, 3 중 무작위 선택
            }

            CardDisplay display = newCardObject.GetComponent<CardDisplay>();
            display.cardData = drawnCard;
            display.UpdateCardDisplay();

            Transform slotTarget = HandManager.Instance.GetNextCardSlotPosition();
            RectTransform slotRect = slotTarget.GetComponent<RectTransform>();
            Vector2 midPoint = (cardRect.anchoredPosition + slotRect.anchoredPosition) / 2f + Vector2.up * 100f;

            TaskCompletionSource<bool> tcs = new();

            DOTween.Sequence()
                .Append(cardRect.DOAnchorPos(midPoint, 0.20f).SetEase(Ease.OutCubic))
                .Append(cardRect.DOAnchorPos(slotRect.anchoredPosition, 0.20f).SetEase(Ease.InCubic))
                .Join(cardRect.DOScale(1f, 0.25f))
                .OnComplete(() =>
                {
                    HandManager.Instance.AddCardToHand(newCardObject);
                    Destroy(slotTarget.gameObject);
                    tcs.SetResult(true);
                });

            NotifyDrawPileUI();

            await tcs.Task;

            isDrawingSafeLock = false;
        }
    }

    public void DiscardCard(Card card)
    {
        discardPile.Add(card);
        OnDiscardCard?.Invoke(card);
        OnDiscardPileChanged?.Invoke();
    }

    public void MoveToDiscardPile(Card card)
    {
        if (card != null)
        {
            discardPile.Add(card);
            NotifyDiscardPileUI();
        }
    }

    private void ReshuffleDiscardPile()
    {
        if (discardPile.Count == 0) return;
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        ShuffleDeck();
        NotifyDiscardPileUI();
    }

    public int GetDrawPileCount() => drawPile.Count;
    public int GetDiscardPileCount() => discardPile.Count;

    private void NotifyDrawPileUI() => OnDrawPileChanged?.Invoke();
    private void NotifyDiscardPileUI() => OnDiscardPileChanged?.Invoke();
}