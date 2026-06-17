using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 배틀 전체 흐름과 상태 전환을 관리하는 매니저.
/// START → PLAYER_TURN → ENEMY_TURN → WON / LOST
/// </summary>
public class BattleManager : SceneSingleton<BattleManager>
{
    public enum BattleState { START, PLAYER_TURN, ENEMY_TURN, WON, LOST }
    public BattleState State { get; private set; }

    private TurnManager turnManager;
    private EnemyManager enemyManager;

    /// <summary>플레이어 입력 잠금 여부.</summary>
    public bool IsPlayerInputLocked { get; private set; } = false;

    [Header("오디오")]
    [SerializeField] private AudioClip defeatJingle;

    private void Start()
    {
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        yield return null;
        Time.timeScale = 1f;
        InitializeReferences();
        StartBattle();
    }

    /// <summary>필요한 매니저 참조를 캐싱한다.</summary>
    private void InitializeReferences()
    {
        turnManager = TurnManager.Instance;
        enemyManager = EnemyManager.Instance;
    }

    /// <summary>배틀을 초기화하고 플레이어 턴을 시작한다.</summary>
    private void StartBattle()
    {
        SetBattleState(BattleState.START);

        DullnessTracker.Reset();
        ExposedTracker.Reset();

        PlayerManager.Instance?.InitializePlayer();
        PlayerStats.Instance?.ResetStrengthAndDexterity();

        enemyManager.InitializeFromScene();

        DistanceManager.Instance?.ResetDistance();
        RelicManager.Instance?.OnBattleStart();
        DeckManager.Instance.InitializeDeck();
        DeckManager.Instance.ShuffleDeck();

        turnManager.StartPlayerTurn();
    }

    /// <summary>배틀 상태를 변경하고 로그를 출력한다.</summary>
    public void SetBattleState(BattleState newState)
    {
        State = newState;
    }

    /// <summary>플레이어 입력을 잠근다.</summary>
    public void LockPlayerInput()
    {
        IsPlayerInputLocked = true;
    }

    /// <summary>플레이어 입력 잠금을 해제한다.</summary>
    public void UnlockPlayerInput()
    {
        IsPlayerInputLocked = false;
    }

    /// <summary>플레이어 패배 처리.</summary>
    private void HandlePlayerDefeat()
    {

        if (State == BattleState.LOST || State == BattleState.WON) return;

        SetBattleState(BattleState.LOST);

        AudioManager.Instance?.StopMusic();

        GameOverUIManager.Instance?.ShowGameOver();
    }

    /// <summary>배틀 승리 처리.</summary>
    public void HandleBattleVictory()
    {
        if (State == BattleState.WON || State == BattleState.LOST) return;

        SetBattleState(BattleState.WON);

        RewardManager.Instance?.ShowReward();
    }

    /// <summary>플레이어 사망 이벤트를 등록한다.</summary>
    public void RegisterPlayerEvents(PlayerStats playerStats)
    {
        if (playerStats != null)
        {
            playerStats.OnDied += HandlePlayerDefeat;
        }
    }

    /// <summary>배틀이 종료됐는지 여부를 반환한다.</summary>
    public bool IsBattleOver() => State == BattleState.LOST || State == BattleState.WON;

    // =====================================================================================
    // ★ [저주 연동 시스템] 턴 종료 버튼 클릭 시 페널티 조건부 정산 함수
    // =====================================================================================

    /// <summary>
    /// 플레이어 턴 종료 프로세스 도중 (손패 카드를 버리기 전 타이밍) 실행하여 저주 피해를 정산합니다.
    /// </summary>
    public void CheckCurseDamageOnTurnEnd()
    {
        if (HandManager.Instance == null || PlayerDataManager.Instance == null) return;

        // 패에 '남은 카드당 피해'를 주는 저주 카드가 실제로 존재하는지 필터링 스캔
        if (HandManager.Instance.HasCurseEffectInHand(CardEffectType.CurseDamageByHand))
        {
            // 현재 손패 루트 밑에 깔린 자식 오브젝트(남아있는 카드)의 총 개수 추출
            int remainingCardCount = HandManager.Instance.CurrentHandSize;

            if (remainingCardCount > 0)
            {
                // 카드 장당 1의 데미지 환산 연산 (기획 의도에 맞춰 조절 가능)
                int finalPenaltyDamage = remainingCardCount * 1;

                // PlayerDataManager를 거쳐 플레이어 실시간 HP 참감 처리
                PlayerDataManager.Instance.ModifyHP(-finalPenaltyDamage);

                Debug.Log($"[저주 피해 정산 완료] 손패에 저주 카드가 잔류하고 있습니다! 남은 카드 수: {remainingCardCount}장 -> 최종 피해: {finalPenaltyDamage}");
            }
        }
    }
}