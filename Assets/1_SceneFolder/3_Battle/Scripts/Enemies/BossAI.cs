using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 전용 AI.
/// BasicEnemyAI를 확장하여 저주카드 배포, 피의 영창 등 특수 패턴을 처리한다.
/// </summary>
public class BossAI : MonoBehaviour, IEnemyAI
{
    private Enemy self;
    private CharacterStats player;
    private EnemyDisplay display;
    private EnemyIntent nextIntent;

    private Sprite attackIcon;
    private Sprite buffIcon;
    private Sprite specialIcon;

    [Header("행동 패턴 (순서대로 반복)")]
    [SerializeField] private List<EnemyActionData> actionPattern = new();

    [Header("보스 특수 패턴 설정")]
    [Tooltip("피의 영창을 발동할 패턴 인덱스 목록 (0부터 시작)")]
    [SerializeField] private List<int> bloodChantTurnIndices = new();

    [Tooltip("악마의 근육 카드 (저주카드 배포용)")]
    [SerializeField] private Card demonsMuscleCard;

    [Tooltip("악마의 티끌 카드 (저주카드 배포용)")]
    [SerializeField] private Card demonsDustCard;

    [Tooltip("악마의 티끌 카드 1장당 보스 공격력 증가량")]
    [SerializeField] private int dustBonusPerCard = 3;

    [Tooltip("악마의 티끌을 배포할 패턴 인덱스 목록")]
    [SerializeField] private List<int> dustDistributeTurnIndices = new();

