using System;
using UnityEngine;

/// <summary>
/// 방어도와 사망 처리를 담당하는 모든 캐릭터의 기반 클래스.
/// HP는 각 서브클래스에서 관리한다.
/// </summary>
public abstract class CharacterStats : MonoBehaviour
{
    [Header("비주얼")]
    [Tooltip("공격 애니메이션 등에 사용하는 비주얼 트랜스폼.")]
    public Transform characterVisualTransform;

    public int Armor { get; protected set; }

    /// <summary>방어도 변화 이벤트. 파라미터: (현재 방어도).</summary>
    public event Action<int> OnArmorChanged;

    /// <summary>사망 이벤트.</summary>
    public event Action OnDied;

    /// <summary>기본 공격 가상 메서드. 서브클래스에서 오버라이드한다.</summary>
    public virtual void PerformAttack(CharacterStats target) { }

    /// <summary>스탯 초기화.</summary>
    public virtual void InitializeStats(int startingArmor = 0)
    {
        Armor = startingArmor;
        OnArmorChanged?.Invoke(Armor);
    }
    /// <summary>
    /// 방어도에는 2배의 데미지를 입히고, 남은 데미지를 체력에 입힙니다. (예: 바디블로우)
    /// </summary>
    /// <summary>
    public void TakeAntiArmorDamage(int baseDamage)
    {
        // 방어도가 1 이상 있을 때
        if (Armor > 0)
        {
            int armorDamage = baseDamage * 2; // 방어도에 들어갈 데미지 뻥튀기 (예: 10 -> 20)

            // 방어도가 데미지보다 높아서 다 막아내는 경우
            if (Armor >= armorDamage)
            {
                Armor -= armorDamage;
                Debug.Log($"방어도에 {armorDamage} 데미지! 남은 방어도: {Armor}");

                // 💡 방어도가 깎였으니 UI 갱신 이벤트 호출
                OnArmorChanged?.Invoke(Armor);
            }
            // 방어도가 깨지고 남은 데미지가 체력으로 들어가는 경우
            else
            {
                int remainingArmorDamage = armorDamage - Armor; // 방어도를 뚫고 남은 아머 데미지
                Armor = 0; // 방어도 파괴

                // 💡 방어도가 0이 되었으니 UI 갱신 이벤트 호출
                OnArmorChanged?.Invoke(Armor);

                // 남은 아머 데미지를 다시 원래의 체력 데미지 비율(/2)로 되돌림
                int remainingBaseDamage = remainingArmorDamage / 2;

                Debug.Log($"방어도 파괴! 남은 {remainingBaseDamage} 데미지가 체력에 들어갑니다.");

                // 기존에 만들어두신 기본 피격 함수로 남은 데미지 전달
                TakeDamage(remainingBaseDamage);
            }
        }
        // 방어도가 아예 없으면 그냥 1배율 기본 데미지를 입힘
        else
        {
            TakeDamage(baseDamage);
        }
    }
    /// <summary>방어도를 고려해 데미지를 입힌다. 실제 들어간 데미지를 반환한다.</summary>
    public virtual int TakeDamage(int amount)
    {
        int damageAfterArmor = Mathf.Max(0, amount - Armor);
        Armor = Mathf.Max(0, Armor - amount);

        OnArmorChanged?.Invoke(Armor);

        return damageAfterArmor;
    }

    /// <summary>방어도를 추가한다.</summary>
    public virtual void AddArmor(int amount)
    {
        Armor = Mathf.Max(0, Armor + amount);
        Logger.Log($"{gameObject.name}이(가) {amount} 방어도를 얻었습니다.", this);
        OnArmorChanged?.Invoke(Armor);
    }

    /// <summary>사망 처리.</summary>
    protected virtual void Die()
    {
        OnDied?.Invoke();
    }
}