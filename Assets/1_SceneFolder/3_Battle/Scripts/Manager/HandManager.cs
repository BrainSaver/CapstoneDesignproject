using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 플레이어 손패 관리: 드로우, 추가, 제거, 부채꼴 배치를 담당한다.
/// </summary>
public class HandManager : SceneSingleton<HandManager>
{
    [Header("카드 설정")]
    public GameObject cardPrefab;
    public Transform handTransform;

    [Header("배치 설정")]
    public float fanSpread = 7.5f;
    public float cardSpacing = 100f;
    public float verticalSpacing = 100f;

    [Header("손패 크기 설정")]
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private int startingHandSize = 5;

    public int MaxHandSize
    {
        get
        {
            int size = maxHandSize;
            if (RelicManager.Instance != null && RelicManager.Instance.HasRelic("archbishopsGloves"))
                size += 1;
            return size;
        }
    }
    public int StartingHandSize => startingHandSize;
    public int CurrentHandSize => cardsInHand.Count;
    public bool IsDrawing { get; private set; }
    public bool IsStartingTurnDraw { get; set; } = false;

    private readonly List<GameObject> cardsInHand = new();
    public IReadOnlyList<GameObject> CardsInHand => cardsInHand;

    [SerializeField] private Transform discardPileAnchor;

    /// <summary>손패가 변경될 때 발행. 마력흡수 인텐트 갱신 등에 사용.</summary>
    public static event Action OnHandChanged;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) UpdateHandLayout();
    }
