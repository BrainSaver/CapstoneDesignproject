using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// 플레이어 턴과 적 턴 전환을 관리하는 매니저 (적 방어도 자동 소멸 시스템 반영).
/// </summary>
public class TurnManager : SceneSingleton<TurnManager>
{
    public event Action OnPlayerTurnStart;
    public event Action OnPlayerTurnEnd;
    public event Action OnEnemyTurnStart;
    public event Action OnEnemyTurnEnd;

    [Header("참조")]
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private PlayerManager playerManager;

    private bool _endingTurn;
    public bool IsEndingTurn => _endingTurn;
    public bool IsPlayerTurn { get; private set; }

    private void Start()
    {
        if (enemyManager == null) enemyManager = EnemyManager.Instance;
        if (playerManager == null) playerManager = PlayerManager.Instance;
    }

    /// <summary>플레이어 턴을 시작한다. 에너지/방어도 리셋 → 입력 잠금 해제 → 카드 드로우.</summary>
    public void StartPlayerTurn()
    {
        Logger.Log("[TurnManager] 플레이어 턴 시작.", this);
        IsPlayerTurn = true;

        // 액티브 적들의 의도(Intent) UI 레이아웃 실시간 새로고침
        if (EnemyManager.Instance != null)
        {
            foreach (var enemy in EnemyManager.Instance.GetActiveEnemies())
            {
                if (enemy != null) enemy.UpdateIntentDisplay();
            }
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.ResetEnergy();
            PlayerStats.Instance.ResetArmor();

            // 유물 효과로 다음 턴 방어도 획득
            int extraArmor = ArmorNextTurnTracker.Consume();
            if (extraArmor > 0) PlayerStats.Instance.AddArmor(extraArmor);
        }

        // 상쇄 보너스 무료 카드 적용
        int freeCards = PerfectBlockTracker.ConsumeAndReset();
        if (freeCards > 0)
        {
            FreeCardCountTracker.AddFreeCards(freeCards);
            Logger.Log($"[TurnManager] 상쇄 보너스 무료 카드 {freeCards}장 적용.");
        }

        // 미사용 이동 보너스 및 버프/디버프 턴 상태 초기화
        RevengeTracker.ResetTurn();
        DamageReductionTracker.TickReduction();
        DoubleAttackTracker.Reset();
        FreeCostTracker.Reset();
        RetainHandTracker.Reset();
        PlayerStats.Instance?.TickStrength();
        MoveBonusTracker.Instance?.ResetBonus();
        MovePointManager.Instance?.ResetPoints();

        RelicManager.Instance?.OnTurnStart();

        BattleManager.Instance.UnlockPlayerInput();
        OnPlayerTurnStart?.Invoke();

        // ── 드로우 장수 계산 시스템 ──
        int drawCount = HandManager.Instance.StartingHandSize;
        drawCount += DrawNextTurnTracker.Consume();

        // 유물 패널티 (Cursed Crown: 드로우 카드 1 감소)
        if (RelicManager.Instance != null && RelicManager.Instance.HasRelic("cursedCrown"))
            drawCount -= 1;

        int spaceLeft = HandManager.Instance.MaxHandSize - HandManager.Instance.CurrentHandSize;
        int finalDrawCount = Mathf.Min(drawCount, spaceLeft);

        if (finalDrawCount > 0)
        {
            // [버그 해결 완료]: 두 번째 매개변수로 true를 명확하게 넘겨서 "턴 시작 드로우 저주 면역" 처리
            StartCoroutine(HandManager.Instance.DrawCardsRoutine(finalDrawCount, true));
        }

        MovePointManager.Instance?.ResetPoints();
    }

    /// <summary>플레이어 턴을 종료한다.</summary>
    public void EndPlayerTurn()
    {
        if (_endingTurn) return;
        RelicManager.Instance?.OnTurnEnd();
        BloodChantTracker.Reset();
        StartCoroutine(EndPlayerTurnRoutine());
    }

    private IEnumerator EndPlayerTurnRoutine()
    {
        _endingTurn = true;
        BattleManager.Instance.LockPlayerInput();

        // 드로우 연산이 진행 중이면 완전히 완료될 때까지 안전 대기
        while (HandManager.Instance.IsDrawing) yield return null;

        PerfectBlockTracker.ResetTurn();

        Logger.Log("[TurnManager] 플레이어 턴 종료.", this);
        IsPlayerTurn = false;

        // 플레이어 행동이 확정 종료되었으므로 고유 디버프 턴 수를 1씩 차감(Tick)시킵니다.
        DullnessTracker.Tick();
        ExposedTracker.Tick();

        // 저주 카드 손패 비례 피해 정산 실행
        BattleManager.Instance?.CheckCurseDamageOnTurnEnd();

        // 손패 전체 버림
        yield return StartCoroutine(HandManager.Instance.DiscardHandRoutine(animated: true));

        OnPlayerTurnEnd?.Invoke();

        // 적 차례 턴 진행
        yield return StartCoroutine(EnemyTurn());

        _endingTurn = false;
    }

    private IEnumerator EnemyTurn()
    {
        Logger.Log("[TurnManager] 적 턴 시작.", this);
        OnEnemyTurnStart?.Invoke();

        // 적들이 행동 개시 직전 이전 턴 방어도 초기화
        if (enemyManager != null && enemyManager.Enemies != null)
        {
            foreach (var enemy in enemyManager.Enemies)
            {
                if (enemy == null) continue;
                enemy.ResetArmor();
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (BattleManager.Instance.IsBattleOver())
        {
            Logger.LogWarning("[TurnManager] 적 턴 취소: 배틀이 이미 종료됨.", this);
            yield break;
        }

        // 적 행동 수행
        yield return StartCoroutine(enemyManager.PerformEnemyActionsCoroutine());

        yield return new WaitForSeconds(1f);

        if (BattleManager.Instance.IsBattleOver()) yield break;

        // ✅ 적 행동 완료 후 상태이상 Tick (행동 먼저, 초기화 나중)
        foreach (var enemy in enemyManager.GetActiveEnemies())
        {
            if (enemy == null) continue;
            enemy.TickStrength();        // ✅ 음수 힘 초기화 (행동 후)
            enemy.TickDullnessAndExposed(); // ✅ 무뎌짐/노출 감소
        }

        // 다음 인텐트 예측치 정산 및 마크 갱신
        foreach (Enemy enemy in enemyManager.Enemies)
        {
            if (enemy == null) continue;
            var display = enemy.GetComponent<EnemyDisplay>();
            if (enemy.EnemyAI != null && display != null)
                display.SetIntent(enemy.EnemyAI.PredictNextIntent());
        }

        if (BattleManager.Instance.IsBattleOver()) yield break;

        Logger.Log("[TurnManager] 적 턴 종료.", this);
        OnEnemyTurnEnd?.Invoke();

        if (GameSession.Instance != null) GameSession.Instance.turnsTaken++;

        // 플레이어 차례 턴으로 안전 복귀
        StartPlayerTurn();
    }
}