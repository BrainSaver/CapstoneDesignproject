using UnityEngine;

/// <summary>
/// 플레이어가 받는 데미지 감소 효과를 추적한다.
/// 
public static class DamageReductionTracker
{
    public static float CurrentReduction { get; private set; } = 0f;
    private static int remainingTurns = 0;

    public static void ApplyReduction(float rate, int duration)
    {
        CurrentReduction = Mathf.Clamp01(CurrentReduction + rate);
        remainingTurns = Mathf.Max(remainingTurns, duration);
    }

    public static void TickReduction()
    {
        if (remainingTurns <= 0) return;
        remainingTurns--;
        if (remainingTurns <= 0) CurrentReduction = 0f;
    }

    public static int ApplyToDamage(int damage)
        => Mathf.RoundToInt(damage * (1f - CurrentReduction));

    public static void Reset()
    {
        CurrentReduction = 0f;
        remainingTurns = 0;
    }
}