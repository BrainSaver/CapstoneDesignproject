/// <summary>
/// 손패 전체 보존 효과를 추적한다.
/// TurnManager의 턴 종료 시 HandManager에서 확인한다.
/// </summary>
public static class RetainHandTracker
{
    public static bool IsActive { get; private set; } = false;

    /// <summary>손패 보존 효과를 활성화한다.</summary>
    public static void Activate()
    {
        IsActive = true;
        Logger.Log("[RetainHandTracker] 손패 보존 활성화.");
    }

    /// <summary>효과를 소비한다.</summary>
    public static void Consume()
    {
        IsActive = false;
        Logger.Log("[RetainHandTracker] 손패 보존 소비.");
    }

    /// <summary>턴 시작 시 초기화한다.</summary>
    public static void Reset() => IsActive = false;
}