    [Tooltip("악마의 근육을 배포할 패턴 인덱스 목록")]
    [SerializeField] private List<int> muscleDistributeTurnIndices = new();

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
        specialIcon = buff;
    }

    public void InitializeAI()
    {
        if (self?.Data != null)
        {
            if (attackIcon == null) attackIcon = self.Data.attackIntentIcon;
            if (buffIcon == null) buffIcon = self.Data.buffIntentIcon;
            if (specialIcon == null) specialIcon = self.Data.awakenIntentIcon ?? self.Data.buffIntentIcon;

            if (self.Data.actionPattern != null && self.Data.actionPattern.Count > 0)
                actionPattern = self.Data.actionPattern;

            demonsMuscleCard = self.Data.demonsMuscleCard;
            demonsDustCard = self.Data.demonsDustCard;
            dustBonusPerCard = self.Data.dustBonusPerCard;

            // 초기 힘 설정
            self.strength = self.Data.initialStrength;
        }

        DemonsDustTracker.BonusPerCard = dustBonusPerCard;
        BossStrengthTracker.Reset();
        DemonsDustTracker.Reset();
        BloodChantTracker.Reset();

        // ✅ 초기 힘 UI 갱신
        self.enemyDisplay?.UpdateStrengthDisplay(self.TotalStrength);

        // ✅ 중복 구독 방지
        BossStrengthTracker.OnBonusChanged -= RefreshBossStrengthUI;
        BossStrengthTracker.OnBonusChanged += RefreshBossStrengthUI;
        HandManager.OnHandChanged -= RefreshManaAbsorbIntent;
        HandManager.OnHandChanged += RefreshManaAbsorbIntent;

        _patternIndex = 0;
        PredictNextIntent();
        display?.SetIntent(nextIntent);
    }

    private void OnDestroy()
    {
        BossStrengthTracker.OnBonusChanged -= RefreshBossStrengthUI;
        HandManager.OnHandChanged -= RefreshManaAbsorbIntent;
    }

    /// <summary>보스 공격력 변경 시 인텐트 + 힘 UI 갱신.</summary>
    private void RefreshBossStrengthUI()
    {
        // ✅ TotalStrength 사용
        self.enemyDisplay?.UpdateStrengthDisplay(self.TotalStrength);
        PredictNextIntent();
        display?.SetIntent(nextIntent);
    }

    /// <summary>손패 변경 시 마력흡수 인텐트 갱신.</summary>
    private void RefreshManaAbsorbIntent()
    {
        if (actionPattern == null || actionPattern.Count == 0) return;

        var current = actionPattern[_patternIndex];
        if (current.actionType == EnemyActionType.ManaAbsorb)
        {
            PredictNextIntent();
            display?.SetIntent(nextIntent);
            Logger.Log($"[BossAI] 마력흡수 인텐트 갱신: {nextIntent?.Description}");
            return;
        }

        if (current.additionalActions != null)
        {
            foreach (var add in current.additionalActions)
            {
                if (add.actionType == EnemyActionType.ManaAbsorb)
                {
                    PredictNextIntent();
                    display?.SetIntent(nextIntent);
                    Logger.Log($"[BossAI] 마력흡수(추가행동) 인텐트 갱신: {nextIntent?.Description}");
                    return;
                }
            }
        }
    }

    public void ExecuteTurn()
    {
        if (BattleManager.Instance.IsBattleOver()) return;

        if (self.IsStunned)
        {
            Logger.Log($"[BossAI] {self.enemyName} 기절 — 행동 건너뜀.");
            self.TickStun();
            PredictNextIntent();
            display?.SetIntent(nextIntent);
            return;
        }

        if (actionPattern == null || actionPattern.Count == 0) return;

        var action = actionPattern[_patternIndex];

        int repeat = Mathf.Max(1, action.repeatCount);
        for (int i = 0; i < repeat; i++)
        {
            if (BattleManager.Instance.IsBattleOver()) break;
            ExecuteAction(action);
        }

        if (action.additionalActions != null)
        {
            foreach (var additional in action.additionalActions)
            {
                if (BattleManager.Instance.IsBattleOver()) break;
                int addRepeat = Mathf.Max(1, additional.repeatCount);
                for (int i = 0; i < addRepeat; i++)
                {
                    if (BattleManager.Instance.IsBattleOver()) break;
                    ExecuteAdditionalAction(additional);
                }
            }
        }

        _patternIndex = (_patternIndex + 1) % actionPattern.Count;

        self.TickStun();
        PredictNextIntent();
        display?.SetIntent(nextIntent);
    }

    /// <summary>피의 영창을 발동한다.</summary>
    private void ActivateBloodChant()
    {
        BloodChantTracker.Activate();
        Logger.Log($"[BossAI] {self.enemyName} 피의 영창 발동!");
    }

    /// <summary>저주카드를 보존 상태로 플레이어 손패에 배포한다.</summary>
    private void DistributeCurseCard(Card card, string cardName)
    {
        if (card == null)
        {
            Logger.LogWarning($"[BossAI] {cardName} 카드가 연결되지 않았습니다.");
            return;
        }

        if (card == demonsDustCard)
            HandManager.Instance?.AddCardToHandDirectlyWithRetain(card);
        else
            HandManager.Instance?.AddCardToHandDirectly(card);

        Logger.Log($"[BossAI] {self.enemyName} → 플레이어에게 {cardName} 배포.");
    }

    /// <summary>행동 데이터를 실행한다.</summary>
    private void ExecuteAction(EnemyActionData action)
    {
        switch (action.actionType)
        {
            case EnemyActionType.Attack:
                // ✅ TotalStrength 포함된 GetWeakenedDamage 사용
                int finalDamage = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                if (player != null)
                {
                    player.TakeDamage(finalDamage);
                    Logger.Log($"[BossAI] {self.enemyName} 공격: {finalDamage}");
                }
                break;

            case EnemyActionType.GainStrength:
                self.AddStrength(action.value);
                Logger.Log($"[BossAI] {self.enemyName} 힘 +{action.value}. 총 힘: {self.TotalStrength}");
                break;

            case EnemyActionType.Defend:
                self.AddArmor(action.value);
                Logger.Log($"[BossAI] {self.enemyName} 방어도: +{action.value}");
                break;

            case EnemyActionType.BloodChant:
                ActivateBloodChant();
                if (action.value > 0)
                {
                    int dmg = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                    player?.TakeDamage(dmg);
                }
                break;

            case EnemyActionType.DistributeDust:
                DistributeCurseCard(demonsDustCard, "악마의 티끌");
                if (action.value > 0)
                {
                    int dmg = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                    player?.TakeDamage(dmg);
                }
                break;

            case EnemyActionType.DistributeMuscle:
                DistributeCurseCard(demonsMuscleCard, "악마의 근육");
                if (action.value > 0)
                {
                    int dmg = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                    player?.TakeDamage(dmg);
                }
                break;

            case EnemyActionType.ManaAbsorb:
                ExecuteManaAbsorb();
                break;

            case EnemyActionType.ApplyDullness:
                DullnessTracker.Apply(action.value);
                Logger.Log($"[BossAI] {self.enemyName} → 플레이어 무뎌짐 {action.value}턴");
                break;

            case EnemyActionType.ApplyExposed:
                ExposedTracker.Apply(action.value);
                Logger.Log($"[BossAI] {self.enemyName} → 플레이어 노출 {action.value}턴");
                break;
        }
    }

    /// <summary>추가 행동을 실행한다.</summary>
    private void ExecuteAdditionalAction(EnemyAdditionalAction action)
    {
        switch (action.actionType)
        {
            case EnemyActionType.Attack:
                // ✅ TotalStrength 포함된 GetWeakenedDamage 사용 (중복 제거)
                int finalDamage = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                if (player != null)
                {
                    player.TakeDamage(finalDamage);
                    Logger.Log($"[BossAI] {self.enemyName} 추가 공격: {finalDamage}");
                }
                break;

            case EnemyActionType.Defend:
                self.AddArmor(action.value);
                Logger.Log($"[BossAI] {self.enemyName} 추가 방어도: +{action.value}");
                break;

            case EnemyActionType.BloodChant:
                ActivateBloodChant();
                if (action.value > 0)
                {
                    int dmg = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                    player?.TakeDamage(dmg);
                }
                break;

            case EnemyActionType.DistributeDust:
                DistributeCurseCard(demonsDustCard, "악마의 티끌");
                if (action.value > 0)
                {
                    int dmg = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                    player?.TakeDamage(dmg);
                }
                break;

            case EnemyActionType.DistributeMuscle:
                DistributeCurseCard(demonsMuscleCard, "악마의 근육");
                if (action.value > 0)
                {
                    int dmg = Mathf.Max(0, self.GetWeakenedDamage(action.value));
                    player?.TakeDamage(dmg);
                }
                break;

            case EnemyActionType.ManaAbsorb:
                ExecuteManaAbsorb();
                break;

            case EnemyActionType.ApplyDullness:
                DullnessTracker.Apply(action.value);
                Logger.Log($"[BossAI] {self.enemyName} → 플레이어 무뎌짐 {action.value}턴");
                break;

            case EnemyActionType.ApplyExposed:
                ExposedTracker.Apply(action.value);
                Logger.Log($"[BossAI] {self.enemyName} → 플레이어 노출 {action.value}턴");
                break;

            case EnemyActionType.GainStrength:
                self.AddStrength(action.value);
                Logger.Log($"[BossAI] {self.enemyName} 힘 +{action.value}. 총 힘: {self.TotalStrength}");
                break;
        }
    }

    /// <summary>마력흡수: 손패의 악마의 티끌 장수 x 12만큼 보스 공격력 증가.</summary>
    private void ExecuteManaAbsorb()
    {
        if (HandManager.Instance == null || demonsDustCard == null) return;

        int dustCount = 0;
        foreach (var cardObj in HandManager.Instance.CardsInHand)
        {
            if (cardObj == null) continue;
            var display = cardObj.GetComponent<CardDisplay>();
            if (display?.cardData == demonsDustCard)
                dustCount++;
        }

        if (dustCount > 0)
        {
            int bonus = dustCount * 12;
            BossStrengthTracker.AddBonus(bonus);
            Logger.Log($"[BossAI] 마력흡수 발동! 티끌 {dustCount}장 x 12 = 보스 공격력 +{bonus}");
            // ✅ RefreshBossStrengthUI가 OnBonusChanged로 자동 호출됨
        }
        else
        {
            Logger.Log("[BossAI] 마력흡수 발동! 손패에 악마의 티끌 없음.");
        }
    }

    /// <summary>마력흡수 인텐트 설명 생성.</summary>
    private string GetManaAbsorbDesc()
    {
        if (HandManager.Instance == null || demonsDustCard == null)
            return "마력흡수";

        int dustCount = 0;
        foreach (var cardObj in HandManager.Instance.CardsInHand)
        {
            if (cardObj == null) continue;
            var display = cardObj.GetComponent<CardDisplay>();
            if (display?.cardData == demonsDustCard)
                dustCount++;
        }

        return dustCount > 0
            ? $"마력흡수 [티끌 {dustCount}장 → +{dustCount * 12}]"
            : "마력흡수";
    }

    public EnemyIntent PredictNextIntent()
    {
        if (self.IsStunned)
        {
            nextIntent = new EnemyIntent(IntentType.Special, "기절", 0, buffIcon);
            return nextIntent;
        }

        if (actionPattern == null || actionPattern.Count == 0)
            return nextIntent;

        var next = actionPattern[_patternIndex];

        // ✅ TotalStrength 사용 (self.strength + BossStrengthTracker.CurrentBonus)
        int displayValue = next.actionType == EnemyActionType.Attack
            ? Mathf.Max(0, self.GetWeakenedDamage(next.value))
            : next.value;

        if (next.actionType == EnemyActionType.Attack || next.actionType == EnemyActionType.BloodChant)
        {
            if (ExposedTracker.IsActive)
                displayValue = Mathf.RoundToInt(displayValue * 1.5f);
        }

        int repeat = Mathf.Max(1, next.repeatCount);
        string attackDesc = next.actionType == EnemyActionType.Attack && repeat > 1
            ? $"{displayValue} x{repeat}"
            : displayValue.ToString();

        string additionalDesc = "";
        if (next.additionalActions != null && next.additionalActions.Count > 0)
        {
            foreach (var add in next.additionalActions)
            {
                // ✅ TotalStrength 포함된 GetWeakenedDamage 사용
                int addValue = add.actionType == EnemyActionType.Attack
                    ? Mathf.Max(0, self.GetWeakenedDamage(add.value))
                    : add.value;

                if (add.actionType == EnemyActionType.Attack && ExposedTracker.IsActive)
                    addValue = Mathf.RoundToInt(addValue * 1.5f);

                int addRepeat = Mathf.Max(1, add.repeatCount);
                string addAttackDesc = add.actionType == EnemyActionType.Attack && addRepeat > 1
                    ? $"{addValue} x{addRepeat}"
                    : addValue.ToString();

                additionalDesc += add.actionType switch
                {
                    EnemyActionType.Attack => $" + 공격 {addAttackDesc}",
                    EnemyActionType.Defend => $" + 방어 +{add.value}",
                    EnemyActionType.BloodChant => $" + 피의 영창 [{addValue}]",
                    EnemyActionType.DistributeDust => " + 티끌 배포",
                    EnemyActionType.DistributeMuscle => " + 근육 배포",
                    EnemyActionType.ApplyDullness => $" + 무뎌짐 {add.value}턴",
                    EnemyActionType.ApplyExposed => $" + 노출 {add.value}턴",
                    EnemyActionType.GainStrength => $" + 힘 +{add.value}",
                    EnemyActionType.ManaAbsorb => " + 마력흡수",
                    _ => ""
                };
            }
        }

        nextIntent = next.actionType switch
        {
            EnemyActionType.Attack => new EnemyIntent(
                IntentType.Attack,
                $"공격 {attackDesc}{additionalDesc}",
                displayValue * repeat,
                attackIcon),

            EnemyActionType.Defend => new EnemyIntent(
                IntentType.Buff,
                $"방어 +{next.value}{additionalDesc}",
                next.value,
                buffIcon),

            EnemyActionType.BloodChant => new EnemyIntent(
                IntentType.Special,
                $"피의 영창 [{displayValue}]{(repeat > 1 ? $" x{repeat}" : "")}{additionalDesc}",
                displayValue,
                specialIcon),

            EnemyActionType.DistributeDust => new EnemyIntent(
                IntentType.Special,
                $"티끌 배포{additionalDesc}",
                0,
                specialIcon),

            EnemyActionType.DistributeMuscle => new EnemyIntent(
                IntentType.Special,
                $"근육 배포{additionalDesc}",
                0,
                specialIcon),

            EnemyActionType.ApplyDullness => new EnemyIntent(
                IntentType.Special,
                $"무뎌짐 {next.value}턴{additionalDesc}",
                next.value,
                specialIcon),

            EnemyActionType.ApplyExposed => new EnemyIntent(
                IntentType.Special,
                $"노출 {next.value}턴{additionalDesc}",
                next.value,
                specialIcon),

            EnemyActionType.GainStrength => new EnemyIntent(
                IntentType.Buff,
                $"힘 +{next.value}{additionalDesc}",
                next.value,
                buffIcon),

            EnemyActionType.ManaAbsorb => new EnemyIntent(
                IntentType.Special,
                GetManaAbsorbDesc(),
                0,
                specialIcon),

            _ => new EnemyIntent(IntentType.Special, "???", 0, specialIcon)
        };

        return nextIntent;
    }

    public EnemyIntent GetCurrentIntent() => nextIntent;
}