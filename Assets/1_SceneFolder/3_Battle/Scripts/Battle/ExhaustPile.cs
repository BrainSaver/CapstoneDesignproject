using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소멸된 카드를 보관하고 복구하는 시스템.
/// </summary>
public static class ExhaustPile
{
    private static readonly List<Card> exhaustedCards = new();
    /// <summary>소멸 카드 목록 복사본을 반환한다.</summary>
    public static List<Card> GetDeck() => new List<Card>(exhaustedCards);

    public static int Count => exhaustedCards.Count;

    /// <summary>소멸 카드 수 변경 시 발행.</summary>
    public static event Action OnExhaustChanged;

    /// <summary>카드를 소멸 묘지에 추가한다.</summary>
    public static void AddCard(Card card)
    {
        if (card == null) return;
        exhaustedCards.Add(card);
        OnExhaustChanged?.Invoke();
        Logger.Log($"[ExhaustPile] '{card.cardName}' 소멸.");
    }


    /// <summary>소멸 카드를 덱으로 복구한다. count가 0이면 전부 복구.</summary>
    public static void RecoverCards(int count)
    {
        if (exhaustedCards.Count == 0)
        {
            Logger.LogWarning("[ExhaustPile] 소멸된 카드가 없습니다.");
            return;
        }

        int actualCount = count <= 0
            ? exhaustedCards.Count
            : Mathf.Min(count, exhaustedCards.Count);

        for (int i = 0; i < actualCount; i++)
        {
            Card card = exhaustedCards[0];
            exhaustedCards.RemoveAt(0);
            DeckManager.Instance?.DiscardCard(card);
            Logger.Log($"[ExhaustPile] '{card.cardName}' 덱으로 복구.");
        }

        OnExhaustChanged?.Invoke();
    }

    /// <summary>소멸 묘지를 초기화한다.</summary>
    public static void Reset()
    {
        exhaustedCards.Clear();
        OnExhaustChanged?.Invoke();
        Logger.Log("[ExhaustPile] 소멸 묘지 초기화.");
    }
}