using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 스폰, 추적, 행동 실행, 제거를 담당하는 매니저.
/// </summary>
public class EnemyManager : SceneSingleton<EnemyManager>
{
    private readonly List<Enemy> activeEnemies = new();
    private bool _initialized;

    // ── 초기화 ───────────────────────────────────────────────────

    /// <summary>
    /// 씬의 BattleSetup 컴포넌트를 읽어 적을 스폰한다.
    /// </summary>
    public void InitializeFromScene()
    {
        if (_initialized)
        {
            Logger.LogWarning("[EnemyManager] 이미 초기화됨.", this);
            return;
        }

        var setup = Object.FindAnyObjectByType<BattleSetup>();
        if (setup == null)
        {
            Logger.LogError("[EnemyManager] BattleSetup이 없습니다.", this);
            return;
        }

        if (setup.SelectedGroup == null)
        {
            Logger.LogError("[EnemyManager] 선택된 적 그룹이 없습니다.", this);
            return;
        }

        ClearExistingEnemies();

        var enemies = setup.SelectedGroup.GetComponentsInChildren<Enemy>(true);

        if (enemies.Length == 0)
        {
            Logger.LogError("[EnemyManager] 선택된 그룹에 Enemy 컴포넌트가 없습니다.", this);
            return;
        }

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            var display = enemy.GetComponent<EnemyDisplay>();
            if (display == null)
            {
                Logger.LogError($"[EnemyManager] {enemy.name}에 EnemyDisplay가 없습니다.", this);
                continue;
            }

            if (enemy.Data == null)
            {
                Logger.LogError($"[EnemyManager] {enemy.name}에 EnemyData가 없습니다.", this);
                continue;
            }

            enemy.InitializeEnemy(enemy.Data, display);
            activeEnemies.Add(enemy);

            Logger.Log($"[EnemyManager] '{enemy.enemyName}' 등록 완료.");
        }

        // ← 적 등록 완료 후 HP 절반 디버프 적용
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.halveNextEnemyHP)
        {
            foreach (var e in activeEnemies)
            {
                e.SetMaxHP(e.maxHP / 2);
            }
            PlayerDataManager.Instance.halveNextEnemyHP = false;
        }

        _initialized = true; // ← 맨 마지막에
        Logger.Log($"[EnemyManager] 초기화 완료. 등록된 적: {activeEnemies.Count}명", this);
    }

    // ── 행동 ─────────────────────────────────────────────────────

    /// <summary>모든 적이 순서대로 행동하는 코루틴.</summary>
    public IEnumerator PerformEnemyActionsCoroutine()
    {
        // 반복 중 리스트 변경에 대비해 스냅샷 사용
        var snapshot = new List<Enemy>(activeEnemies);

        foreach (Enemy enemy in snapshot)
        {
            if (enemy == null) continue;

            yield return new WaitForSeconds(0.5f);
            enemy.PerformAction();
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void PerformEnemyActions() => StartCoroutine(PerformEnemyActionsCoroutine());

    // ── 제거 ─────────────────────────────────────────────────────

    /// <summary>패배한 적을 목록에서 제거한다. 씬에 배치된 오브젝트는 파괴하지 않고 비활성화.</summary>
    public void RemoveEnemy(Enemy enemy)
    {
        if (!activeEnemies.Contains(enemy)) return;

        activeEnemies.Remove(enemy);

        // 유물 효과 트리거: 적 처치 시 (성자의 손가락 뼈 등)
        RelicManager.Instance?.OnEnemyKilled();

        // 파괴 대신 비활성화 (씬에 미리 배치된 오브젝트이므로)
        if (enemy != null)
            enemy.gameObject.SetActive(false);

        Logger.Log($"[EnemyManager] '{enemy.enemyName}' 제거됨.", this);

        // 모든 적이 사망하면 승리
        if (activeEnemies.Count == 0)
        {
            Logger.Log("[EnemyManager] 모든 적 사망. 배틀 승리.", this);
            BattleManager.Instance.HandleBattleVictory();
        }
    }

    /// <summary>특정 적에게 데미지를 직접 적용한다.</summary>
    public void ApplyDamageToEnemy(Enemy targetEnemy, int damage)
    {
        if (targetEnemy == null)
        {
            Logger.LogWarning("[EnemyManager] 대상 적이 null입니다.", this);
            return;
        }
        targetEnemy.TakeDamage(damage);
    }

    /// <summary>현재 활성화된 적 중 무작위 하나를 반환한다.</summary>
    public Enemy GetRandomActiveEnemy()
    {
        if (activeEnemies.Count == 0) return null;
        return activeEnemies[Random.Range(0, activeEnemies.Count)];
    }

    // ── 조회 ─────────────────────────────────────────────────────

    /// <summary>현재 활성 적 목록의 복사본을 반환한다.</summary>
    public List<Enemy> GetActiveEnemies() => new List<Enemy>(activeEnemies);

    /// <summary>활성 적 목록을 직접 참조한다 (TurnManager 인텐트 갱신용).</summary>
    public List<Enemy> Enemies => activeEnemies;

    // ── 유틸 ─────────────────────────────────────────────────────

    private void ClearExistingEnemies()
    {
        // 파괴 대신 비활성화
        foreach (var e in activeEnemies)
            if (e != null) e.gameObject.SetActive(false);

        activeEnemies.Clear();
    }
}