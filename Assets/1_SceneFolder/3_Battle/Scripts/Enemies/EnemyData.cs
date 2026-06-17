using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 한 명의 스탯, 비주얼, AI 타입, 인텐트 아이콘을 담는 ScriptableObject.
/// Project 창에서 우클릭 > Enemy Data 로 생성한다.
/// </summary>
[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("적 기본 정보")]
    public string enemyName;    // 적 이름
    public int health;          // 최대 체력

    [Header("비주얼")]
    public Sprite enemySprite;  // 적 스프라이트
    public Vector2 position;    // EnemyCanvas 내 배치 위치
    public Vector2 size;        // 적 UI 크기

    [Header("AI 행동 타입")]
    public EnemyAIType enemyAIType; // 어떤 AI를 사용할지

    [Header("인텐트 아이콘")]
    public Sprite attackIntentIcon; // 공격 인텐트 아이콘
    public Sprite buffIntentIcon;   // 버프 인텐트 아이콘
    public Sprite healIntentIcon;   // 힐 인텐트 아이콘
    public Sprite awakenIntentIcon; // 각성 인텐트 아이콘

    [Header("보스 소환 설정 (선택)")]
    public EnemyData summonLeftData;  // 소환할 왼쪽 미니언 데이터
    public EnemyData summonRightData; // 소환할 오른쪽 미니언 데이터

    [Header("행동 패턴")]
    public List<EnemyActionData> actionPattern = new();

    [Header("그림자 설정")]
    public ShadowMode shadowMode = ShadowMode.Auto;
    [Range(0.1f, 2f)] public float shadowWidthMultiplier = 0.75f;
    [Range(0.05f, 0.6f)] public float shadowHeightToWidth = 0.20f;
    public Vector2 shadowOffset = new Vector2(0f, -10f);
    public Vector2 manualShadowSize = new Vector2(180f, 28f);

    [Header("보스 전용 설정 (EnemyAIType.Boss일 때만 사용)")]
    public Card demonsMuscleCard;       // 악마의 근육 Card SO
    public Card demonsDustCard;         // 악마의 티끌 Card SO

    [Tooltip("악마의 티끌 카드 1장당 보스 공격력 증가량")]
    public int dustBonusPerCard = 3;

    [Header("스탯")]
    public int initialStrength = 0; // 초기 힘


}

/// <summary>적 AI 타입 열거형. 새 적 추가 시 여기에 항목을 추가한다.</summary>
public enum EnemyAIType
{
    None,
    Normal, // 기본 몹
    Boss,   // 보스 몹
}

/// <summary>그림자 크기 계산 방식.</summary>
public enum ShadowMode { None, Auto, Manual }