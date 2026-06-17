using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class TopBarUIManager : MonoBehaviour
{
    public static TopBarUIManager Instance;

    [Header("UI References")]
    public GameObject topBarPanel;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI goldText;

    [Header("Relic UI References")]
    public Transform relicContainer;
    public GameObject relicIconPrefab;

    [Header("ConsecrationItem UI References")]
    public Transform ConsecrationItemPanel;
    public GameObject ConsecrationItemIconPrefab;

    [Header("Scene Settings")]
    public List<string> hiddenSceneNames = new List<string> { "TitleScene" };
    public TMP_FontAsset tooltipFont;

    // ✅ 슬롯 목록을 멤버 변수로 유지 (재생성 방지)
    private readonly List<ConsecrationItemSlotUI> consecrationSlots = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (GetComponent<TooltipManager>() == null)
            {
                TooltipManager tm = gameObject.AddComponent<TooltipManager>();
                tm.fontAsset = tooltipFont;
            }
            
            Debug.Log("[TopBarUIManager] 메인 인스턴스가 생성되고 유지됩니다.");
        }
        else
        {
            Debug.Log("[TopBarUIManager] 중복된 UI 캔버스를 파괴합니다.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitConsecrationSlots();
        SubscribeEvents();
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnHPChanged -= UpdateHPUI;
                PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldUI;
                PlayerDataManager.Instance.OnRelicsChanged -= UpdateRelicUI;
                PlayerDataManager.Instance.OnConsecrationChanged -= UpdateConsecrationUI;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TopBarUIManager] Scene Loaded: {scene.name}");

        if (hiddenSceneNames.Contains(scene.name))
        {
            if (topBarPanel != null) topBarPanel.SetActive(false);
        }
        else
        {
            if (topBarPanel != null) topBarPanel.SetActive(true);

            if (PlayerDataManager.Instance != null)
                PlayerDataManager.Instance.UpdateAllUI();
        }
    }

    public void SubscribeEvents()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnHPChanged -= UpdateHPUI;
            PlayerDataManager.Instance.OnGoldChanged -= UpdateGoldUI;
            PlayerDataManager.Instance.OnRelicsChanged -= UpdateRelicUI;
            PlayerDataManager.Instance.OnConsecrationChanged -= UpdateConsecrationUI;

            PlayerDataManager.Instance.OnHPChanged += UpdateHPUI;
            PlayerDataManager.Instance.OnGoldChanged += UpdateGoldUI;
            PlayerDataManager.Instance.OnRelicsChanged += UpdateRelicUI;
            PlayerDataManager.Instance.OnConsecrationChanged += UpdateConsecrationUI;

            PlayerDataManager.Instance.UpdateAllUI();
        }
    }

    // ── 슬롯 초기화 ─────────────────────────────────────────────

    /// <summary>축성물 슬롯을 최초 1회 생성한다.</summary>
    private void InitConsecrationSlots()
    {
        if (ConsecrationItemPanel == null || ConsecrationItemIconPrefab == null) return;

        // 이미 슬롯이 있으면 수집만 함 (중복 생성 방지)
        consecrationSlots.Clear();
        foreach (Transform child in ConsecrationItemPanel)
        {
            var slot = child.GetComponent<ConsecrationItemSlotUI>();
            if (slot != null) consecrationSlots.Add(slot);
        }

        int maxSlots = ConsecrationItemManager.Instance != null
            ? ConsecrationItemManager.Instance.MaxSlots : 3;

        // 부족한 수만큼만 생성
        while (consecrationSlots.Count < maxSlots)
        {
            GameObject newIcon = Instantiate(ConsecrationItemIconPrefab, ConsecrationItemPanel);
            var slot = newIcon.GetComponent<ConsecrationItemSlotUI>();
            if (slot != null) consecrationSlots.Add(slot);
        }
    }

    // ── UI 갱신 ──────────────────────────────────────────────────

    private void UpdateHPUI(int current, int max)
    {
        if (hpText != null) hpText.text = $"{current} / {max}";
    }

    private void UpdateGoldUI(int gold)
    {
        if (goldText != null) goldText.text = $"{gold}";
    }

    private void UpdateRelicUI(List<RelicData> relics)
    {
        if (relicContainer == null || relicIconPrefab == null) return;

        foreach (Transform child in relicContainer)
            Destroy(child.gameObject);

        foreach (RelicData relic in relics)
        {
            if (relic == null || relic.relicIcon == null) continue;

            GameObject newIcon = Instantiate(relicIconPrefab, relicContainer);
            Image iconImage = newIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = relic.relicIcon;
                iconImage.color = Color.white;
            }

            HoverTooltip tooltip = newIcon.GetComponent<HoverTooltip>();
            if (tooltip == null) tooltip = newIcon.AddComponent<HoverTooltip>();
            tooltip.title = relic.nameKo;
            tooltip.description = relic.description;
        }
    }

    /// <summary>축성물 슬롯에 데이터를 할당한다. 슬롯은 재생성하지 않는다.</summary>
    private void UpdateConsecrationUI(List<ConsecrationItemData> consecrations)
    {
        if (ConsecrationItemPanel == null || ConsecrationItemIconPrefab == null) return;

        // 슬롯이 아직 없으면 초기화 (안전장치)
        if (consecrationSlots.Count == 0)
            InitConsecrationSlots();

        // ✅ 슬롯 재생성 없이 데이터만 교체
        for (int i = 0; i < consecrationSlots.Count; i++)
        {
            if (consecrationSlots[i] == null) continue;

            HoverTooltip tooltip = consecrationSlots[i].GetComponent<HoverTooltip>();
            if (tooltip == null) tooltip = consecrationSlots[i].gameObject.AddComponent<HoverTooltip>();

            if (i < consecrations.Count)
            {
                consecrationSlots[i].SetPotion(consecrations[i]);
                tooltip.title = consecrations[i].nameKo;
                tooltip.description = consecrations[i].description;
                tooltip.enabled = true;
            }
            else
            {
                consecrationSlots[i].SetPotion(null);
                tooltip.title = "";
                tooltip.description = "";
                tooltip.enabled = false;
            }
        }
    }
}