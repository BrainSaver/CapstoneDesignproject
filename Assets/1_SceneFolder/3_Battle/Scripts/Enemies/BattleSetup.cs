using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 미리 배치된 EnemyCanvas 그룹 중 하나를 랜덤으로 활성화한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class BattleSetup : MonoBehaviour
{
    [Header("적 배치 그룹 목록")]
    [Tooltip("미리 배치해둔 EnemyCanvas 그룹들. 이 중 하나가 랜덤으로 활성화됨.")]
    public List<GameObject> enemyGroups = new();

    [Header("보스 설정")]
    [Tooltip("true면 보스 그룹 고정 사용.")]
    public bool isBossBattle = false;

    [Tooltip("보스 전투 시 사용할 고정 그룹.")]
    public GameObject bossGroup;

    /// <summary>선택된 그룹. EnemyManager에서 참조.</summary>
    public GameObject SelectedGroup { get; private set; }

    private void Awake()
    {
        if (enemyGroups == null || enemyGroups.Count == 0)
        {
            Logger.LogError("[BattleSetup] enemyGroups가 비어있습니다.");
            return;
        }

        // 보스 전투
        if (isBossBattle)
        {
            if (bossGroup != null)
            {
                SelectedGroup = bossGroup;
                SelectedGroup.SetActive(true);
                Logger.Log("[BattleSetup] 보스 그룹 활성화.");
            }
            return;
        }
        else
        {
            // 랜덤 그룹 선택 후 활성화 (나머지는 이미 비활성화 상태)
            int index = UnityEngine.Random.Range(0, enemyGroups.Count);
            SelectedGroup = enemyGroups[index];
            SelectedGroup.SetActive(true);

            Logger.Log($"[BattleSetup] '{SelectedGroup.name}' 그룹 활성화.");
            return;
        }

    }
}