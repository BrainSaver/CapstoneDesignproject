using UnityEngine;

/// <summary>
/// 배틀 패배 시 Game Over 패널을 표시하고 재시도/메인메뉴 버튼을 처리하는 UI 매니저.
/// </summary>
public class GameOverUIManager : SceneSingleton<GameOverUIManager>
{
    [SerializeField] private GameObject gameOverPanel;

    protected override void Awake()
    {
        base.Awake();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    /// <summary>Game Over 패널을 표시하고 타임스케일을 0으로 설정한다.</summary>
    public void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
        Logger.Log("[GameOverUIManager] Game Over 표시.", this);
    }

    /// <summary>Game Over 패널을 숨기고 타임스케일을 복원한다.</summary>
    public void HideGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        Time.timeScale = 1f;
    }
    /// <summary>메인메뉴 버튼 클릭 시 메인메뉴로 이동한다.</summary>
    public void OnMainMenuClicked()
    {
        HideGameOver();
        Time.timeScale = 1f;

        // 모든 진행 상황 초기화
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.currentGold = 100;
            PlayerDataManager.Instance.maxHP = 100;
            PlayerDataManager.Instance.currentHP = 100;
            PlayerDataManager.Instance.currentRelics.Clear();
            PlayerDataManager.Instance.savedMapNodes = null;
            PlayerDataManager.Instance.playerMapPos = new UnityEngine.Vector2Int(-1, -1);
            PlayerDataManager.Instance.halveNextEnemyHP = false;
            PlayerDataManager.Instance.UpdateAllUI();
        }

        // 덱 초기화
        PlayerDeck.Instance?.InitializeStartingDeck();

        // static 트래커 초기화
        ExhaustPile.Reset();
        RevengeTracker.ResetTurn();
        DamageReductionTracker.Reset();
        DoubleAttackTracker.Reset();
        PerfectBlockTracker.ResetTurn();
        FreeCardCountTracker.Reset();
        FreeCostTracker.Reset();
        RetainHandTracker.Reset();

        // 세션 초기화
        GameSession.Instance?.ResetStats();

        Logger.Log("[GameOverUIManager] 모든 진행 상황 초기화 완료.");

        SceneFlowManager.Instance?.LoadMainMenu();
    }
}