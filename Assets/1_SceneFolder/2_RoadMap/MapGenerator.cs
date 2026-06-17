using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoomType { None, Monster, Shop, Event, Healing, Treasure, Boss, Start }

public class MapNode
{
    public Vector2Int pos;
    public RoomType type = RoomType.None;
    public List<MapNode> nextNodes = new List<MapNode>();
    public bool visited = false;
}

public class MapGenerator : MonoBehaviour
{
    [Header("맵 생성 설정")]
    public int width = 7;
    public int height = 12;
    public List<List<MapNode>> nodesByFloor;

    [Header("UI 자동 정렬 설정")]
    [Tooltip("맵 노드들이 자식으로 생성되는 빈 UI 객체 (MapContent)")]
    public RectTransform mapContent;
    [Tooltip("최상위 캔버스의 RectTransform")]
    public RectTransform canvasRect;
    [Tooltip("화면 가장자리와 맵 사이의 여백")]
    public float padding = 100f;

    public void Generate()
    {
        do
        {
            InitGrid();
            CreatePaths(4);
            RemoveOrphans();
        }
        while (GetActiveNodeCount(1) < 2);

        AssignRooms();
        EnsureMinimumRooms();
    }

    int GetActiveNodeCount(int floor)
    {
        return nodesByFloor[floor].Count(node => node.nextNodes.Count > 0);
    }

    void InitGrid()
    {
        nodesByFloor = new List<List<MapNode>>();
        for (int x = 0; x < height; x++)
        {
            var floor = new List<MapNode>();
            for (int y = 0; y < width; y++)
            {
                floor.Add(new MapNode { pos = new Vector2Int(x, y) });
            }
            nodesByFloor.Add(floor);
        }
    }

    void CreatePaths(int pathCount)
    {
        int startY = width / 2;
        int bossY = width / 2;

        nodesByFloor[0][startY].type = RoomType.Start;
        int[] pathsY = Enumerable.Repeat(startY, pathCount).ToArray();

        for (int x = 0; x < height - 1; x++)
        {
            int[] nextY = new int[pathCount];
            for (int i = 0; i < pathCount; i++)
            {
                if (x == height - 2)
                {
                    nextY[i] = bossY;
                }
                else
                {
                    int current = pathsY[i];
                    int min = Mathf.Max(0, current - 1);
                    int max = Mathf.Min(width - 1, current + 1);
                    if (i > 0) min = Mathf.Max(min, nextY[i - 1]);
                    nextY[i] = Random.Range(min, max + 1);
                }
                nodesByFloor[x][pathsY[i]].nextNodes.Add(nodesByFloor[x + 1][nextY[i]]);
            }
            pathsY = nextY;
        }
    }

    void RemoveOrphans()
    {
        HashSet<MapNode> visited = new HashSet<MapNode>();
        foreach (var node in nodesByFloor[0])
        {
            if (node.type == RoomType.Start) Traverse(node, visited);
        }
        for (int x = 0; x < height; x++)
        {
            foreach (var node in nodesByFloor[x])
            {
                if (!visited.Contains(node))
                {
                    node.type = RoomType.None;
                    node.nextNodes.Clear();
                }
            }
        }
    }

    void Traverse(MapNode node, HashSet<MapNode> visited)
    {
        if (visited.Contains(node)) return;
        visited.Add(node);
        foreach (var next in node.nextNodes) Traverse(next, visited);
    }

    void AssignRooms()
    {
        nodesByFloor[height - 1][width / 2].type = RoomType.Boss;

        for (int x = 1; x < height - 1; x++)
        {
            foreach (var node in nodesByFloor[x])
            {
                if (node.nextNodes.Count == 0) continue;

                if (x == 1)
                    node.type = RoomType.Monster;
                else if (x == 5)
                    node.type = RoomType.Treasure;
                else if (x == height - 2)
                    node.type = RoomType.Healing;
                else
                    node.type = GetValidRandomType(node, x);
            }
        }
    }

    void EnsureMinimumRooms()
    {
        bool hasShop = false;
        List<MapNode> validCandidates = new List<MapNode>();

        for (int x = 2; x < height - 2; x++)
        {
            if (x == 5) continue;

            foreach (var node in nodesByFloor[x])
            {
                if (node.nextNodes.Count > 0)
                {
                    if (node.type == RoomType.Shop)
                    {
                        hasShop = true;
                    }
                    validCandidates.Add(node);
                }
            }
        }

        if (!hasShop && validCandidates.Count > 0)
        {
            int randIdx = Random.Range(0, validCandidates.Count);
            validCandidates[randIdx].type = RoomType.Shop;
        }
    }

    RoomType GetValidRandomType(MapNode node, int floorIndex)
    {
        bool canBeShop = !HasParentOfType(node, RoomType.Shop);
        bool isJustBeforeFixedHealing = (floorIndex == height - 3);
        bool canBeHealing = (floorIndex >= 4) && !HasParentOfType(node, RoomType.Healing) && !isJustBeforeFixedHealing;

        float mWeight = 49f;
        float eWeight = 26f;
        float sWeight = canBeShop ? 9f : 0f;
        float hWeight = canBeHealing ? 16f : 0f;

        float total = mWeight + eWeight + sWeight + hWeight;
        float roll = Random.Range(0f, total);

        if (roll < mWeight) return RoomType.Monster;
        if (roll < mWeight + eWeight) return RoomType.Event;
        if (canBeShop && roll < mWeight + eWeight + sWeight) return RoomType.Shop;
        return RoomType.Healing;
    }

    bool HasParentOfType(MapNode node, RoomType type)
    {
        int prevX = node.pos.x - 1;
        if (prevX < 0) return false;
        foreach (var p in nodesByFloor[prevX])
        {
            if (p.nextNodes.Contains(node) && p.type == type) return true;
        }
        return false;
    }

    /// <summary>
    /// 화면 캔버스 크기에 맞춰 맵 UI의 스케일을 줄이고 좌측으로 정렬합니다.
    /// </summary>
    public void FitMapToCanvas()
    {
        if (mapContent == null || canvasRect == null || mapContent.childCount == 0)
        {
            Debug.LogWarning("정렬할 맵 노드가 없거나 UI(MapContent, Canvas) 참조가 누락되었습니다.");
            return;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // 1. 맵 콘텐츠 하위에 생성된 모든 자식(노드) 객체의 좌표를 파악합니다.
        foreach (RectTransform child in mapContent)
        {
            Vector2 pos = child.anchoredPosition;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        float mapHeight = maxY - minY;

        // 2. 캔버스의 높이와 여백(padding)에 맞춰 적절한 스케일 비율을 계산합니다.
        float targetHeight = canvasRect.rect.height - (padding * 2);
        float scaleRatio = mapHeight > 0 ? targetHeight / mapHeight : 1f;

        mapContent.localScale = new Vector3(scaleRatio, scaleRatio, 1f);

        // 3. 시작점(가장 왼쪽)이 화면 좌측 여백에 닿고, 전체 맵이 상하 중앙에 오도록 이동시킵니다.
        float startXPosition = -(canvasRect.rect.width / 2f) + padding;
        float centerYOffset = -(minY + maxY) / 2f * scaleRatio;

        mapContent.anchoredPosition = new Vector2(startXPosition - (minX * scaleRatio), centerYOffset);
    }
}
