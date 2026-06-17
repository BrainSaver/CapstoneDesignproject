using System;

/// <summary>
/// 플레이어 무뎌짐(Dullness) 상태를 추적한다.
/// 활성화 시 플레이어가 가하는 데미지가 25% 감소한다.
/// </summary>
public static class DullnessTracker
{
    public static int RemainingTurns { get; private set; } = 0;
    public static bool IsActive => RemainingTurns > 0;

    /// <summary>무뎌짐 수치 변경 시 발행. UI 갱신용.</summary>
    public static event Action<int> OnChanged;

    public static void Apply(int duration)
    {
        if (RelicManager.Instance != null && RelicManager.Instance.IsStatusImmune)
        {
            Logger.Log("[DullnessTracker] 상태 이상 무효화 효과로 인해 무뎌짐이 부여되지 않았습니다.");
            return;
        }
        RemainingTurns = duration;
        OnChanged?.Invoke(RemainingTurns);
        Logger.Log($"[DullnessTracker] 플레이어 무뎌짐 {duration}턴 적용.");
    }

    public static int ApplyToDamage(int damage)
    {
        if (!IsActive) return damage;
        int reduced = UnityEngine.Mathf.RoundToInt(damage * 0.75f);
        Logger.Log($"[DullnessTracker] 무뎌짐 적용: {damage} → {reduced}");
        return reduced;
    }

    public static void Tick()
    {
        if (RemainingTurns <= 0) return;
        RemainingTurns--;
        OnChanged?.Invoke(RemainingTurns);
        Logger.Log($"[DullnessTracker] 무뎌짐 남은 턴: {RemainingTurns}");
    }

    public static void Reset()
    {
        RemainingTurns = 0;
        OnChanged?.Invoke(RemainingTurns);
    }
}