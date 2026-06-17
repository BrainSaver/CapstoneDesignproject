using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 플레이어 초기화, 에너지 소모, 카드 효과 적용 등을 담당하는 매니저 클래스입니다.
/// 플레이어는 씬에 미리 배치되어 있으므로 프리팹 스폰이 아닌 참조 방식을 사용합니다.
/// </summary>
public class PlayerManager : SceneSingleton<PlayerManager>
{
    /// <summary>
    /// 씬에서 PlayerStats를 찾아 플레이어 상태를 초기화합니다.
    /// 플레이어는 씬에 미리 배치되어 있으므로 별도로 스폰하지 않습니다.
    /// </summary>
    public void InitializePlayer()
    {
        // Unity 6 권장 사항: FindObjectOfType 대신 Object.FindAnyObjectByType 사용
        PlayerStats playerStats = Object.FindAnyObjectByType<PlayerStats>();

        BattleManager.Instance.RegisterPlayerEvents(playerStats);
        playerStats.ResetEnergy();
        playerStats.ResetArmor();
    }

    /// <summary>
    /// 카드를 플레이할 에너지가 충분한지 확인합니다.
    /// </summary>
    /// <param name="card">검사할 카드 객체</param>
    /// <returns>플레이 가능 여부</returns>
    public bool CanPlayCard(Card card)
    {
        if (card == null) return false;

        // 유물 효과로 인해 카드 플레이가 불가능한지 체크
        if (RelicManager.Instance != null && !RelicManager.Instance.OnCanPlayCard(card)) return false;

        // 특정 카드 무료 버프 적용 여부 (FreeCostTracker)
        if (FreeCostTracker.IsFree(card)) return true;

        // 다음 N장 무료 버프 적용 여부 (FreeCardCountTracker)
        if (FreeCardCountTracker.HasFreeCard()) return true;

        // 기본 코스트 계산 및 유물 효과 반영
        int finalCost = card.energyCost;
        RelicManager.Instance?.OnGetCardCost(card, ref finalCost);

        return PlayerStats.Instance != null &&
               PlayerStats.Instance.energy >= finalCost;
    }

    /// <summary>
    /// 카드를 사용하고 그에 따른 비용(에너지 또는 HP)을 소모합니다.
    /// </summary>
    /// <param name="card">사용할 카드 객체</param>
    public void UseCard(Card card)
    {
        if (card == null) return;

        // [특수 기믹] 피의 영창(BloodChant) 활성화 시 코스트만큼 에너지 대신 HP 소모
        if (BloodChantTracker.IsActive)
        {
            int cost = card.energyCost;
            if (cost > 0)
            {
                // ✅ 코스트 x 2만큼 HP 소모
                PlayerDataManager.Instance?.ModifyHP(-(cost * 2));
                Logger.Log($"[PlayerManager] 피의 영창 발동 — 코스트 {cost} x 2 = {cost * 2}만큼 HP 소모.");

                if (PlayerDataManager.Instance?.currentHP <= 0)
                    PlayerStats.Instance?.TriggerDeath();
            }
            return;
        }

        // 기존 에너지 소모 로직 버프 체크
        if (FreeCostTracker.IsFree(card))
        {
            FreeCostTracker.ConsumeCard(card);
        }
        else if (FreeCardCountTracker.HasFreeCard())
        {
            FreeCardCountTracker.ConsumeOne();
        }
        else
        {
            int cost = card.energyCost;
            RelicManager.Instance?.OnGetCardCost(card, ref cost);
            PlayerStats.Instance?.UseEnergy(cost);
        }
    }

    /// <summary>
    /// 현재 아군 목록을 반환합니다. (확장성을 고려하여 List 구조 유지)
    /// </summary>
    /// <returns>아군 PlayerStats 리스트</returns>
    public List<PlayerStats> GetAllies()
    {
        var allies = new List<PlayerStats>();
        if (PlayerStats.Instance != null)
        {
            allies.Add(PlayerStats.Instance);
        }
        return allies;
    }
}