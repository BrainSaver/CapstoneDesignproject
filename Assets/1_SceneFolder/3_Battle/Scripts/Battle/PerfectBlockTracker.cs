using System;

/// <summary>
/// 이번 턴 상쇄(방어도로 데미지를 정확히 0으로 막음) 성공 여부를 추적한다.
/// </summary>
public static class PerfectBlockTracker
{
    /// <summary>이번 턴 상쇄 성공 횟수.</summary>
    public static int PerfectBlockCount { get; private set; } = 0;

    /// <summary>다음 턴 무료 카드 수.</summary>
    public static int FreeCardsNextTurn { get; private set; } = 0;

    /// <summary>상쇄 성공 시 발행.</summary>
    public static event Action OnPerfectBlock;

    /// <summary>상쇄 성공 처리.</summary>
    public static void RegisterPerfectBlock()
    {
        PerfectBlockCount++;
        Logger.Log($"[PerfectBlockTracker] 상쇄 성공! 총 {PerfectBlockCount}회.");
        if (RelicManager.Instance != null && RelicManager.Instance.HasRelic("holyRing"))
        {
            DrawNextTurnTracker.Add(2);
        }
        OnPerfectBlock?.Invoke();
    }

    /// <summary>다음 턴 무료 카드 수를 추가한다.</summary>
    public static void AddFreeCardsNextTurn(int count)
    {
        FreeCardsNextTurn += count;
        Logger.Log($"[PerfectBlockTracker] 다음 턴 무료 카드 {count}장 추가. 총 {FreeCardsNextTurn}장.");
    }

    /// <summary>턴 시작 시 무료 카드 적용 후 초기화.</summary>
    public static int ConsumeAndReset()
    {
        int free = FreeCardsNextTurn;
        FreeCardsNextTurn = 0;
        PerfectBlockCount = 0;
        return free;
    }

    /// <summary>턴 종료 시 이번 턴 상쇄 횟수 + 이벤트 초기화.</summary>
    public static void ResetTurn()
    {
        PerfectBlockCount = 0;
        OnPerfectBlock = null; // ← 미발동 구독 전부 정리
    }
}