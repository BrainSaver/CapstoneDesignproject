using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 축성물 데이터 로드 및 플레이어 보유 축성물을 관리한다.
/// DontDestroyOnLoad로 씬 전환에도 유지된다.
/// </summary>
public class ConsecrationItemManager : MonoBehaviour
{
    public static ConsecrationItemManager Instance { get; private set; }

    [Header("축성물 슬롯 설정")]
    [SerializeField] private int maxSlots = 3;

    [Header("축성물 데이터베이스")]
    public ConsecrationItemDatabase database; // Inspector에서 연결

    public int MaxSlots => maxSlots;

    private Dictionary<string, ConsecrationItemData> allPotions = new();
    private readonly List<ConsecrationItemData> potions = new();
    public IReadOnlyList<ConsecrationItemData> Potions => potions;

    public static event Action OnPotionsChanged;

    /// <summary>보유하지 않은 축성물 중 랜덤으로 획득.</summary>
    public void AddRandomPotionToPlayer(int amount)
    {
        if (database == null || database.items == null) return;
        if (potions.Count >= maxSlots)
        {
            Logger.Log("[ConsecrationItemManager] 슬롯이 가득 차 무작위 축성물을 얻지 못했습니다.");
            return;
        }

        List<string> availableItemIds = new List<string>();
        foreach (var item in database.items)
        {
            if (item != null && !potions.Exists(p => p.ConsecrationItemId == item.ConsecrationItemId))
                availableItemIds.Add(item.ConsecrationItemId);
        }

        if (availableItemIds.Count == 0)
        {
            Logger.Log("[ConsecrationItemManager] 더 이상 획득할 수 있는 새로운 축성물이 없습니다.");
            return;
        }

        int spaceLeft = maxSlots - potions.Count;
        int rewardCount = Mathf.Min(amount, Mathf.Min(spaceLeft, availableItemIds.Count));

        for (int i = 0; i < rewardCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableItemIds.Count);
            string selectedId = availableItemIds[randomIndex];
            AddPotion(selectedId);
            availableItemIds.RemoveAt(randomIndex);
        }
    }

    /// <summary>보유 중인 축성물 중 랜덤으로 분실.</summary>
    public void RemoveRandomPotionFromPlayer(int amount)
    {
        if (potions.Count == 0)
        {
            Logger.Log("[ConsecrationItemManager] 잃을 축성물이 없습니다.");
            return;
        }

        int removeCount = Mathf.Min(amount, potions.Count);
        for (int i = 0; i < removeCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, potions.Count);
            ConsecrationItemData target = potions[randomIndex];
            Logger.Log($"[ConsecrationItemManager] 축성물 분실: {target.nameKo} ({target.ConsecrationItemId})");
            potions.RemoveAt(randomIndex);
        }

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.currentConsecration.Clear();
            PlayerDataManager.Instance.currentConsecration.AddRange(potions);
            PlayerDataManager.Instance.UpdateAllUI();
        }

        OnPotionsChanged?.Invoke();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Logger.Log($"[ConsecrationItemManager] Awake. database={database}");
            LoadPotionData();
            Logger.Log($"[ConsecrationItemManager] 로드 후 allPotions 수={allPotions.Count}");
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        // 테스트용 — PlayerDataManager.startingConsecrationIDs로 대체됐으므로 비워둠
    }

    // ── 데이터 로드 ──────────────────────────────────────────────

    private void LoadPotionData()
    {
        allPotions.Clear();

        if (database != null)
            LoadFromDatabase();
        else
            LoadFromJSON();
    }

    private void LoadFromDatabase()
    {
        database.Initialize();

        foreach (var item in database.items)
        {
            if (item == null) continue;
            if (item.ConsecrationItemIcon == null)
                item.ConsecrationItemIcon = Resources.Load<Sprite>($"Potions/{item.ConsecrationItemId}");
            allPotions[item.ConsecrationItemId] = item;
        }

        Logger.Log($"[ConsecrationItemManager] 데이터베이스 로드 완료: {allPotions.Count}개");
    }

    private void LoadFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("potion");
        if (jsonFile == null)
        {
            Logger.LogError("[ConsecrationItemManager] potion.json을 찾을 수 없습니다.");
            return;
        }

        ConsecrationItemDataList list = JsonUtility.FromJson<ConsecrationItemDataList>(jsonFile.text);
        if (list?.ConsecrationItem == null) return;

        foreach (var item in list.ConsecrationItem)
        {
            item.ConsecrationItemIcon = Resources.Load<Sprite>($"Potions/{item.ConsecrationItemId}");
            allPotions[item.ConsecrationItemId] = item;
        }

        Logger.Log($"[ConsecrationItemManager] JSON 로드 완료: {allPotions.Count}개");
    }

    // ── 축성물 관리 ──────────────────────────────────────────────

    public ConsecrationItemData GetPotionData(string potionId)
    {
        return allPotions.TryGetValue(potionId, out var data) ? data : null;
    }

    public bool AddPotion(string potionId)
    {
        ConsecrationItemData potion = GetPotionData(potionId);
        if (potion == null)
        {
            Logger.LogError($"[ConsecrationItemManager] '{potionId}' 축성물을 찾을 수 없습니다.");
            return false;
        }
        return AddPotion(potion);
    }

    public bool AddPotion(ConsecrationItemData potion)
    {
        if (potion == null) return false;

        if (potions.Count >= maxSlots)
        {
            Logger.Log("[ConsecrationItemManager] 축성물 슬롯이 가득 찼습니다.");
            return false;
        }

        potions.Add(potion);

        // PlayerDataManager에 직접 동기화
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.currentConsecration.Clear();
            PlayerDataManager.Instance.currentConsecration.AddRange(potions);
            PlayerDataManager.Instance.UpdateAllUI();
        }

        OnPotionsChanged?.Invoke();
        Logger.Log($"[ConsecrationItemManager] '{potion.nameKo}' 획득. {potions.Count}/{maxSlots}");
        return true;
    }

    public void RemovePotion(ConsecrationItemData potion)
    {
        if (potions.Remove(potion))
        {
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.currentConsecration.Clear();
                PlayerDataManager.Instance.currentConsecration.AddRange(potions);
                PlayerDataManager.Instance.UpdateAllUI();
            }

            OnPotionsChanged?.Invoke();
            Logger.Log($"[ConsecrationItemManager] '{potion.nameKo}' 제거.");
        }
    }

    public void UsePotion(ConsecrationItemData potion, Enemy target = null)
    {
        ConsecrationItemData found = potions.Find(p => p.ConsecrationItemId == potion.ConsecrationItemId);
        if (found == null)
        {
            Debug.LogWarning($"[ConsecrationItemManager] '{potion.ConsecrationItemId}' 슬롯에 없습니다.");
            return;
        }

        Logger.Log($"[ConsecrationItemManager] '{found.nameKo}' 사용.");
        StartCoroutine(ExecutePotionEffect(found, target));
        RemovePotion(found);
    }

    // ── 축성물 효과 ──────────────────────────────────────────────

    private IEnumerator ExecutePotionEffect(ConsecrationItemData potion, Enemy target)
    {
        switch (potion.ConsecrationItemId)
        {
            case "blessedBread":
                PlayerDataManager.Instance?.ModifyHP(10);
                break;

            case "stJohnsWort":
                PlayerDataManager.Instance?.ModifyHP(5);
                break;

            case "blessedAshes":
                PlayerStats.Instance?.AddArmor(15);
                break;

            case "aspergorium":
                target?.TakeDamage(10);
                break;

            case "blessedSilverCross":
                target?.LoseHealthDirect(15);
                break;

            case "inquisitorsJournal":
                target?.ApplyDullness(2);
                break;

            case "blessedShackles":
                target?.ApplyStun(1);
                break;

            case "purifyingHolyBell":
                foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                    e.TakeDamage(5);
                break;

            case "handfulOfConsecratedSalt":
                foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                    e.ApplyDullness(2);
                break;

            case "angelsTrumpet":
                foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                    e.ApplyStun(1);
                break;

            case "demonHorn":
                foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                    e.ApplyDullness(1);
                break;

            case "sootStainedCandelabrum":
                foreach (var e in EnemyManager.Instance.GetActiveEnemies())
                    e.TakeDamage(3);
                yield return StartCoroutine(HandManager.Instance.DrawCardsRoutine(1));
                break;

            case "monasteryRations":
                PlayerStats.Instance?.GainEnergy(1);
                yield return StartCoroutine(HandManager.Instance.DrawCardsRoutine(1));
                break;

            case "flagellantsWhip":
                PlayerDataManager.Instance?.ModifyHP(-3);
                PlayerStats.Instance?.GainEnergy(2);
                break;

            case "lumpOfFrankincense":
                yield return StartCoroutine(HandManager.Instance.DrawCardsRoutine(2));
                FreeCostTracker.AddFreeCard(null);
                break;

            case "martyrsReliquary":
                yield return StartCoroutine(HandManager.Instance.DrawCardsRoutine(3));
                break;

            case "featherOfSaintMichael":
                DistanceManager.Instance?.ResetDistance();
                yield return StartCoroutine(HandManager.Instance.DrawCardsRoutine(1));
                break;

            case "holyOil":
                FreeCostTracker.AddFreeCard(null);
                break;

            case "saintsHourglass":
                RetainHandTracker.Activate();
                break;

            case "demonicRemains":
                // ✅ StrengthTracker.AddStrength → PlayerStats.AddStrength으로 교체
                PlayerStats.Instance?.AddStrength(5);
                break;

            case "martyrsCloth":
                DamageReductionTracker.ApplyReduction(1f, 1);
                break;

            default:
                Logger.LogWarning($"[ConsecrationItemManager] '{potion.ConsecrationItemId}' 효과 미구현.");
                break;
        }

        yield return null;
    }

    // ── 슬롯 관리 ────────────────────────────────────────────────

    public void AddSlot(int amount = 1)
    {
        maxSlots += amount;
        OnPotionsChanged?.Invoke();
        Logger.Log($"[ConsecrationItemManager] 슬롯 +{amount}. 최대: {maxSlots}");
    }

    public bool IsFull => potions.Count >= maxSlots;

    public void Reset()
    {
        potions.Clear();

        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.currentConsecration.Clear();

        OnPotionsChanged?.Invoke();
        Logger.Log("[ConsecrationItemManager] 축성물 목록 초기화.");
    }
}