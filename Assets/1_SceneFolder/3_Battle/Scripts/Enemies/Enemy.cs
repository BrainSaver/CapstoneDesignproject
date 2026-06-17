using UnityEngine;
using System.Collections;

/// <summary>
/// 적 캐릭터의 스탯, AI 연결, 데미지 수신, 사망 처리를 담당한다.
/// HP는 자체 관리, 방어도는 CharacterStats에서 관리한다.
/// </summary>
public class Enemy : CharacterStats
{
    public string enemyName;        // 적 이름
    public bool IsEnraged = false;  // 과격화 상태 여부

    private int stunDuration = 0;   // 기절 남은 턴 수
    public int currentBleedStacks = 0;

    // ── 무뎌짐 / 노출 ────────────────────────────────────────────
    private int dullnessDuration = 0; // 무뎌짐 남은 턴
    private int exposedDuration = 0;  // 노출 남은 턴

    /// <summary>무뎌짐 상태인지 여부.</summary>
    public bool IsDullness => dullnessDuration > 0;

    /// <summary>노출 상태인지 여부.</summary>
    public bool IsExposed => exposedDuration > 0;

    public EnemyDisplay enemyDisplay { get; private set; }
    public IEnemyAI EnemyAI { get; private set; }
    public EnemyData Data { get; private set; }
    public EnemyAIType AiType { get; private set; }

    [Header("HP")]
    public int maxHP;
    public int currentHP;

    [Header("힘")]
    public int strength = 0; // 힘: 공격력 +N

    [Header("적 데이터 (씬에 미리 설정)")]
    [SerializeField] private EnemyData enemyDataPreset; // Inspector에서 연결

    // ── 초기화 ───────────────────────────────────────────────────

    /// <summary>실제 적용되는 총 힘 (자체 힘 + 보스 보너스).</summary>
    public int TotalStrength
    {
        get
        {
            // 보스 AI가 붙어있으면 BossStrengthTracker 보너스도 합산
            if (GetComponent<BossAI>() != null)
                return strength + BossStrengthTracker.CurrentBonus;
            return strength;
        }
    }

    /// <summary>최대 HP를 변경한다.</summary>
    public void SetMaxHP(int newMaxHP)
    {
        maxHP = Mathf.Max(1, newMaxHP);
        currentHP = Mathf.Min(currentHP, maxHP);
        enemyDisplay?.UpdateDisplay(currentHP, maxHP);
    }

    private void Awake()
    {
        // 씬 배치 시 미리 설정된 데이터 로드
        if (enemyDataPreset != null)
            Data = enemyDataPreset;
    }

    // ── 상태이상 적용 ────────────────────────────────────────────

    /// <summary>무뎌짐을 적용한다. 공격력 25% 감소.</summary>
    public void ApplyDullness(int duration)
    {
        dullnessDuration = Mathf.Max(dullnessDuration, duration);
        enemyDisplay?.UpdateDullnessDisplay(dullnessDuration);
        UpdateIntentDisplay(); // ✅ 인텐트 수치 즉시 갱신
        Logger.Log($"[Enemy] {enemyName} 무뎌짐 {dullnessDuration}턴.");
    }

    /// <summary>노출을 적용한다. 받는 데미지 50% 증가.</summary>
    public void ApplyExposed(int duration)
    {
        exposedDuration = Mathf.Max(exposedDuration, duration);
        enemyDisplay?.UpdateExposedDisplay(exposedDuration);
        UpdateIntentDisplay(); // 인텐트 수치 즉시 갱신
        Logger.Log($"[Enemy] {enemyName} 노출 {exposedDuration}턴.");
    }

    /// <summary>약화 + 무뎌짐 + 힘이 적용된 데미지를 반환한다.</summary>
    public int GetWeakenedDamage(int baseDamage)
    {
        // ✅ TotalStrength 사용
        int dmg = baseDamage + TotalStrength;

        if (IsDullness)
            dmg = Mathf.RoundToInt(dmg * 0.75f);

        return Mathf.Max(0, dmg);
    }

    /// <summary>기절 상태인지 여부.</summary>
    public bool IsStunned => stunDuration > 0;

    /// <summary>기절을 적용한다.</summary>
    public void ApplyStun(int duration)
    {
        stunDuration = Mathf.Max(stunDuration, duration);
        enemyDisplay?.ShowStunEffect(true);
        Logger.Log($"[Enemy] {enemyName} 기절 {stunDuration}턴.");
    }

    /// <summary>턴 종료 시 기절 지속 턴 감소.</summary>
    public void TickStun()
    {
        if (stunDuration <= 0) return;
        stunDuration--;
        if (stunDuration <= 0)
        {
            enemyDisplay?.ShowStunEffect(false);
            Logger.Log($"[Enemy] {enemyName} 기절 해제.");
        }
    }

