/// <summary>
/// 다음 공격 카드 데미지 2배 효과를 추적한다.
/// DamageEffect에서 데미지 계산 시 확인한다.
/// </summary>
public static class DoubleAttackTracker
{
    public static bool IsActive { get; private set; } = false;

    /// <summary>다음 공격 2배 효과를 활성화한다.</summary>
    public static void Activate()
    {
        IsActive = true;
        Logger.Log("[DoubleAttackTracker] 다음 공격 2배 활성화.");
    }

    /// <summary>효과를 소비하고 배율을 반환한다. 활성화됐으면 2, 아니면 1.</summary>
    public static int ConsumeMultiplier()
    {
        if (!IsActive) return 1;
        IsActive = false;
        Logger.Log("[DoubleAttackTracker] 다음 공격 2배 소비.");
        return 2;
    }

    /// <summary>턴 시작 시 초기화한다.</summary>
    public static void Reset()
    {
        IsActive = false;
    }
}