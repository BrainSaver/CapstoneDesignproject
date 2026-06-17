using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 사거리 검증 기능 및 머리 위 연타/유효 사거리 실시간 예고 피드백 기능이 탑재된 기본 적 AI.
/// </summary>
public class BasicEnemyAI : MonoBehaviour, IEnemyAI
{
    private Enemy self;
    private CharacterStats player;
    private EnemyDisplay display;
    private EnemyIntent nextIntent;

    private Sprite attackIcon;
    private Sprite buffIcon;

    [Header("행동 패턴 (순서대로 반복)")]
    [SerializeField] private List<EnemyActionData> actionPattern = new();

    private int _patternIndex = 0;

    private void Awake()
    {
        self = GetComponent<Enemy>();
    }

    public void SetPlayerStats(CharacterStats playerStats) => player = playerStats;
    public void SetEnemyDisplay(EnemyDisplay enemyDisplay) => display = enemyDisplay;

    public void SetIntentIcons(Sprite attack, Sprite buff)
    {
        attackIcon = attack;
        buffIcon = buff;
    }

    public void InitializeAI()
    {
        self.strength = self.Data.initialStrength;
        self.enemyDisplay?.UpdateStrengthDisplay(self.TotalStrength); // ✅ TotalStrength 사용

        if (self?.Data != null)
        {
            if (attackIcon == null) attackIcon = self.Data.attackIntentIcon;
            if (buffIcon == null) buffIcon = self.Data.buffIntentIcon;

            Debug.Log($"[BasicEnemyAI] Data={self.Data.enemyName}, actionPattern Count={self.Data.actionPattern?.Count ?? -1}");

            if (self.Data.actionPattern != null && self.Data.actionPattern.Count > 0)
                actionPattern = self.Data.actionPattern;
        }
        else
        {
            Debug.LogError($"[BasicEnemyAI] self.Data가 null입니다!");
        }

        _patternIndex = 0;
        PredictNextIntent();
        display?.SetIntent(nextIntent);
    }

    public void ExecuteTurn()
    {
        if (BattleManager.Instance.IsBattleOver()) return;

        if (self.IsStunned)
        {
            Logger.Log($"[BasicEnemyAI] {self.enemyName} 기절 — 행동 건너뜀.");
            self.TickStun();
            PredictNextIntent();
            display?.SetIntent(nextIntent);
            return;
        }

        if (actionPattern == null || actionPattern.Count == 0)
        {
            Logger.LogWarning($"[BasicEnemyAI] {self.enemyName} 행동 패턴이 없습니다.");
            return;
        }

        // 1. 현재 메인 행동 실행
        var mainAction = actionPattern[_patternIndex];
        ExecuteSingleAction(mainAction);

        // 2. 같은 턴에 묶여있는 모든 추가 행동 실행
        if (mainAction.additionalActions != null && mainAction.additionalActions.Count > 0)
        {
            foreach (var addAction in mainAction.additionalActions)
            {
                if (addAction == null) continue;

                EnemyActionData dummyData = new EnemyActionData();
                dummyData.actionType = addAction.actionType;
                dummyData.value = addAction.value;
                dummyData.repeatCount = addAction.repeatCount;
                dummyData.minRange = addAction.minRange;
                dummyData.maxRange = addAction.maxRange;

                ExecuteSingleAction(dummyData);
            }
        }

        // 다음 패턴으로
        _patternIndex = (_patternIndex + 1) % actionPattern.Count;

        self.TickStun();
        PredictNextIntent();
        display?.SetIntent(nextIntent);
    }

