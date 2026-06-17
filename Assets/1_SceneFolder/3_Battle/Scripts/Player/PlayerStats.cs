using UnityEngine;
using System;

/// <summary>
/// 플레이어 방어력, 에너지, 스탯을 관리하는 스크립트.
/// HP는 PlayerDataManager.Instance.ModifyHP로 관리한다.
/// </summary>
public class PlayerStats : CharacterStats
{
    public static PlayerStats Instance { get; private set; }

    [Header("에너지 설정")]
    public int initialEnergy;
    public int energy;

    [Header("힘 / 민첩")]
    public int strength = 0;      // 카드/포션으로 얻은 힘 (배틀 내 유지)
    public int dexterity = 0;     // 카드/포션으로 얻은 민첩 (배틀 내 유지)
    public int relicStrength = 0; // 유물로 얻은 힘 (매 턴 재계산)
    public int relicDexterity = 0;// 유물로 얻은 민첩 (매 턴 재계산)

    /// <summary>실제 적용되는 총 힘 (카드힘 + 유물힘).</summary>
    public int TotalStrength => strength + relicStrength;

    /// <summary>실제 적용되는 총 민첩 (카드민첩 + 유물민첩).</summary>
    public int TotalDexterity => dexterity + relicDexterity;

    /// <summary>스탯 변경 시 발생 알림. 방어력, 에너지 UI 등에 연결.</summary>
    public static event Action OnStatsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeStats();
            energy = initialEnergy;
            OnArmorChanged += HandleArmorRelay;

            // PlayerDataManager HP 변경 이벤트 구독
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.OnHPChanged += OnHPChangedHandler;

            NotifyUI();
        }
        else
        {
            Logger.LogWarning("[PlayerStats] 중복 인스턴스 감지. 파괴합니다.");
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 💡 핵심 수정: 싱글톤 인스턴스인 경우에만 구독을 해제하여 다른 인스턴스 파괴 시의 부작용을 방지합니다.
        if (Instance == this)
        {
            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.OnHPChanged -= OnHPChangedHandler;
        }
    }

    /// <summary>카드/포션으로 힘을 추가한다. 배틀 내 유지.</summary>
    public void AddStrength(int amount)
    {
        strength += amount;
        OnStatsChanged?.Invoke();
        Logger.Log($"[PlayerStats] 힘 +{amount}. 카드힘: {strength}, 총 힘: {TotalStrength}");
    }

    /// <summary>카드/포션으로 민첩을 추가한다. 배틀 내 유지.</summary>
    public void AddDexterity(int amount)
    {
        dexterity += amount;
        OnStatsChanged?.Invoke();
        Logger.Log($"[PlayerStats] 민첩 +{amount}. 카드민첩: {dexterity}, 총 민첩: {TotalDexterity}");
    }

    /// <summary>배틀 시작 시 힘/민첩 초기화.</summary>
    public void ResetStrengthAndDexterity()
    {
        strength = 0;
        dexterity = 0;
        relicStrength = 0;
        relicDexterity = 0;
        OnStatsChanged?.Invoke();
    }

    /// <summary>유물 힘 보너스를 설정한다. 매 턴 재계산.</summary>
    public void SetRelicStrength(int amount)
    {
        relicStrength = amount;
        OnStatsChanged?.Invoke();
    }

    /// <summary>유물 민첩 보너스를 설정한다. 매 턴 재계산.</summary>
    public void SetRelicDexterity(int amount)
    {
        relicDexterity = amount;
        OnStatsChanged?.Invoke();
    }

    /// <summary>방어도 획득 시 민첩 적용.</summary>
    public override void AddArmor(int amount)
    {
        int finalArmor = amount + TotalDexterity; // ✅ TotalDexterity 사용
        base.AddArmor(finalArmor);
        RelicManager.Instance?.OnArmorGained(finalArmor);
        NotifyUI();
    }

    /// <summary>PlayerDataManager HP 변경 시 UI 갱신 및 사망 체크.</summary>
    private void OnHPChangedHandler(int current, int max)
    {
        NotifyUI();

        // HP 0 이하이면 사망
        if (current <= 0)
            Die();
    }

    // -- 데미지/회복 관련 로직 --

    public override int TakeDamage(int amount)
    {
        amount = DamageReductionTracker.ApplyToDamage(amount);

        // ✅ 노출 상태면 받는 데미지 50% 증가
        amount = ExposedTracker.ApplyToDamage(amount);

        if (amount > 0 && Armor == amount)
        {
            Logger.Log($"[PlayerStats] 저스트 가드 성공!");
            PerfectBlockTracker.RegisterPerfectBlock();
        }

        int realDamage = base.TakeDamage(amount);

        if (realDamage > 0)
        {
            RelicManager.Instance?.OnDamageTaken(ref realDamage);
            if (realDamage > 0)
            {
                PlayerDataManager.Instance?.ModifyHP(-realDamage);
                RevengeTracker.RecordDamage(realDamage);
            }
        }

        NotifyUI();
        return realDamage;
    }

    /// <summary>턴 종료 시 음수 힘 초기화.</summary>
    public void TickStrength()
    {
        if (strength < 0)
        {
            strength = 0;
            OnStatsChanged?.Invoke();
            Logger.Log("[PlayerStats] 음수 힘 초기화.");
        }
    }

    public void LoseHealthDirect(int amount)
    {
        if (amount <= 0) return;

        PlayerDataManager.Instance?.ModifyHP(-amount);
        Logger.Log($"[PlayerStats] {amount} HP 직접 감소 (방어력 무시). 남은 HP: {PlayerDataManager.Instance?.currentHP}");

        NotifyUI();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;

        PlayerDataManager.Instance?.ModifyHP(amount);
        Logger.Log($"[PlayerStats] {amount} HP 회복. 현재 HP: {PlayerDataManager.Instance?.currentHP}");

        NotifyUI();
    }

    public void SetCurrentHealth(int value)
    {
        if (PlayerDataManager.Instance == null) return;

        int diff = value - PlayerDataManager.Instance.currentHP;
        PlayerDataManager.Instance.ModifyHP(diff);

        NotifyUI();
    }
    // -- 에너지 관련 --

    public void ResetEnergy()
    {
        energy = initialEnergy;
        NotifyUI();
    }

    public void ResetArmor()
    {
        Armor = 0;
        NotifyUI();
    }

    public void UseEnergy(int amount)
    {
        energy -= amount;
        if (energy < 0) energy = 0;
        NotifyUI();
    }

    public void GainEnergy(int amount)
    {
        energy += amount;
        NotifyUI();
    }

    // -- 유틸리티 --

    private void NotifyUI() => OnStatsChanged?.Invoke();

    protected override void Die()
    {
        Logger.Log("[PlayerStats] 플레이어 사망. Game Over.", this);
        base.Die();
    }
    public void TriggerDeath()
    {
        Die();
    }

    private void HandleArmorRelay(int _) => NotifyUI();
}
