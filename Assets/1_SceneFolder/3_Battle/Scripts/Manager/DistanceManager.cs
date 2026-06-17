using UnityEngine;
using System;

/// <summary>
/// 플레이어와 적 사이의 거리를 관리하며, 플레이어가 카드로 움직일 때마다 
/// 모든 살아있는 적들의 머리 위 사거리 예고 UI를 실시간으로 갱신시킵니다.
/// </summary>
public class DistanceManager : SceneSingleton<DistanceManager>
{
    [Header("거리 설정")]
    [SerializeField] private int startDistance = 2;  // 전투 시작 시 초기 거리
    [SerializeField] private int minDistance = 1;    // 최소 거리 (인파이팅)
    [SerializeField] private int maxDistance = 2;    // 최대 거리 (아웃복싱)

    /// <summary>현재 거리.</summary>
    public int CurrentDistance { get; private set; }

    /// <summary>거리 변경 시 발행. 파라미터: (이전 거리, 현재 거리).</summary>
    public static event Action<int, int> OnDistanceChanged;

    protected override void Awake()
    {
        base.Awake();
        CurrentDistance = startDistance;
    }

    /// <summary>배틀 시작 시 초기 거리로 리셋한다.</summary>
    public void ResetDistance()
    {
        int prev = CurrentDistance;
        CurrentDistance = startDistance;
        Logger.Log($"[DistanceManager] 거리 리셋: {CurrentDistance}", this);
        OnDistanceChanged?.Invoke(prev, CurrentDistance);
    }

    /// <summary>
    /// 거리를 변경한다. 양수면 후퇴, 음수면 전진.
    /// 이동하는 순간 적들의 사거리 인텐트를 실시간으로 스위칭(새로고침)합니다.
    /// </summary>
    public int Move(int amount)
    {
        int prev = CurrentDistance;
        int newDist = Mathf.Clamp(CurrentDistance + amount, minDistance, maxDistance);
        int actualMove = newDist - prev;

        CurrentDistance = newDist;

        Logger.Log($"[DistanceManager] 거리 변경: {prev} → {CurrentDistance} (요청: {amount}, 실제: {actualMove})", this);
        OnDistanceChanged?.Invoke(prev, CurrentDistance);

        // ★ [실시간 사거리 UI 새로고침 핵심 연동]
        // 플레이어가 이동 카드를 내는 순간, 화면 속 모든 적의 '사거리 밖 예고 ↔ 원래 대미지' 수치를 바꿉니다.
        if (EnemyManager.Instance != null && EnemyManager.Instance.GetActiveEnemies() != null)
        {
            foreach (var enemy in EnemyManager.Instance.GetActiveEnemies())
            {
                if (enemy == null) continue;

                // 1. 적 AI 내부 예측 함수를 호출해 사거리를 실시간 재연산시킴
                if (enemy.EnemyAI != null)
                {
                    enemy.EnemyAI.PredictNextIntent();
                }

                // 2. 바뀐 수치를 머리 위 말풍선 그래픽에 강제로 적용해 갱신시킴
                enemy.UpdateIntentDisplay();
            }
        }

        return actualMove;
    }

    /// <summary>현재 거리가 지정된 범위 안에 있는지 확인한다.</summary>
    public bool IsInRange(int minRange, int maxRange)
    {
        return CurrentDistance >= minRange && CurrentDistance <= maxRange;
    }

    /// <summary>현재 거리 구간 이름을 반환한다 (UI 표시용).</summary>
    public string GetRangeName()
    {
        return CurrentDistance switch
        {
            1 => "인파이팅",
            2 => "아웃 파이팅",
            _ => "알 수 없음"
        };
    }
}