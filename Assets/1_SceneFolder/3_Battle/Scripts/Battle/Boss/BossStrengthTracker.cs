/// <summary>
/// 보스 공격력 증감을 추적하는 static 클래스.
/// 전투 시작 시 Reset()으로 초기화한다.
/// </summary>
using System;

public static class BossStrengthTracker
{
    public static int CurrentBonus { get; private set; } = 0;

    /// <summary>보스 공격력 변경 시 발행.</summary>
    public static event Action OnBonusChanged;

    public static void AddBonus(int amount)
    {
        CurrentBonus += amount;
        OnBonusChanged?.Invoke();
        Logger.Log($"[BossStrengthTracker] 보스 공격력 +{amount}. 총 보너스: {CurrentBonus}");
    }

    public static void ReduceBonus(int amount)
    {
        CurrentBonus -= amount;
        OnBonusChanged?.Invoke();
        Logger.Log($"[BossStrengthTracker] 보스 공격력 -{amount}. 총 보너스: {CurrentBonus}");
    }

    public static void Reset()
    {
        CurrentBonus = 0;
        OnBonusChanged?.Invoke();
        Logger.Log("[BossStrengthTracker] 초기화.");
    }
}