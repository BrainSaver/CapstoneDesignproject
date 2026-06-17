using UnityEngine;

/// <summary>
/// 모든 적 AI가 구현해야 하는 인터페이스.
/// 턴 실행, 인텐트 예고, 초기화 등을 정의한다.
/// </summary>
public interface IEnemyAI
{
    /// <summary>적 턴에 행동을 실행한다.</summary>
    void ExecuteTurn();

    /// <summary>플레이어 스탯 참조를 설정한다.</summary>
    void SetPlayerStats(CharacterStats player);

    /// <summary>인텐트 아이콘을 설정한다.</summary>
    void SetIntentIcons(Sprite attack, Sprite buff);

    /// <summary>AI를 초기화하고 첫 인텐트를 결정한다.</summary>
    void InitializeAI();

    /// <summary>다음 턴 인텐트를 반환한다 (재계산 없이 잠긴 값).</summary>
    EnemyIntent PredictNextIntent();

    /// <summary>현재 인텐트를 반환한다.</summary>
    EnemyIntent GetCurrentIntent();

    /// <summary>EnemyDisplay 참조를 설정한다.</summary>
    void SetEnemyDisplay(EnemyDisplay display);
}