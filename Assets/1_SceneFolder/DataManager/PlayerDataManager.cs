using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// 인스펙터에서 드롭다운 표시를 활성화하기 위한 커스텀 속성(Attribute)
public class RelicIdAttribute : PropertyAttribute { }

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance;

    [Header("Player Stats")]
    public int maxHP = 80;
    public int currentHP = 80;
    public int currentGold = 99;

    public List<RelicData> currentRelics = new List<RelicData>();
    public List<ConsecrationItemData> currentConsecration = new List<ConsecrationItemData>();

    [Header("테스트용 시작 유물 지급")]
    [Tooltip("드롭다운에서 유물을 선택하면 게임 시작 시 자동으로 지급됩니다.")]
    [RelicId]
    public List<string> startingRelicIDs = new List<string>();

    [Header("테스트용 시작 축성물 지급")]
    [Tooltip("축성물 ID를 입력하면 게임 시작 시 자동으로 지급됩니다.")]
    public List<string> startingConsecrationIDs = new List<string>();

    [Header("Saved Map Data")]
    public List<List<MapNode>> savedMapNodes = null;
    public Vector2Int playerMapPos = new Vector2Int(-1, -1);

    [Header("다음 전투 디버프")]
    public bool halveNextEnemyHP = false; // 다음 전투 적 HP 절반

    [Header("★ 이벤트 버프 패시브")]
    public bool guaranteeRelicInNextEventRoom = false; // 다음 ?방에서 유물방 확정 플래그

    public event Action<int, int> OnHPChanged;
    public event Action<int> OnGoldChanged;
    public event Action<List<RelicData>> OnRelicsChanged;
    public event Action<List<ConsecrationItemData>> OnConsecrationChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureRelicManagerExists();

            Debug.Log($"[PlayerDataManager] Instance initialized. HP: {currentHP}, Gold: {currentGold}");
        }
        else
        {
            Debug.Log("[PlayerDataManager] Duplicate instance destroyed.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 시작 유물 자동 지급
        foreach (string relicId in startingRelicIDs)
            if (!string.IsNullOrEmpty(relicId)) AddRelic(relicId);

        // 시작 축성물 자동 지급
        foreach (string consecrationId in startingConsecrationIDs)
            if (!string.IsNullOrEmpty(consecrationId)) AddConsecration(consecrationId);

        UpdateAllUI();
    }

    // ── 유물 ────────────────────────────────────────────────────

    public void AddRelic(string itemId)
    {
        if (RelicManager.Instance != null)
        {
            RelicManager.Instance.AddRelicToPlayer(itemId);
            UpdateAllUI();
        }
        else
        {
            Debug.LogError("[PlayerDataManager] RelicManager 인스턴스를 찾을 수 없어 유물을 추가할 수 없습니다.");
        }
    }

    public RelicData GetRelicData(string itemId)
    {
        if (RelicManager.Instance != null && RelicManager.Instance.relicDatabase != null)
            return RelicManager.Instance.relicDatabase.GetRelic(itemId);
        return null;
    }

    public List<RelicData> GetAllRelics()
    {
        if (RelicManager.Instance != null && RelicManager.Instance.relicDatabase != null)
            return RelicManager.Instance.relicDatabase.relics;
        return new List<RelicData>();
    }

    // ── 축성물 ───────────────────────────────────────────────────

    public void AddConsecration(string itemId)
    {
        if (ConsecrationItemManager.Instance != null)
        {
            bool success = ConsecrationItemManager.Instance.AddPotion(itemId);

            if (success)
            {
                currentConsecration.Clear();
                currentConsecration.AddRange(ConsecrationItemManager.Instance.Potions);
                UpdateAllUI();
            }
        }
        else
        {
            Debug.LogError("[PlayerDataManager] ConsecrationItemManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    public void SyncConsecration()
    {
        if (ConsecrationItemManager.Instance != null)
        {
            currentConsecration.Clear();
            currentConsecration.AddRange(ConsecrationItemManager.Instance.Potions);
            OnConsecrationChanged?.Invoke(currentConsecration);
        }
    }

    // ── HP / Gold ────────────────────────────────────────────────

    public void ModifyHP(int amount)
    {
        currentHP += amount;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        if (amount < 0 && RelicManager.Instance != null)
        {
            RelicManager.Instance.OnPlayerHPLost();
        }

        Debug.Log($"[PlayerDataManager] HP Modified: {amount} -> Current HP: {currentHP}");
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void ModifyMaxHP(int amount)
    {
        maxHP += amount;
        maxHP = Mathf.Max(1, maxHP);
        currentHP = Mathf.Min(currentHP, maxHP);
        Debug.Log($"[PlayerDataManager] MaxHP Modified: {amount} -> Max HP: {maxHP}");
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void AddGold(int amount)
    {
        // 유물 효과 적용 (행운의 물통 등 골드 획득량 증가)
        if (amount > 0 && RelicManager.Instance != null)
        {
            RelicManager.Instance.OnGoldGained(ref amount);
        }

        currentGold += amount;
        Debug.Log($"[PlayerDataManager] Gold Added: {amount} -> Current Gold: {currentGold}");
        OnGoldChanged?.Invoke(currentGold);
    }

    // ── 지도 ─────────────────────────────────────────────────────

    public void TeleportToBeforeBossFloor()
    {
        if (savedMapNodes == null || savedMapNodes.Count < 2)
        {
            Debug.LogWarning("저장된 지도 데이터가 없어 텔레포트할 수 없습니다.");
            return;
        }

        int targetFloorX = savedMapNodes.Count - 3;

        List<MapNode> validNodes = new List<MapNode>();
        foreach (var node in savedMapNodes[targetFloorX])
        {
            if (node.type != RoomType.None)
                validNodes.Add(node);
        }

        if (validNodes.Count > 0)
        {
            playerMapPos = validNodes[0].pos;
            Debug.Log("보스 전방 노드로 좌표 강제 워프 완료: " + playerMapPos);
        }
        else
        {
            Debug.LogError("보스 전방 층에서 유효한 노드를 찾을 수 없습니다.");
        }
    }

    // ── UI 갱신 ──────────────────────────────────────────────────

    public void UpdateAllUI()
    {
        Debug.Log($"[PlayerDataManager] UpdateAllUI called. HP: {currentHP}/{maxHP}, Gold: {currentGold}");
        OnHPChanged?.Invoke(currentHP, maxHP);
        OnGoldChanged?.Invoke(currentGold);
        OnRelicsChanged?.Invoke(currentRelics);
        OnConsecrationChanged?.Invoke(currentConsecration);
    }

    private void EnsureRelicManagerExists()
    {
        if (RelicManager.Instance == null)
        {
            GameObject go = new GameObject("RelicManager");
            go.AddComponent<RelicManager>();
            Debug.Log("[PlayerDataManager] RelicManager created automatically.");
        }
    }
}

// =====================================================================================
// ★ 유니티 인스펙터 창에서 유물 ID를 드롭다운으로 편하게 고르게 해주는 에디터 스크립트 구역
// =====================================================================================
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(RelicIdAttribute))]
public class RelicIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            List<string> idList = new List<string> { "" }; // 빈 값 기본 추가
            
            if (PlayerDataManager.Instance != null)
            {
                var allRelics = PlayerDataManager.Instance.GetAllRelics();
                foreach (var relic in allRelics)
                {
                    if (relic != null && !string.IsNullOrEmpty(relic.itemId))
                        idList.Add(relic.itemId);
                }
            }
            else
            {
                // 플레이 모드가 아닐 때 데이터베이스 직접 로드 폴백
                RelicDatabase db = AssetDatabase.LoadAssetAtPath<RelicDatabase>("Assets/1_SceneFolder/DataManager/Relic/RelicData/RelicDatabase.asset");
                if (db == null)
                {
                    // 위 경로에서 못 찾을 시 프로젝트 내 전체 검색 후 자동 매핑
                    string[] guids = AssetDatabase.FindAssets("t:RelicDatabase");
                    if (guids.Length > 0)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        db = AssetDatabase.LoadAssetAtPath<RelicDatabase>(path);
                    }
                }

                if (db != null && db.relics != null)
                {
                    foreach (var relic in db.relics)
                    {
                        if (relic != null && !string.IsNullOrEmpty(relic.itemId))
                            idList.Add(relic.itemId);
                    }
                }
            }

            int currentIndex = Mathf.Max(0, idList.IndexOf(property.stringValue));
            int nextIndex = EditorGUI.Popup(position, label.text, currentIndex, idList.ToArray());
            property.stringValue = idList[nextIndex];
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif