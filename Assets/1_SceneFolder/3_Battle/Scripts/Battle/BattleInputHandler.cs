using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// 카드 선택 후 클릭으로 카드를 사용한다.
/// Attack/SingleEnemy: 적 클릭으로 사용
/// Skill/Movement/AllEnemies/Self/None: 빈 공간 클릭으로 사용
/// </summary>
public class BattleInputHandler : MonoBehaviour
{
    private void Update()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        HandleClick();
    }

    private void HandleClick()
    {
        if (CardMovement.SelectedCard == null) return;

        var selectedCard = CardMovement.SelectedCard;
        var card = selectedCard.cardData;

        // 카드 위 클릭이면 무시 (카드 선택/해제는 CardMovement가 처리)
        if (IsClickOnCard()) return;

        // Attack 카드 또는 SingleEnemy 타겟 — 레이캐스트로 적 탐색
        if (card.cardType == Card.CardType.Attack ||
            card.targetType == Card.TargetType.SingleEnemy)
        {
            Enemy enemy = GetEnemyUnderCursor();

            if (enemy == null)
            {
                Logger.Log("[BattleInputHandler] 적을 클릭해서 사용하세요.");
                return;
            }

            Logger.Log($"[BattleInputHandler] 적 '{enemy.enemyName}' 클릭. 카드 사용.");
            selectedCard.UseCardOnTarget(enemy);
            return;
        }

        // AllEnemies — 적 클릭 또는 빈 공간 클릭으로 사용
        if (card.targetType == Card.TargetType.AllEnemies)
        {
            Logger.Log("[BattleInputHandler] AllEnemies 카드 사용.");
            selectedCard.UseCardNoTarget();
            return;
        }

        // Skill, Movement, Self, None — 빈 공간 클릭으로 사용
        Logger.Log($"[BattleInputHandler] '{card.cardName}' 카드 사용.");
        selectedCard.UseCardNoTarget();
    }

    /// <summary>커서 아래 적을 레이캐스트로 찾는다.</summary>
    private Enemy GetEnemyUnderCursor()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            var enemy = r.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                Logger.Log($"[BattleInputHandler] 레이캐스트로 '{enemy.enemyName}' 발견.");
                return enemy;
            }
        }

        return null;
    }

    /// <summary>클릭한 위치에 카드가 있는지 확인한다.</summary>
    private bool IsClickOnCard()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            if (r.gameObject.GetComponentInParent<CardMovement>() != null)
            {
                Logger.Log("[BattleInputHandler] 카드 위 클릭 — 무시.");
                return true;
            }
        }

        return false;
    }
}