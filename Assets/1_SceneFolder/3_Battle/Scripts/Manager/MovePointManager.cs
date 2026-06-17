using UnityEngine;
using System;

/// <summary>
/// 매 턴 3코스트가 제공되는 이동 포인트를 관리한다.
/// 에너지와 별개로 동작하며 턴 시작 시 자동으로 리셋된다.
/// </summary>
public class MovePointManager : SceneSingleton<MovePointManager>
{
    [Header("이동 코스트 설정")]
    [SerializeField] private int maxPointsPerTurn = 2; // 매 턴 제공되는 이동 코스트

    /// <summary>현재 남은 이동 코스트.</summary>
    public int CurrentPoints { get; private set; }

    /// <summary>최대 이동 코스트.</summary>
    public int MaxPoints => maxPointsPerTurn;

    /// <summary>이동 코스트 변경 시 발행. 파라미터: (현재 코스트, 최대 코스트).</summary>
    public static event Action<int, int> OnMovePointsChanged;

    protected override void Awake()
    {
        base.Awake();
        CurrentPoints = maxPointsPerTurn;
    }

    /// <summary>턴 시작 시 이동 코스트를 최대치로 리셋한다.</summary>
    public void ResetPoints()
    {
        CurrentPoints = maxPointsPerTurn;
        Logger.Log($"[MovePointManager] 이동 코스트 리셋: {CurrentPoints}/{MaxPoints}");
        OnMovePointsChanged?.Invoke(CurrentPoints, MaxPoints);
    }

    /// <summary>이동 코스트가 충분한지 확인한다.</summary>
    public bool HasEnoughPoints(int cost) => CurrentPoints >= cost;

    /// <summary>이동 코스트를 소모한다.</summary>
    public void UsePoints(int cost)
    {
        CurrentPoints = Mathf.Max(0, CurrentPoints - cost);
        Logger.Log($"[MovePointManager] 이동 코스트 소모: -{cost}, 남은: {CurrentPoints}/{MaxPoints}");
        RelicManager.Instance?.OnMovePointUsed(cost);
        OnMovePointsChanged?.Invoke(CurrentPoints, MaxPoints);
    }
}