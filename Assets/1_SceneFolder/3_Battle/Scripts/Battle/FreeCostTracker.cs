using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이번 턴 비용 없이 사용 가능한 카드를 추적한다.
/// PlayerManager.CanPlayCard()에서 확인한다.
/// </summary>
public static class FreeCostTracker
{
    // 이번 턴 무료 사용 가능한 카드 목록
    private static readonly List<Card> freeCards = new();

    /// <summary>카드를 이번 턴 무료 사용 목록에 추가한다.</summary>
    public static void AddFreeCard(Card card)
    {
        if (card == null) return;
        freeCards.Add(card);
        Logger.Log($"[FreeCostTracker] '{card.cardName}' 무료 사용 등록.");
    }

    /// <summary>카드가 이번 턴 무료 사용 가능한지 확인한다.</summary>
    public static bool IsFree(Card card)
    {
        if (card == null) return false;
        return freeCards.Contains(card);
    }

    /// <summary>무료 카드를 사용 후 목록에서 제거한다.</summary>
    public static void ConsumeCard(Card card)
    {
        if (card == null) return;
        freeCards.Remove(card);
        Logger.Log($"[FreeCostTracker] '{card.cardName}' 무료 사용 소비.");
    }

    /// <summary>턴 시작 시 무료 카드 목록을 초기화한다.</summary>
    public static void Reset()
    {
        freeCards.Clear();
        Logger.Log("[FreeCostTracker] 무료 카드 목록 초기화.");
    }
}