    /// <summary>행동 데이터를 기반으로 사거리를 체크하여 실행합니다.</summary>
    private void ExecuteSingleAction(EnemyActionData actionData)
    {
        if (actionData == null) return;

        EnemyActionType type = actionData.actionType;
        int baseValue = actionData.value;
        int loops = Mathf.Max(1, actionData.repeatCount);

        // 공격 행동일 때만 현재 플레이어와의 거리를 체크합니다.
        if (type == EnemyActionType.Attack && DistanceManager.Instance != null)
        {
            // 현재 거리가 지정된 사거리를 벗어났다면 공격 무효화 처리!
            if (!DistanceManager.Instance.IsInRange(actionData.minRange, actionData.maxRange))
            {
                Logger.Log($"[사거리 미달/초과] {self.enemyName}의 공격이 사거리를 벗어났습니다! " +
                           $"(현재 거리: {DistanceManager.Instance.CurrentDistance}, 요구 사거리: {actionData.minRange}~{actionData.maxRange})");
                return;
            }
        }

        for (int i = 0; i < loops; i++)
        {
            switch (type)
            {
                case EnemyActionType.Attack:
                    // ✅ GetWeakenedDamage()가 TotalStrength 포함
                    int damage = self.GetWeakenedDamage(baseValue);
                    if (player != null)
                    {
                        player.TakeDamage(damage);
                        Logger.Log($"[BasicEnemyAI] {self.enemyName} 공격 ({i + 1}/{loops}): {damage} 데미지");
                    }
                    break;

                case EnemyActionType.Defend:
                    self.AddArmor(baseValue);
                    Logger.Log($"[BasicEnemyAI] {self.enemyName} 방어도 획득: +{baseValue}");
                    break;

                case EnemyActionType.ApplyDullness:
                    DullnessTracker.Apply(baseValue);
                    Logger.Log($"[BasicEnemyAI] {self.enemyName} → 플레이어 무뎌짐 {baseValue}턴 부여");
                    break;

                case EnemyActionType.ApplyExposed:
                    ExposedTracker.Apply(baseValue);
                    Logger.Log($"[BasicEnemyAI] {self.enemyName} → 플레이어 노출 {baseValue}턴 부여");
                    break;

                case EnemyActionType.BloodChant:
                    Logger.Log($"[BasicEnemyAI] {self.enemyName} 피의 영창 발동");
                    break;

                case EnemyActionType.DistributeDust:
                    Logger.Log($"[BasicEnemyAI] {self.enemyName} 악마의 티끌 배포");
                    break;

                case EnemyActionType.DistributeMuscle:
                    Logger.Log($"[BasicEnemyAI] {self.enemyName} 악마의 근육 배포");
                    break;
            }
        }
    }

    /// <summary>다음 인텐트를 예측합니다.</summary>
    public EnemyIntent PredictNextIntent()
    {
        if (self.IsStunned)
        {
            nextIntent = new EnemyIntent(IntentType.Special, "기절", 0, buffIcon);
            return nextIntent;
        }

        if (actionPattern == null || actionPattern.Count == 0)
            return nextIntent;

        var currentPattern = actionPattern[_patternIndex];

        // 1. 메인 행동 텍스트 정산
        string combinedDesc = GetActionDescription(currentPattern, out IntentType primaryType, out int primaryValue);

        // 2. 추가 행동 결합 및 정산
        if (currentPattern.additionalActions != null && currentPattern.additionalActions.Count > 0)
        {
            foreach (var addAction in currentPattern.additionalActions)
            {
                if (addAction == null) continue;

                EnemyActionData dummy = new EnemyActionData();
                dummy.actionType = addAction.actionType;
                dummy.value = addAction.value;
                dummy.minRange = addAction.minRange;
                dummy.maxRange = addAction.maxRange;

                string addDesc = GetActionDescription(dummy, out _, out int addValue);
                combinedDesc += $" & {addDesc}";

                if (addAction.actionType == EnemyActionType.Attack)
                {
                    primaryType = IntentType.Attack;
                    if (DistanceManager.Instance != null &&
                        DistanceManager.Instance.IsInRange(addAction.minRange, addAction.maxRange))
                        primaryValue += addValue;
                }
            }
        }

        Sprite finalIcon = (primaryType == IntentType.Attack) ? attackIcon : buffIcon;
        nextIntent = new EnemyIntent(primaryType, combinedDesc, primaryValue, finalIcon);
        return nextIntent;
    }

