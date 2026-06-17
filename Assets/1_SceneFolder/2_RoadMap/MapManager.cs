using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;
    public MapVisualizer visualizer;

    [Header("Scroll Persistence")]
    [Tooltip("위치를 저장하고 복구할 로드맵 컨테이너(RectTransform)를 지정하세요.")]
    public RectTransform scrollTarget;
    private static float? savedX; // 정적 변수로 씬 전환 시 데이터 유지

    private bool isTransitioning = false;

    [Header("★ ?방 변환 확률 설정 (합계 100)")]
    [Tooltip("일반 전투방이 될 확률")][SerializeField] private int monsterRoomChance = 25;
    [Tooltip("이벤트방(랜덤스테이지)이 될 확률")][SerializeField] private int eventRoomChance = 45;
    [Tooltip("유물방(보물방)이 될 확률")][SerializeField] private int treasureRoomChance = 15;
    [Tooltip("상점방이 될 확률")][SerializeField] private int shopRoomChance = 15;
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// 현재 로드맵의 가로 스크롤 위치를 저장합니다.
    /// </summary>
    public void SaveScrollPosition()
    {
        if (scrollTarget != null)
        {
            savedX = scrollTarget.anchoredPosition.x;
        }
    }

    /// <summary>
    /// 저장된 로드맵의 가로 스크롤 위치를 복구합니다.
    /// </summary>
    public void RestoreScrollPosition()
    {
        if (savedX.HasValue && scrollTarget != null)
        {
            Vector2 pos = scrollTarget.anchoredPosition;
            pos.x = savedX.Value;
            scrollTarget.anchoredPosition = pos;

            // 드래그 컴포넌트가 있다면 관성 중지
            UIDragController drag = scrollTarget.GetComponent<UIDragController>();
            if (drag != null) drag.StopMovement();
        }
    }

    public void UpdateInteractableNodes(List<MapNodeUI> allNodes)
    {
        if (PlayerDataManager.Instance == null) return;

        Vector2Int currentPos = PlayerDataManager.Instance.playerMapPos;

        foreach (var nodeUI in allNodes)
        {
            bool isCurrent = (nodeUI.nodeData.pos == currentPos);
            bool isPassed = nodeUI.nodeData.visited || isCurrent;
            bool isClickable = false;

            if (currentPos.x == -1)
            {
                if (nodeUI.nodeData.type == RoomType.Start) isClickable = true;
            }
            else
            {
                MapNode currentNode = visualizer.generator.nodesByFloor[currentPos.x].Find(n => n.pos == currentPos);

                if (currentNode != null && currentNode.nextNodes.Contains(nodeUI.nodeData))
                {
                    isClickable = true;
                }
            }

            nodeUI.UpdateVisualState(isCurrent, isPassed, isClickable);
        }
    }

    public void OnNodeClicked(MapNodeUI clickedNodeUI)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        if (FistShot.Instance != null)
        {
            FistShot.Instance.PunchTo(clickedNodeUI.transform.position, () =>
            {
                clickedNodeUI.PlayBreakAnimation(() =>
                {
                    TransitionToNode(clickedNodeUI);
                });
            });
        }
        else
        {
            clickedNodeUI.PlayBreakAnimation(() =>
            {
                TransitionToNode(clickedNodeUI);
            });
        }
    }

    private void TransitionToNode(MapNodeUI clickedNodeUI)
    {
        RoomType finalRoomType = clickedNodeUI.nodeData.type;

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.playerMapPos = clickedNodeUI.nodeData.pos;
            clickedNodeUI.nodeData.visited = true;

            // ★ 핵심: 진입하려는 방이 ?방(Event)일 때 무작위 확률 변환 연산 진행
            if (clickedNodeUI.nodeData.type == RoomType.Event) { if (RelicManager.Instance != null) RelicManager.Instance.OnEnterEventNode(); }
            if (finalRoomType == RoomType.Event)
            {
                finalRoomType = DetermineRandomEventRoomType();
            }
        }

        isTransitioning = false;

        // ★ 로드맵 위치 저장
        SaveScrollPosition();

        LoadSceneByRoomType(finalRoomType);
    }

    /// <summary>
    /// ?방을 밟았을 때 패시브 버프 상태 및 확률 테이블을 연산하여 최종 이동할 방 타입을 결정합니다.
    /// </summary>
    private RoomType DetermineRandomEventRoomType()
    {
        // 1. 유물방 확정 버프 패시브가 켜져 있는지 먼저 검사합니다.
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.guaranteeRelicInNextEventRoom)
        {
            // 확정 플래그 사용 후 소모(해제) 처리
            PlayerDataManager.Instance.guaranteeRelicInNextEventRoom = false;
            Debug.Log("[MapManager] 패시브 발동: 다음 ?방이 유물방(보물방)으로 확정 변경되었습니다!");
            return RoomType.Treasure;
        }

        // 2. 버프가 없다면 인스펙터에 설정해 둔 확률(합산 100)을 기준으로 무작위 방 계산
        int roll = Random.Range(0, 100);

        // 일반 전투방 판단
        if (roll < monsterRoomChance)
            return RoomType.Monster;

        // 이벤트방 판단
        else if (roll < monsterRoomChance + eventRoomChance)
            return RoomType.Event;

        // 유물방(보물방) 판단
        else if (roll < monsterRoomChance + eventRoomChance + treasureRoomChance)
            return RoomType.Treasure;

        else // 상점방 판단
            return RoomType.Shop;
    }

    private void LoadSceneByRoomType(RoomType type)
    {
        string sceneName = "";

        switch (type)
        {
            case RoomType.Monster: sceneName = "BattleScene"; break;
            case RoomType.Shop: sceneName = "Shop"; break;
            case RoomType.Healing: sceneName = "Heal"; break;
            case RoomType.Event: sceneName = "RandomStage"; break;
            case RoomType.Treasure: sceneName = "ExorcismTool"; break; // 보물(유물) 획득 씬
            case RoomType.Boss: sceneName = "BossScene"; break;
            case RoomType.Start:
                UpdateInteractableNodes(visualizer.allNodeUIs);
                return;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
