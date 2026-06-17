using UnityEngine;

/// <summary>
/// 이번 턴 받은 피해를 추적하고 다음 공격에 추가 데미지로 반영한다.
/// </summary>
public static class RevengeTracker
{
    public static int DamageTakenThisTurn { get; private set; } = 0;
    private static bool isActive = false;

    public static void ActivateRevenge() => isActive = true;

    public static void RecordDamage(int damage)
    {
        DamageTakenThisTurn += damage;
    }

    public static int ConsumeRevengeDamage()
    {
        if (!isActive) return 0;
        int bonus = DamageTakenThisTurn;
        isActive = false;
        DamageTakenThisTurn = 0;
        return bonus;
    }

    public static void ResetTurn() => DamageTakenThisTurn = 0;
}