    /// <summary>사거리를 비교하여 연타, 유효 범위 텍스트 등을 조립합니다.</summary>
    private string GetActionDescription(EnemyActionData actionData, out IntentType intentType, out int calculatedValue)
    {
        calculatedValue = actionData.value;
        intentType = IntentType.Buff;

        switch (actionData.actionType)
        {
            case EnemyActionType.Attack:
                intentType = IntentType.Attack;

                // ✅ TotalStrength 포함된 GetWeakenedDamage 사용
                calculatedValue = self.GetWeakenedDamage(calculatedValue);

                // 플레이어 노출 적용
                if (ExposedTracker.IsActive)
                    calculatedValue = Mathf.RoundToInt(calculatedValue * 1.5f);

                // 사거리 밖 처리
                if (DistanceManager.Instance != null &&
                    !DistanceManager.Instance.IsInRange(actionData.minRange, actionData.maxRange))
                {
                    calculatedValue = 0;
                    return "사거리 밖";
                }

                // 사거리 문자열 조립
                string rangeInfo = (actionData.minRange == actionData.maxRange)
                    ? $"{actionData.minRange}"
                    : $"{actionData.minRange}-{actionData.maxRange}";

                // ✅ TotalStrength 기준으로 표시 (보스 보너스 포함)
                int totalStr = self.TotalStrength;
                string strengthInfo = totalStr != 0
                    ? $" (힘 {(totalStr > 0 ? "+" : "")}{totalStr})"
                    : "";

                // 연타 횟수 표기
                if (actionData.repeatCount > 1)
                    return $"{calculatedValue}{strengthInfo} × {actionData.repeatCount} ({rangeInfo})";

                return $"{calculatedValue}{strengthInfo} ({rangeInfo})";

            case EnemyActionType.Defend:
                return $"방어 +{actionData.value}";

            case EnemyActionType.ApplyDullness:
                intentType = IntentType.Special;
                return $"무뎌짐 {actionData.value}턴";

            case EnemyActionType.ApplyExposed:
                intentType = IntentType.Special;
                return $"노출 {actionData.value}턴";

            case EnemyActionType.GainStrength:
                intentType = IntentType.Buff;
                return $"힘 +{actionData.value}";

            default:
                return actionData.value.ToString();
        }
    }

    public EnemyIntent GetCurrentIntent() => nextIntent;
}

// ── 행동 데이터 구조 세팅 ──────────────────────────────────────────────

[System.Serializable]
public class EnemyActionData
{
    public EnemyActionType actionType;
    public int value;
    public int repeatCount = 1;

    [Header("사거리 제약 설정 (공격용)")]
    public int minRange = 1;
    public int maxRange = 2;

    [Tooltip("같은 턴에 추가로 실행할 행동 목록")]
    public List<EnemyAdditionalAction> additionalActions = new();
}

[System.Serializable]
public class EnemyAdditionalAction
{
    public EnemyActionType actionType;
    public int value;
    public int repeatCount = 1;

    [Header("사거리 제약 설정 (공격용)")]
    public int minRange = 1;
    public int maxRange = 2;
}

// ── 열거형 정의 ──────────────────────────────────────────────
public enum EnemyActionType
{
    Attack,          // 공격
    Defend,          // 방어도 획득
    ApplyDullness,   // 플레이어 무뎌짐
    ApplyExposed,    // 플레이어 노출
    BloodChant,      // 피의 영창
    DistributeDust,  // 악마의 티끌 배포
    DistributeMuscle, // 악마의 근육 배포

    // ★ [보스 전용 행동 유형 신규 추가]
    GainStrength,     // 보스 힘(공격력) 증가
    ManaAbsorb
}