#endif

    /// <summary>count장 카드를 드로우한다. 완료 후 레이아웃을 갱신한다.</summary>
    public IEnumerator DrawCardsRoutine(int count, bool isStartingTurn = false)
    {
        if (count <= 0) yield break;

        IsDrawing = true;
        IsStartingTurnDraw = isStartingTurn;

        for (int i = 0; i < count; i++)
        {
            if (isStartingTurn)
                yield return DeckManager.Instance.DrawCardIgnoreCurseAsync().AsCoroutine();
            else
                yield return DeckManager.Instance.DrawCardAsync().AsCoroutine();
        }

        yield return null;
        UpdateHandLayout();
        IsDrawing = false;
        IsStartingTurnDraw = false;
    }

    /// <summary>턴 시작 시 호출. 손패 공간에 맞게 카드를 뽑는다.</summary>
    public void DrawCardsForTurn()
    {
        int spaceLeft = MaxHandSize - CurrentHandSize;
        int cardsToDraw = Mathf.Min(startingHandSize, spaceLeft);

        if (cardsToDraw > 0)
            StartCoroutine(DrawCardsRoutine(cardsToDraw, true));
    }

    /// <summary>손패에 카드 GameObject를 추가하고 레이아웃을 갱신한다.</summary>
    public void AddCardToHand(GameObject cardObject)
    {
        if (CurrentHandSize >= MaxHandSize)
        {
            Logger.LogWarning("[HandManager] 손패 가득 참.", this);
            return;
        }
        cardsInHand.Add(cardObject);

        // ✅ 손패 변경 이벤트 발행
        OnHandChanged?.Invoke();

        // 광기 디버프 전파
        if (HasMadnessCurseInHand())
            ApplyMadnessToExistingHand();
        else
            UpdateHandLayout();
    }

    /// <summary>카드를 보존(Retain) 상태로 손패에 직접 추가한다.</summary>
    public void AddCardToHandDirectlyWithRetain(Card card)
    {
        if (card == null) return;
        if (cardsInHand.Count >= MaxHandSize) return;

        GameObject cardObj = Instantiate(cardPrefab, handTransform);
        CardDisplay display = cardObj.GetComponent<CardDisplay>();

        if (display != null)
        {
            display.cardData = card;
            display.UpdateCardDisplay();
        }

        var movement = cardObj.GetComponent<CardMovement>();
        if (movement != null)
        {
            movement.isRetained = true;
            // ✅ rectTransform이 초기화될 때까지 대기
            movement.ForceInitialize();
        }

        cardsInHand.Add(cardObj);
        OnHandChanged?.Invoke();
        UpdateHandLayout();
        Logger.Log($"[HandManager] '{card.cardName}' 보존 상태로 손패에 추가.");
    }

    /// <summary>손패 카드들을 부채꼴로 배치하고 CardMovement에 원본 트랜스폼을 저장시킨다.</summary>
    private void UpdateHandLayout()
    {
        int cardCount = cardsInHand.Count;

        if (cardCount == 1)
        {
            cardsInHand[0].transform.localRotation = Quaternion.identity;
            cardsInHand[0].transform.localPosition = Vector3.zero;
            cardsInHand[0].GetComponent<CardMovement>()?.SaveOriginalTransform();
            return;
        }

        for (int i = 0; i < cardCount; i++)
        {
            var cm = cardsInHand[i].GetComponent<CardMovement>();
            if (cm) cm.isInHand = true;

            float t = (float)i / (cardCount - 1);
            float normalized = (2f * t) - 1f;

            float angle = -fanSpread * normalized;
            float xOffset = cardSpacing * (i - (cardCount - 1) / 2f);
            float yOffset = -verticalSpacing * (normalized * normalized);

            cardsInHand[i].transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            cardsInHand[i].transform.localPosition = new Vector3(xOffset, yOffset, 0f);

            cm?.SaveOriginalTransform();
        }
    }

    /// <summary>손패에서 카드를 제거하고, 필요 시 버린 더미로 보낸다.</summary>
    public void RemoveCardFromHand(GameObject cardObject, bool destroyGO = true)
    {
        if (cardObject != null && cardsInHand.Contains(cardObject))
        {
            cardsInHand.Remove(cardObject);

            // ✅ 손패 변경 이벤트 발행
            OnHandChanged?.Invoke();

            if (destroyGO)
                cardObject.GetComponent<CardMovement>()?.SetEnabled(false);

            var cd = cardObject.GetComponent<CardDisplay>();
            if (cd != null)
            {
                if (cd.cardData.exhaustAfterUse)
                {
                    ExhaustPile.AddCard(cd.cardData);
                    RelicManager.Instance?.OnCardExhausted(cd.cardData);
                    Logger.Log($"[HandManager] '{cd.cardData.cardName}' 소멸.");
                }
                else
                    DeckManager.Instance?.DiscardCard(cd.cardData);
            }

            cardObject.GetComponent<RectTransform>()?.DOKill();
            UpdateHandLayout();

            if (destroyGO) Destroy(cardObject, 0.01f);
        }
        else
        {
            Logger.LogWarning("[HandManager] 제거하려는 카드가 없습니다.", this);
        }
    }

    /// <summary>카드를 버린 더미로 강제 이동 (exhaustAfterUse 무시).</summary>
    private void RemoveCardFromHandToDiscard(GameObject cardObject)
    {
        if (cardObject != null && cardsInHand.Contains(cardObject))
        {
            cardsInHand.Remove(cardObject);

            // ✅ 손패 변경 이벤트 발행
            OnHandChanged?.Invoke();

            cardObject.GetComponent<CardMovement>()?.SetEnabled(false);

            var cd = cardObject.GetComponent<CardDisplay>();
            if (cd != null)
                DeckManager.Instance?.DiscardCard(cd.cardData);

            cardObject.GetComponent<RectTransform>()?.DOKill();
            UpdateHandLayout();
            Destroy(cardObject, 0.01f);
        }
    }

    /// <summary>다음 카드 슬롯 위치를 계산해 임시 앵커로 반환한다.</summary>
    public Transform GetNextCardSlotPosition()
    {
        GameObject tempCard = new GameObject("TempCard", typeof(RectTransform));
        tempCard.transform.SetParent(handTransform, false);
        cardsInHand.Add(tempCard);
        UpdateHandLayout();

        Vector3 pos = tempCard.GetComponent<RectTransform>().anchoredPosition;
        cardsInHand.Remove(tempCard);
        Destroy(tempCard);

        return CreateTempAnchorAt(pos);
    }

    private Transform CreateTempAnchorAt(Vector3 anchoredPos)
    {
        GameObject anchor = new GameObject("CardTargetAnchor", typeof(RectTransform));
        anchor.transform.SetParent(handTransform, false);
        anchor.GetComponent<RectTransform>().anchoredPosition = anchoredPos;
        return anchor.transform;
    }

    /// <summary>카드를 버린 더미 앵커로 애니메이션하며 제거한다.</summary>
    public IEnumerator AnimateDiscardAndRemoveCard(GameObject card)
    {
        var rt = card.GetComponent<RectTransform>();
        if (rt == null || discardPileAnchor == null) yield break;

        float duration = 0.5f;
        rt.SetAsLastSibling();
        rt.DOMove(discardPileAnchor.position, duration).SetEase(Ease.InBack);
        rt.DOScale(Vector3.zero, duration);
        rt.DORotate(new Vector3(0, 0, 180f), duration, RotateMode.FastBeyond360);

        yield return new WaitForSeconds(duration);
        RemoveCardFromHandToDiscard(card);
    }

    public IEnumerator DiscardHandRoutine(bool animated = true)
    {
        if (RetainHandTracker.IsActive)
        {
            RetainHandTracker.Consume();
            Logger.Log("[HandManager] 손패 보존 — 패를 유지합니다.");
            yield break;
        }

        var snapshot = new List<GameObject>(cardsInHand);
        foreach (var card in snapshot)
        {
            if (card == null) continue;

            var display = card.GetComponent<CardDisplay>();
            if (display?.cardData != null)
            {
                // 기존 Retain 효과 체크
                bool hasRetain = display.cardData.effects
                    .Exists(e => e.effectType == CardEffectType.Retain);
                if (hasRetain)
                {
                    Logger.Log($"[HandManager] '{display.cardData.cardName}' 개별 보존.");
                    continue;
                }
            }

            // isRetained 플래그 체크 (악마의 티끌 등 보존 상태로 배포된 카드)
            var movement = card.GetComponent<CardMovement>();
            if (movement != null && movement.isRetained)
            {
                Logger.Log($"[HandManager] '{display?.cardData?.cardName}' 보존 상태 — 유지.");
                continue;
            }

            if (animated && discardPileAnchor != null)
                StartCoroutine(AnimateDiscardAndRemoveCard(card));
            else
                RemoveCardFromHandToDiscard(card);
        }

        yield return new WaitForSeconds(animated ? 0.55f : 0f);
        UpdateHandLayout();
    }

    /// <summary>손패 전체를 즉시 버린다 (애니메이션 없음).</summary>
    public void DiscardEntireHandInstant()
    {
        var snapshot = new List<GameObject>(cardsInHand);
        foreach (var card in snapshot)
            if (card != null) RemoveCardFromHandToDiscard(card);
        UpdateHandLayout();
    }

    /// <summary>카드 에셋을 직접 받아 손패에 즉시 추가한다. (덱/버린더미 거치지 않음)</summary>
    public void AddCardToHandDirectly(Card card)
    {
        if (card == null) return;
        if (CurrentHandSize >= MaxHandSize) return;

        GameObject newCardObject = Instantiate(cardPrefab, handTransform);
        CardDisplay display = newCardObject.GetComponent<CardDisplay>();

        if (display != null)
        {
            display.cardData = card;
            display.UpdateCardDisplay();
        }

        cardsInHand.Add(newCardObject);

        // ✅ 손패 변경 이벤트 발행
        OnHandChanged?.Invoke();

        UpdateHandLayout();
    }

    /// <summary>손패의 모든 카드 UI를 갱신한다.</summary>
    public void RefreshHandDisplay()
    {
        foreach (var cardObj in cardsInHand)
        {
            if (cardObj != null)
                cardObj.GetComponent<CardDisplay>()?.UpdateCardDisplay();
        }
    }

    /// <summary>현재 손패에 특정 효과를 가진 저주 카드가 존재하는지 실시간으로 검색합니다.</summary>
    public bool HasCurseEffectInHand(CardEffectType targetEffect)
    {
        foreach (GameObject cardObj in cardsInHand)
        {
            if (cardObj == null) continue;

            CardDisplay display = cardObj.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null)
            {
                foreach (var effect in display.cardData.GetCardEffects())
                {
                    if (effect.effectType == targetEffect) return true;
                }
            }
        }
        return false;
    }

    /// <summary>현재 손패에 '광기' 저주 카드가 존재하는지 실시간으로 검색합니다.</summary>
    public bool HasMadnessCurseInHand()
    {
        return HasCurseEffectInHand(CardEffectType.CurseMadness);
    }

    /// <summary>광기 저주 효과가 패에 있을 때, 손패 내 다른 모든 카드의 비용을 1~3 사이로 무작위 강제 재정산합니다.</summary>
    public void ApplyMadnessToExistingHand()
    {
        foreach (GameObject cardObj in cardsInHand)
        {
            if (cardObj == null) continue;
            CardDisplay display = cardObj.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null)
            {
                if (display.cardData.energyCost < 1 || display.cardData.energyCost > 3)
                    display.cardData.energyCost = UnityEngine.Random.Range(1, 4);
            }
        }
        UpdateHandLayout();
        RefreshHandDisplay();
    }
}