/// <summary>
/// 플레이어 취약 상태를 추적한다.
/// 활성화 시 플레이어가 받는 데미지가 50% 증가한다.
/// </summary>
public static class PlayerVulnerableTracker
{
    /// <summary>남은 취약 지속 턴.</summary>
    public static int RemainingTurns { get; private set; } = 0;

    /// <summary>취약 상태인지 여부.</summary>
    public static bool IsActive => RemainingTurns > 0;

    /// <summary>취약을 적용한다.</summary>
    public static void Apply(int duration)
    {
        if (RelicManager.Instance != null && RelicManager.Instance.IsStatusImmune)
        {
            Logger.Log("[PlayerVulnerableTracker] 상태 이상 무효화 효과로 인해 취약이 부여되지 않았습니다.");
            return;
        }
        RemainingTurns = duration;
        Logger.Log($"[PlayerVulnerableTracker] 플레이어 취약 {duration}턴 적용.");
    }

    /// <summary>데미지에 취약을 적용한다. 50% 증가.</summary>
    public static int ApplyToDamage(int damage)
    {
        if (!IsActive) return damage;
        int increased = UnityEngine.Mathf.RoundToInt(damage * 1.5f);
        Logger.Log($"[PlayerVulnerableTracker] 취약 적용: {damage} → {increased}");
        return increased;
    }

    /// <summary>턴 시작 시 호출. 지속 턴 감소.</summary>
    public static void Tick()
    {
        if (RemainingTurns <= 0) return;
        RemainingTurns--;
        Logger.Log($"[PlayerVulnerableTracker] 취약 남은 턴: {RemainingTurns}");
    }

    /// <summary>전투 종료 시 초기화.</summary>
    public static void Reset()
    {
        RemainingTurns = 0;
    }
}