using System;

/// <summary>
/// 플레이어 노출(Exposed) 상태를 추적한다.
/// 활성화 시 플레이어가 받는 데미지가 50% 증가한다.
/// </summary>
public static class ExposedTracker
{
    public static int RemainingTurns { get; private set; } = 0;
    public static bool IsActive => RemainingTurns > 0;

    /// <summary>노출 수치 변경 시 발행. UI 갱신용.</summary>
    public static event Action<int> OnChanged;

    public static void Apply(int duration)
    {
        if (RelicManager.Instance != null && RelicManager.Instance.IsStatusImmune)
        {
            Logger.Log("[ExposedTracker] 상태 이상 무효화 효과로 인해 노출(Exposed)이 부여되지 않았습니다.");
            return;
        }
        RemainingTurns = duration;
        OnChanged?.Invoke(RemainingTurns);
        Logger.Log($"[ExposedTracker] 플레이어 노출 {duration}턴 적용.");
    }

    public static int ApplyToDamage(int damage)
    {
        if (!IsActive) return damage;
        int increased = UnityEngine.Mathf.RoundToInt(damage * 1.5f);
        Logger.Log($"[ExposedTracker] 노출 적용: {damage} → {increased}");
        return increased;
    }

    public static void Tick()
    {
        if (RemainingTurns <= 0) return;
        RemainingTurns--;
        OnChanged?.Invoke(RemainingTurns);
        Logger.Log($"[ExposedTracker] 노출 남은 턴: {RemainingTurns}");
    }

    public static void Reset()
    {
        RemainingTurns = 0;
        OnChanged?.Invoke(RemainingTurns);
    }
}