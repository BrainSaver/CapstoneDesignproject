using UnityEngine;

/// <summary>
/// 이동 카드에 적용될 보너스 이동 거리를 추적한다.
/// 이동 카드 사용 시 보너스를 소비하고 초기화한다.
/// </summary>
public class MoveBonusTracker : SceneSingleton<MoveBonusTracker>
{
    /// <summary>현재 누적된 이동 보너스.</summary>
    public int CurrentBonus { get; private set; } = 0;

    /// <summary>이동 보너스를 추가한다.</summary>
    public void AddBonus(int amount)
    {
        CurrentBonus += amount;
        Logger.Log($"[MoveBonusTracker] 보너스 추가: +{amount}, 총 보너스: {CurrentBonus}");
    }

    /// <summary>이동 보너스를 소비하고 초기화한다. 소비된 보너스 값을 반환한다.</summary>
    public int ConsumeBonus()
    {
        int bonus = CurrentBonus;
        CurrentBonus = 0;
        Logger.Log($"[MoveBonusTracker] 보너스 소비: {bonus}");
        return bonus;
    }

    /// <summary>턴 시작 시 미사용 보너스를 초기화한다.</summary>
    public void ResetBonus()
    {
        CurrentBonus = 0;
        Logger.Log("[MoveBonusTracker] 보너스 초기화.");
    }
}