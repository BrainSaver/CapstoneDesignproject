using UnityEngine;

/// <summary>
/// 모든 카드/포션 이펙트의 기반 추상 클래스.
/// ScriptableObject를 상속받아 에셋으로 관리된다.
/// </summary>
public abstract class EffectData : ScriptableObject
{
    public Card.TargetType targetType;

    /// <summary>이펙트를 source → target 방향으로 적용한다.</summary>
    public abstract void ApplyEffect(CharacterStats source, CharacterStats target);
}