    /// <summary>턴 종료 시 무뎌짐/노출 지속 턴 감소.</summary>
    public void TickDullnessAndExposed()
    {
        if (dullnessDuration > 0)
        {
            dullnessDuration--;
            enemyDisplay?.UpdateDullnessDisplay(dullnessDuration);
            Logger.Log($"[Enemy] {enemyName} 무뎌짐 남은 턴: {dullnessDuration}");
        }
        if (exposedDuration > 0)
        {
            exposedDuration--;
            enemyDisplay?.UpdateExposedDisplay(exposedDuration);
            Logger.Log($"[Enemy] {enemyName} 노출 남은 턴: {exposedDuration}");
        }

        UpdateIntentDisplay(); // Tick 후 인텐트 수치 갱신
    }

    /// <summary>출혈 스택을 부여한다.</summary>
    public void AddBleed(int amount)
    {
        if (amount <= 0) return;
        currentBleedStacks += amount;
        Logger.Log($"[Enemy] {enemyName} 출혈 {amount} 부여됨. 현재 출혈 스택: {currentBleedStacks}");
    }

    public void InitializeEnemy(EnemyData enemyData, EnemyDisplay display)
    {
        Data = enemyData;
        AiType = enemyData.enemyAIType;
        enemyName = enemyData.enemyName;

        // HP 초기화
        maxHP = enemyData.health;
        currentHP = enemyData.health;
        strength = enemyData.initialStrength;

        enemyDisplay?.UpdateStrengthDisplay(strength); // ✅ 초기 힘 UI 반영
        // 방어도 초기화 (CharacterStats)
        InitializeStats();

        enemyDisplay = display;
        if (enemyDisplay != null)
            enemyDisplay.Setup(this, enemyData);
        else
            Logger.LogError($"[Enemy] {enemyName}의 EnemyDisplay가 NULL입니다.", this);

        AttachAI(enemyData.enemyAIType, enemyData, display);
        UpdateIntentDisplay();
    }

    private void AttachAI(EnemyAIType aiType, EnemyData enemyData, EnemyDisplay display)
    {
        switch (aiType)
        {
            case EnemyAIType.Normal:
                EnemyAI = gameObject.AddComponent<BasicEnemyAI>();
                break;

            case EnemyAIType.Boss:
                EnemyAI = gameObject.AddComponent<BossAI>();
                break;

            default:
                Logger.LogWarning($"[Enemy] {enemyName}에 AI가 없습니다. BasicEnemyAI로 대체합니다.", this);
                EnemyAI = gameObject.AddComponent<BasicEnemyAI>();
                break;
        }

        if (EnemyAI != null)
        {
            EnemyAI.SetPlayerStats(PlayerStats.Instance);
            EnemyAI.SetIntentIcons(enemyData.attackIntentIcon, enemyData.buffIntentIcon);
            EnemyAI.InitializeAI();
            EnemyAI.SetEnemyDisplay(display);
        }
    }

    // ── 턴 행동 ──────────────────────────────────────────────────

    public void PerformAction()
    {
        if (EnemyAI != null)
        {
            EnemyAI.ExecuteTurn();
            UpdateIntentDisplay();
        }
        else
        {
            Logger.LogWarning($"[Enemy] {enemyName}에 AI가 없습니다.", this);
            if (PlayerStats.Instance != null)
                PerformAttack(PlayerStats.Instance);
        }
    }

    public void UpdateIntentDisplay()
    {
        if (EnemyAI == null || enemyDisplay == null) return;
        Debug.Log($"[Enemy] {enemyName} UpdateIntentDisplay 호출");
        EnemyIntent intent = EnemyAI.PredictNextIntent();
        if (intent != null)
            enemyDisplay.SetIntent(intent);
        else
            enemyDisplay.ClearIntentDisplay();
    }

    // ── 데미지/회복/사망 ─────────────────────────────────────────

    /// <summary>방어도를 고려해 데미지를 입힌다.</summary>
    public override int TakeDamage(int amount)
    {
        // 노출 상태면 받는 데미지 50% 증가
        if (IsExposed)
        {
            Debug.Log($"[Enemy] {enemyName} TakeDamage 호출. IsExposed={IsExposed}, exposedDuration={exposedDuration}, amount={amount}");
            amount = Mathf.RoundToInt(amount * 1.5f);
            Logger.Log($"[Enemy] {enemyName} 노출 발동! 데미지 1.5배: {amount}");
        }

        int finalDamage = amount;

        // 出혈 메커니즘 적용
        if (currentBleedStacks > 0)
        {
            finalDamage += currentBleedStacks;
            currentBleedStacks--;
            if (currentBleedStacks < 0) currentBleedStacks = 0;
            Logger.Log($"[Enemy] {enemyName} 출혈 발동! 추가 데미지. 남은 출혈 스택: {currentBleedStacks}");
        }

        int initialArmor = Armor;
        int initialHP = currentHP;

        int realDamage = base.TakeDamage(finalDamage);

        currentHP = Mathf.Max(0, currentHP - realDamage);

        RelicManager.Instance?.OnEnemyDamaged(this, finalDamage, realDamage, initialArmor, initialHP);

        enemyDisplay?.UpdateDisplay(currentHP, maxHP);
        enemyDisplay?.ShowDamagePopup(realDamage);
        enemyDisplay?.UpdateArmorDisplay(Armor);

        Logger.Log($"[Enemy] {enemyName} {realDamage} 데미지. HP: {currentHP}/{maxHP}", this);

        if (currentHP <= 0)
            Die();

        return realDamage;
    }

