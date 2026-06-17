using UnityEngine;
/// <summary>
/// 플레이어 이동 불가 상태를 추적한다.
/// MovePointManager에서 포인트 지급 시 확인한다.
/// </summary>
public static class ImmobilizeTracker
{
    public static int RemainingTurns { get; private set; } = 0;

    /// <summary>이동 불가 상태를 적용한다.</summary>
    public static void Apply(int duration)
    {
        if (RelicManager.Instance != null && RelicManager.Instance.IsStatusImmune)
        {
            Logger.Log("[ImmobilizeTracker] 상태 이상 무효화 효과로 인해 이동 불가가 부여되지 않았습니다.");
            return;
        }
        RemainingTurns = Mathf.Max(RemainingTurns, duration);
        Logger.Log($"[ImmobilizeTracker] 이동 불가 {RemainingTurns}턴.");
    }

    /// <summary>이동 불가 상태인지 여부.</summary>
    public static bool IsImmobilized => RemainingTurns > 0;

    /// <summary>턴 시작 시 지속 턴 감소.</summary>
    public static void Tick()
    {
        if (RemainingTurns <= 0) return;
        RemainingTurns--;
        Logger.Log($"[ImmobilizeTracker] 이동 불가 남은 턴: {RemainingTurns}.");
    }

    /// <summary>초기화.</summary>
    public static void Reset() => RemainingTurns = 0;
}