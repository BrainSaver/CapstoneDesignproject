using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MapVisualizer : MonoBehaviour
{
    public MapGenerator generator;
    public GameObject nodePrefab;
    public GameObject linePrefab;
    public Transform mapParent;

    [Header("지도 고정 영역 설정")]
    public Vector2 mapAreaSize = new Vector2(900f, 300f);
    public RectTransform mapBackground;

    [Header("노드 스프라이트 설정")]
    public Sprite startSprite;
    public Sprite monsterSprite;
    public Sprite shopSprite;
    public Sprite eventSprite;
    public Sprite healingSprite;
    public Sprite treasureSprite;
    public Sprite bossSprite;
    public Sprite defaultSprite;

    public List<MapNodeUI> allNodeUIs = new List<MapNodeUI>();

    private float currentXSpacing;
    private float currentYSpacing;

    void Start() { RegenerateMap(); }

    void Update()
    {
        bool rPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) rPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.R)) rPressed = true;
#endif
        if (rPressed)
        {
            // R키를 누르면 완전히 새로운 게임판을 짜기 위해 세이브 데이터를 초기화하고 맵을 새로 깝니다.
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.savedMapNodes = null;
                PlayerDataManager.Instance.playerMapPos = new Vector2Int(-1, -1);
            }
            RegenerateMap();
        }
    }

    public void RegenerateMap()
    {
        allNodeUIs.Clear();

        if (mapParent != null)
        {
            foreach (Transform child in mapParent)
            {
                if (mapBackground != null && child == mapBackground) continue;
                // 💡 단일 플레이어 마커를 쓰지 않으므로 자식 오브젝트들을 깔끔하게 전부 정리합니다.
                Destroy(child.gameObject);
            }
        }

        // 데이터 매니저에 이미 저장된 지도가 있으면 그대로 쓰고, 없으면 새로 만듭니다.
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.savedMapNodes != null)
        {
            generator.nodesByFloor = PlayerDataManager.Instance.savedMapNodes;
        }
        else
        {
            generator.Generate();
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.savedMapNodes = generator.nodesByFloor;
            }
        }

        currentXSpacing = mapAreaSize.x / Mathf.Max(1, generator.height - 1);
        currentYSpacing = mapAreaSize.y / Mathf.Max(1, generator.width - 1);

        DrawMap();
    }

    void DrawMap()
    {
        float startX = -mapAreaSize.x / 2f;
        float startY = -(generator.width - 1) * currentYSpacing / 2f;

        for (int x = 0; x < generator.height - 1; x++)
        {
            foreach (var node in generator.nodesByFloor[x])
            {
                if (node.type == RoomType.None) continue;
                foreach (var next in node.nextNodes) DrawLine(node, next, startX, startY);
            }
        }

        for (int x = 0; x < generator.height; x++)
        {
            foreach (var node in generator.nodesByFloor[x])
            {
                if (node.type == RoomType.None) continue;
                GameObject go = Instantiate(nodePrefab, mapParent);
                RectTransform rt = go.GetComponent<RectTransform>();

                MapNodeUI nodeUI = go.GetComponent<MapNodeUI>();
                if (nodeUI == null)
                {
                    nodeUI = go.AddComponent<MapNodeUI>();
                }

                nodeUI.Setup(node);
                allNodeUIs.Add(nodeUI);

                rt.anchoredPosition = new Vector2(startX + (x * currentXSpacing), startY + (node.pos.y * currentYSpacing));

                Image nodeImage = go.GetComponent<Image>();
                nodeImage.sprite = GetSprite(node.type);
                nodeImage.color = Color.white;
            }
        }

        if (mapBackground != null) mapBackground.SetAsFirstSibling();

        if (MapManager.Instance != null)
        {
            MapManager.Instance.UpdateInteractableNodes(allNodeUIs);
            MapManager.Instance.RestoreScrollPosition(); // ★ 저장된 위치 복구
        }
    }

    void DrawLine(MapNode start, MapNode end, float startX, float startY)
    {
        Vector2 sPos = new Vector2(startX + (start.pos.x * currentXSpacing), startY + (start.pos.y * currentYSpacing));
        Vector2 ePos = new Vector2(startX + (end.pos.x * currentXSpacing), startY + (end.pos.y * currentYSpacing));
        Vector2 dir = ePos - sPos;

        GameObject line = Instantiate(linePrefab, mapParent);
        line.transform.SetAsFirstSibling();

        RectTransform rt = line.GetComponent<RectTransform>();
        rt.anchoredPosition = sPos + dir / 2f;
        rt.sizeDelta = new Vector2(dir.magnitude, 5f);
        rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    Sprite GetSprite(RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return startSprite;
            case RoomType.Monster: return monsterSprite;
            case RoomType.Shop: return shopSprite;
            case RoomType.Event: return eventSprite;
            case RoomType.Healing: return healingSprite;
            case RoomType.Treasure: return treasureSprite;
            case RoomType.Boss: return bossSprite;
            default: return defaultSprite;
        }
    }
}