    /// <summary>힘을 추가한다.</summary>
    public void AddStrength(int amount)
    {
        strength += amount;
        enemyDisplay?.UpdateStrengthDisplay(TotalStrength); // ✅ TotalStrength 사용
        UpdateIntentDisplay();
        Debug.Log($"[Enemy] {enemyName} AddStrength. strength={strength}, TotalStrength={TotalStrength}");
    }

    /// <summary>턴 종료 시 음수 힘 초기화.</summary>
    public void TickStrength()
    {
        if (strength < 0)
        {
            strength = 0;
            enemyDisplay?.UpdateStrengthDisplay(TotalStrength); // ✅ TotalStrength 사용
            UpdateIntentDisplay();
            Logger.Log($"[Enemy] {enemyName} 음수 힘 초기화.");
        }
    }

    public override void AddArmor(int amount)
    {
        base.AddArmor(amount);
        enemyDisplay?.UpdateArmorDisplay(Armor);
    }

    public void DoubleBleedStacks()
    {
        if (currentBleedStacks > 0)
        {
            currentBleedStacks *= 2;
            Debug.Log($"출혈이 2배가 되었습니다! 현재 출혈 스택: {currentBleedStacks}");
        }
    }

    /// <summary>방어도 무시 직접 HP 감소.</summary>
    public void LoseHealthDirect(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        enemyDisplay?.UpdateDisplay(currentHP, maxHP);
        Logger.Log($"[Enemy] {enemyName} {amount} HP 직접 감소. HP: {currentHP}/{maxHP}", this);

        if (currentHP <= 0)
            Die();
    }

    /// <summary>HP를 회복한다.</summary>
    public void HealHP(int amount)
    {
        if (amount <= 0) return;

        currentHP = Mathf.Min(currentHP + amount, maxHP);
        enemyDisplay?.UpdateDisplay(currentHP, maxHP);
        Logger.Log($"[Enemy] {enemyName} {amount} HP 회복. HP: {currentHP}/{maxHP}", this);
    }

    protected override void Die()
    {
        base.Die();

        if (enemyDisplay != null)
        {
            AudioManager.Instance?.PlaySFX("Enemy_Death");
            enemyDisplay.PlayDeathAnimation(() => EnemyManager.Instance?.RemoveEnemy(this));
        }
    }

    /// <summary>기본 공격을 수행한다. AI가 없을 때 폴백으로 사용.</summary>
    public override void PerformAttack(CharacterStats target)
    {
        int attackDamage = 10;

        if (target is PlayerStats playerStats)
        {
            playerStats.TakeDamage(attackDamage);
            Logger.Log($"[Enemy] {enemyName}이(가) 플레이어에게 {attackDamage} 데미지.", this);
        }
    }

    public void SetEnraged(bool value)
    {
        IsEnraged = value;
        enemyDisplay?.SetEnragedVisual(value);
    }

    // =====================================================================================
    // ★ [오류 해결부 추가]: 턴매니저와 무뎌짐/노출 디버프를 안전 정산하는 기믹 핵심 연동 함수
    // =====================================================================================

    /// <summary>
    /// 턴이 경과되어 적이 가진 방어도를 완전히 초기화하고 UI를 새로고침한다.
    /// </summary>
    public void ResetArmor()
    {
        // 부모인 CharacterStats의 Armor 프로퍼티를 0으로 리셋
        Armor = 0;

        // 방어도가 증발했으므로 에디터 실드 UI 마크도 숨김(0) 정산 처리
        enemyDisplay?.UpdateArmorDisplay(0);
        Logger.Log($"[Enemy] {enemyName}의 방어도가 턴 경과로 인해 완전히 초기화되었습니다.");
    }

    /// <summary>
    /// 적 행동 종료 후 적에게 걸려있던 약화 및 고유 디버프(무뎌짐/노출) 지속 시간을 차감한다.
    /// </summary>
    public void TickWeaken()
    {
        // 기존에 적이 갖고 있던 무뎌짐과 노출 감소 로직을 이 타이밍에 실시간 연동해 줍니다.
        TickDullnessAndExposed();
        Logger.Log($"[Enemy] {enemyName}의 차례가 끝나 상태이상 디버프가 1턴 감소했습니다.");
    }
}