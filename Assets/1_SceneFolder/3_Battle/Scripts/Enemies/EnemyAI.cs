using UnityEngine;

/// <summary>
/// 적이 다음 턴에 취할 행동의 종류.
/// </summary>
public enum IntentType
{
    Attack,  // 공격
    Buff,    // 버프/강화
    Special, // 특수 행동 (소환, 각성 등)
}

/// <summary>
/// 적이 다음 턴에 취할 행동을 플레이어에게 예고하는 인텐트 데이터.
/// </summary>
public class EnemyIntent
{
    public IntentType Type { get; private set; } // 행동 종류
    public string Description { get; private set; } // 표시할 텍스트
    public int Value { get; private set; } // 수치 (데미지 등)
    public Sprite Icon { get; private set; } // 표시할 아이콘

    public EnemyIntent(IntentType type, string description, int value, Sprite icon)
    {
        Type = type;
        Description = description;
        Value = value;
        Icon = icon;
    }
}