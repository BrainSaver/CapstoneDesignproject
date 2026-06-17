using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 효과의 종류를 정의합니다. (기존 데이터 보존을 위해 새 효과는 맨 아래에만 추가합니다)
/// </summary>
public enum CardEffectType
{
    None,
    Damage,                  // 단일 데미지
    Armor,                   // 방어도 획득
    Heal,                    // 체력 회복
    DrawCard,                // 카드 뽑기
    GainEnergy,              // 에너지(마나) 획득
    LoseHealth,              // 체력 감소
    AOEDamage,               // 광역 데미지
    SetHealth,               // 체력 고정
    ApplyBleed,              // 출혈 부여
    ApplyWeaken,             // 약화 부여
    Retain,                  // 보존
    AntiArmorDamage,         // 방어도 2배 데미지
    DoubleBleed,             // 출혈량 2배
    DamageEqualArmor,        // 방어도와 동일한 데미지 
    IgnoreArmorDamage,       // 방어도 무시 데미지
    ConditionalDraw,         // 조건부 드로우
    CopyEnemyArmor,          // 적 방어도 복사
    DamageReduction,         // 피해 감소 버프
    DelayedDamage,           // 지연 데미지
    DiscardRandomCard,       // 무작위 카드 버리기
    DoubleNextAttack,        // 다음 공격 2배
    EqualizeHealth,          // 체력 비율 맞추기
    GainCardNextTurn,        // 다음 턴 카드 획득
    GainRandomSkill,         // 무작위 스킬 획득 (0코스트)
    Immobilize,              // 이동 불가 부여
    MoveBonus,               // 이동 거리 보너스
    Move,                    // 이동
    PerfectBlockBonus,       // 퍼펙트 블록 보너스
    RecoverAllAndFillHand,   // 소멸 카드 복구 및 패 채우기
    RecoverExhausted,        // 소멸 카드 복구
    ReduceCost,              // 비용 감소
    RetainHand,              // 패 보존
    Revenge,                 // 복수 (피격 시 데미지 증가)
    GainStrength,           // 플레이어 힘 획득
    GainDexterity,          // 플레이어 민첩 획득
    ApplyStrengthToEnemy,   // 적에게 힘 부여 (적 공격력 증가)
    Stun,                    // 기절
    CurseNoDraw,             // 손패에 있으면 드로우 불가
    CurseDamageByHand,       // 손패당 피해
    ApplyDullnessToEnemy,    // 적에게 무뎌짐 부여효과
    ApplyExposedToEnemy,     // 적에게 노출 부여 효과
    DemonsMuscle,            // 악마의 근육
    DemonsDust,              // 악마의 티끌
    CurseMadness,            // 손패에 있으면 다른 카드 비용이 1~3이 됨

    // ★ [더블 래리어트 연타 구현용 신규 이펙트]
    RandomXHitsDamage        // 남은 에너지를 모두 소모해 랜덤 적에게 X번 피해를 줌
}

/// <summary>
/// 인스펙터에서 설정할 카드 효과 데이터입니다.
/// </summary>
[System.Serializable]
public class CardEffectInfo
{
    [Header("효과 종류")]
    public CardEffectType effectType;

    [Header("기본 수치 (데미지, 방어도, 횟수 등)")]
    public int amount;

    [Header("추가 설정")]
    public int duration;          // 지속 턴 수
    public float floatValue;      // 소수점 수치 (감소율 등)
    public Card effectCard;       // 특정 카드 참조
    public List<Card> cardPool;   // 카드 랜덤 풀
    public bool applyToAll = false; // 전체 적용 여부

    [Header("효과 대상")]
    public Card.TargetType targetType;

    [Tooltip("이 효과를 몇 번 반복할 것인지 (연타 수)")]
    public int hitCount = 1;
}

/// <summary>
/// 카드 에셋. 이동/공격/유틸리티 세 종류로 나뉘며,
/// 공격 카드는 유효 사거리(minRange ~ maxRange) 안에서만 발동된다.
/// </summary>
[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class Card : ScriptableObject
{
    [SerializeField] private string resourcePath;

    [Header("카드 기본 정보")]
    public string cardName;
    [TextArea]
    public string cardDescription; // {damage}, {armor}, {move} 등 플레이스홀더 지원
    public CardType cardType;
    public Sprite cardSprite;
    public int energyCost;
    public CardRarity cardRarity;
    public bool exhaustAfterUse = false;

    [Header("이동 설정 (Movement 카드 전용)")]
    [Tooltip("양수 = 후퇴(거리 증가), 음수 = 전진(거리 감소).")]
    public int moveAmount = 0;

    [Header("사거리 설정 (Attack 카드 전용)")]
    [Tooltip("이 카드가 발동 가능한 최소 거리.")]
    public int minRange = 1;
    [Tooltip("이 카드가 발동 가능한 최대 거리.")]
    public int maxRange = 3;

    [Header("카드 이펙트")]
    public List<CardEffectInfo> effects = new List<CardEffectInfo>();

    [Header("카드 타겟팅")]
    public TargetType targetType;

    [Header("획득 경로 필터링")]
    [Tooltip("체크 시 상점, 클리어 보상, 이벤트 랜덤 선택 풀 등 무작위 카드 획득 풀에서 완전히 제외됩니다.")]
    public bool isEventOnly = false;

    [Header("이벤트 영구 조작 상태")]
    public bool isInnate = false;           // 선천성 여부 (전투 시작 시 손패로 고정)
    public bool isSnecko = false;           // 스네코 여부 (드로우 시 코스트 랜덤화)

    [Tooltip("데미지 배율 (기본 1) - 이벤트 효과로 증가할 수 있습니다.")]
    public int bonusDamageMultiplier = 1;   // 데미지 배율

    [Tooltip("방어도 배율 (기본 1) - 이벤트 효과로 증가할 수 있습니다.")]
    public int bonusBlockMultiplier = 1;    // 방어도 배율

    /// <summary>이펙트 목록을 반환한다 (null 안전).</summary>
    public List<CardEffectInfo> GetCardEffects() => effects ?? new List<CardEffectInfo>();

    public string GetResourcePath() => resourcePath;

    /// <summary>현재 거리에서 이 카드를 사용할 수 있는지 확인한다.</summary>
    public bool IsUsableAtCurrentDistance()
    {
        if (cardType != CardType.Attack) return true;
        if (DistanceManager.Instance == null) return true;
        return DistanceManager.Instance.IsInRange(minRange, maxRange);
    }

    /// <summary>
    /// 카드 배율이 적용된 특정 효과 타입의 실 수치를 연산하여 반환합니다.
    /// </summary>
    public int GetCalculatedAmount(CardEffectInfo info)
    {
        if (info == null) return 0;
        if (info.effectType == CardEffectType.Damage ||
            info.effectType == CardEffectType.AOEDamage ||
            info.effectType == CardEffectType.RandomXHitsDamage ||
            info.effectType == CardEffectType.AntiArmorDamage || 
            info.effectType == CardEffectType.IgnoreArmorDamage) 
        {
            return info.amount * bonusDamageMultiplier;
        }
        else if (info.effectType == CardEffectType.Armor)
        {
            return info.amount * bonusBlockMultiplier;
        }
        return info.amount;
    }

    public enum CardType { Attack, Skill, Curse }
    public enum CardRarity { Common, Uncommon, Rare }
    public enum TargetType { SingleEnemy, AllEnemies, Self, None }
}