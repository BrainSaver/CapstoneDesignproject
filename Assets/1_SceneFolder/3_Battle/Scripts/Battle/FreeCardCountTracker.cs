using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다음 N장의 카드를 0코스트로 사용할 수 있게 추적한다.
/// 상쇄 보너스 등에서 사용한다.
/// </summary>
public static class FreeCardCountTracker
{
    /// <summary>남은 무료 사용 가능 카드 수.</summary>
    public static int FreeCardCount { get; private set; } = 0;

    /// <summary>무료 카드 수를 추가한다.</summary>
    public static void AddFreeCards(int count)
    {
        FreeCardCount += count;
        Logger.Log($"[FreeCardCountTracker] 무료 카드 +{count}. 총 {FreeCardCount}장.");
    }

    /// <summary>카드가 무료 사용 가능한지 확인한다.</summary>
    public static bool HasFreeCard() => FreeCardCount > 0;

    /// <summary>무료 카드 1장을 소비한다.</summary>
    public static void ConsumeOne()
    {
        if (FreeCardCount <= 0) return;
        FreeCardCount--;
        Logger.Log($"[FreeCardCountTracker] 무료 카드 소비. 남은 {FreeCardCount}장.");
    }

    /// <summary>턴 시작 시 초기화한다.</summary>
    public static void Reset()
    {
        FreeCardCount = 0;
    }
}