using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance;

    [Header("Database")]
    public RelicDatabase relicDatabase;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private int previousPotionCount = 0;

    private void Start()
    {
        ConsecrationItemManager.OnPotionsChanged += HandlePotionsChanged;
    }

    private void OnDestroy()
    {
        ConsecrationItemManager.OnPotionsChanged -= HandlePotionsChanged;
    }

    private void HandlePotionsChanged()
    {
        if (ConsecrationItemManager.Instance == null) return;

        int currentCount = ConsecrationItemManager.Instance.Potions.Count;

        if (currentCount < previousPotionCount)
            OnPotionUsed();

        previousPotionCount = currentCount;
    }

    public void OnPotionUsed()
    {
        if (HasRelic("holyOilFlask"))
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.StartCoroutine(HandManager.Instance.DrawCardsRoutine(1));
                Debug.Log("[RelicManager] 성유병 효과 발동: 카드 1장 드로우!");
            }
        }
    }

    public bool HasRelic(string itemId)
    {
        if (PlayerDataManager.Instance == null) return false;
        return PlayerDataManager.Instance.currentRelics.Exists(r => r.itemId == itemId);
    }

    /// <summary>플레이어에게 유물을 지급한다.</summary>
    public void AddRelicToPlayer(string itemId)
    {
        if (PlayerDataManager.Instance == null) return;

        if (HasRelic(itemId))
        {
            Debug.Log($"[RelicManager] 이미 보유한 유물입니다: {itemId}");
            return;
        }

        RelicData newRelic = relicDatabase.GetRelic(itemId);

        if (newRelic != null)
        {
            PlayerDataManager.Instance.currentRelics.Add(newRelic);
            Debug.Log($"[RelicManager] 유물 획득: {newRelic.nameKo}");
            RefreshEffects();
            PlayerDataManager.Instance.UpdateAllUI();
        }
        else
        {
            Debug.LogWarning($"[RelicManager] 데이터베이스에 없는 유물 ID입니다: {itemId}");
        }
    }

    // ── 전투 훅 ──────────────────────────────────────────────────

    private bool giantPumpkinUsed = false;
    private bool embersUsed = false;
    private bool statusImmuneNextTurn = false;
    public bool IsStatusImmune => statusImmuneNextTurn;

    private int cardsExhaustedCount = 0;
    private int turnTotalCost = 0;
    private bool nothingBoughtInLastShop = false;
    private Card.CardType lastPlayedCardType = Card.CardType.Curse;
    private Card lastPlayedCard = null;
    private bool isPlayerTurn = false;
    private bool damagedWithoutArmorThisTurn = false;
    private bool tornCassockActiveNextTurn = false;
    private int battleTurnCount = 0;
    private int pilgrimsStrawShoesCount = 0;
    private int pilgrimsBonusStrength = 0;

    public void OnBattleStart()
    {
        cardsExhaustedCount = 0;
        giantPumpkinUsed = false;
        embersUsed = false;
        statusImmuneNextTurn = false;
        turnTotalCost = 0;
        lastPlayedCardType = Card.CardType.Curse;
        lastPlayedCard = null;
        isPlayerTurn = false;
        damagedWithoutArmorThisTurn = false;
        tornCassockActiveNextTurn = false;
        battleTurnCount = 0;

        // 순례자의 짚신: 이벤트 노드 방문 횟수만큼 힘 보너스
        if (HasRelic("pilgrimsStrawShoes"))
        {
            pilgrimsBonusStrength = pilgrimsStrawShoesCount;
            if (pilgrimsBonusStrength > 0)
                Debug.Log($"[RelicManager] 순례자의 짚신: 이번 전투 힘 보너스 +{pilgrimsBonusStrength} 설정");
        }
        else
        {
            pilgrimsBonusStrength = 0;
        }
        pilgrimsStrawShoesCount = 0;

        RefreshEffects();

        if (HasRelic("lostGospel")) StartCoroutine(LostGospelRoutine());

        if (HasRelic("waitingStatue"))
            DrawNextTurnTracker.Add(-2);
    }

    /// <summary>찢어진 수단: 데미지 배율 훅</summary>
    public float OnGetDamageMultiplier(Card card, bool consume = false)
    {
        if (card == null || card.cardType != Card.CardType.Attack) return 1f;

        if (HasRelic("tornCassock") && tornCassockActiveNextTurn)
        {
            if (consume)
            {
                tornCassockActiveNextTurn = false;
                Debug.Log("[RelicManager] 찢어진 수단 효과 소모: 데미지 50% 증가 적용!");
            }
            return 1.5f;
        }

        return 1f;
    }

    /// <summary>유물 효과로 카드 사용 불가 체크</summary>
    public bool OnCanPlayCard(Card card)
    {
        if (card == null) return true;

        if (HasRelic("heavyShackles"))
        {
            if (card.cardType == Card.CardType.Attack || card.cardType == Card.CardType.Skill)
            {
                if (card.cardType == lastPlayedCardType)
                    return false;
            }
        }

        return true;
    }

    /// <summary>유물의 영구/패시브 효과를 재계산한다.</summary>
    public void RefreshEffects()
    {
        ApplyGlobalRelicEffects();

        if (BattleManager.Instance != null && !BattleManager.Instance.IsBattleOver())
        {
            // ✅ StrengthTracker 제거 → ApplyPermanentRelicEffects에서 PlayerStats로 직접 적용
            ApplyPermanentRelicEffects();
            HandManager.Instance?.RefreshHandDisplay();
            Debug.Log("[RelicManager] 유물 효과 재계산 및 UI 갱신 완료.");
        }
    }

    private int basePotionSlots = -1;

    private void ApplyGlobalRelicEffects()
    {
        if (ConsecrationItemManager.Instance != null)
        {
            if (basePotionSlots == -1) basePotionSlots = ConsecrationItemManager.Instance.MaxSlots;

            int bonusSlots = 0;
            if (HasRelic("priestsBelt")) bonusSlots += 1;

            int targetSlots = basePotionSlots + bonusSlots;
            int currentMax = ConsecrationItemManager.Instance.MaxSlots;

            if (currentMax != targetSlots)
                ConsecrationItemManager.Instance.AddSlot(targetSlots - currentMax);
        }
    }

    private void ApplyPermanentRelicEffects()
    {
        if (PlayerStats.Instance == null) return;

        // 매 턴 재계산되는 유물 힘 보너스
        int bonusStrength = 0;
        if (HasRelic("warManual")) bonusStrength += 1;
        if (HasRelic("ominousGloves")) bonusStrength += 1;
        if (HasRelic("ominousRope")) bonusStrength -= 1;

        if (HasRelic("inquisitionRecord"))
        {
            int curseCount = 0;
            if (PlayerDeck.Instance != null)
                foreach (var card in PlayerDeck.Instance.GetDeck())
                    if (card != null && card.cardType == Card.CardType.Curse) curseCount++;
            bonusStrength += curseCount;
        }

        if (HasRelic("pilgrimsStrawShoes"))
            bonusStrength += pilgrimsBonusStrength;

        if (HasRelic("goldenDumbbell") && PlayerDataManager.Instance != null)
            bonusStrength += PlayerDataManager.Instance.currentGold / 300;

        // ✅ AddStrength 대신 SetRelicStrength 사용 (누적 방지)
        PlayerStats.Instance.SetRelicStrength(bonusStrength);

        // 매 턴 재계산되는 유물 민첩 보너스
        int bonusDexterity = 0;
        if (HasRelic("dexterityRelic")) bonusDexterity += 2;
        if (HasRelic("ominousGloves")) bonusDexterity -= 1;
        if (HasRelic("ominousRope")) bonusDexterity += 1;

        PlayerStats.Instance.SetRelicDexterity(bonusDexterity);
    }

    private IEnumerator LostGospelRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        var hand = HandManager.Instance?.CardsInHand;
        if (hand != null && hand.Count > 0)
        {
            var cardObj = hand[Random.Range(0, hand.Count)];
            var display = cardObj.GetComponent<CardDisplay>();
            if (display != null && display.cardData != null)
            {
                lostGospelTarget = display.cardData;
                display.UpdateCardDisplay();
            }
        }
    }

    private Card lostGospelTarget = null;

    public void OnTurnStart()
    {
        battleTurnCount++;
        turnTotalCost = 0;
        lostGospelTarget = null;
        lastPlayedCardType = Card.CardType.Curse;
        lastPlayedCard = null;

        // ✅ 보존(Retain) 상태 초기화 (한 턴만 유지되도록)
        var hand = HandManager.Instance?.CardsInHand;
        if (hand != null)
        {
            foreach (var cardObj in hand)
            {
                var movement = cardObj.GetComponent<CardMovement>();
                if (movement != null) movement.isRetained = false;
            }
        }

        if (HasRelic("tornCassock") && damagedWithoutArmorThisTurn)
        {
            tornCassockActiveNextTurn = true;
            Debug.Log("[RelicManager] 찢어진 수단 활성화: 이번 턴 첫 공격 데미지 50% 증가!");
        }
        else
        {
            tornCassockActiveNextTurn = false;
        }
        damagedWithoutArmorThisTurn = false;

        // ✅ ApplyPermanentRelicEffects에서 PlayerStats.AddStrength 호출
        ApplyPermanentRelicEffects();
        HandManager.Instance?.RefreshHandDisplay();

        if (HasRelic("cursedCrown")) PlayerStats.Instance?.GainEnergy(1);
        if (HasRelic("vowOfPoverty")) PlayerStats.Instance?.GainEnergy(1);
        if (HasRelic("hereticsNail")) PlayerStats.Instance?.GainEnergy(1);
        if (HasRelic("heavyShackles")) PlayerStats.Instance?.GainEnergy(1);

        if (statusImmuneNextTurn)
            statusImmuneNextTurn = false;

        if (HasRelic("waitingStatue"))
        {
            if (battleTurnCount > 1)
                DrawNextTurnTracker.Add(1);
        }
    }

    /// <summary>턴 시작 시 드로우 장수 수정 훅</summary>
    public void OnGetDrawCount(ref int drawCount)
    {
        int extra = DrawNextTurnTracker.Consume();
        if (extra != 0)
        {
            drawCount += extra;
            Debug.Log($"[RelicManager] 드로우 장수 수정: 기본 + {extra}장 (총 {drawCount}장)");
        }
    }

    public void OnGetCardCost(Card card, ref int cost)
    {
        if (card == null) return;

        if (card == lostGospelTarget)
            cost = 0;

        if (HasRelic("crownOfThorns"))
        {
            if (PlayerDataManager.Instance != null)
            {
                float hpPercent = (float)PlayerDataManager.Instance.currentHP / PlayerDataManager.Instance.maxHP;
                if (hpPercent <= 0.3f && card.cardType == Card.CardType.Attack)
                    cost = Mathf.Max(0, cost - 1);
            }
        }
    }

    public void OnTurnEnd()
    {
        if (HasRelic("monksSandals"))
        {
            if (DistanceManager.Instance != null && DistanceManager.Instance.CurrentDistance >= 2)
                DrawNextTurnTracker.Add(1);
        }

        if (HasRelic("crackedHourglass"))
        {
            if (HandManager.Instance != null && HandManager.Instance.CurrentHandSize == 0)
                DrawNextTurnTracker.Add(1);
        }

        if (HasRelic("ironKneePads"))
        {
            if (DistanceManager.Instance != null && DistanceManager.Instance.CurrentDistance <= 1)
                PlayerStats.Instance?.AddArmor(4);
        }

        if (HasRelic("bellOfSilence"))
        {
            int remainingEnergy = PlayerStats.Instance != null ? PlayerStats.Instance.energy : 0;
            if (remainingEnergy > 0) PlayerStats.Instance?.AddArmor(remainingEnergy * 5);
        }

        if (HasRelic("solidBedrock"))
        {
            if (PlayerStats.Instance != null && PlayerStats.Instance.Armor >= 15)
                statusImmuneNextTurn = true;
        }

        if (HasRelic("chainLink"))
        {
            int cardCount = HandManager.Instance != null ? HandManager.Instance.CurrentHandSize : 0;
            PlayerStats.Instance?.AddArmor(cardCount);
        }

        if (HasRelic("heavyCenser"))
        {
            if (turnTotalCost > 0)
            {
                if (turnTotalCost % 2 != 0) PlayerStats.Instance?.AddArmor(5);
                else
                {
                    Enemy target = EnemyManager.Instance?.GetRandomActiveEnemy();
                    if (target != null)
                    {
                        target.TakeDamage(7);
                        target.enemyDisplay?.ShowDamagePopup(7);
                    }
                }
            }
        }

        if (HasRelic("vibratingGauntlet"))
        {
            if (turnTotalCost >= 4)
            {
                Enemy target = EnemyManager.Instance?.GetRandomActiveEnemy();
                if (target != null)
                {
                    target.TakeDamage(10);
                    target.enemyDisplay?.ShowDamagePopup(10);
                }
            }
        }

        if (HasRelic("holyPouch"))
        {
            var hand = HandManager.Instance?.CardsInHand;
            if (hand != null && hand.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, hand.Count);
                var cardObj = hand[randomIndex];
                var movement = cardObj.GetComponent<CardMovement>();
                if (movement != null)
                {
                    movement.isRetained = true;
                    Debug.Log($"[RelicManager] 성스러운 주머니 효과 발동: '{movement.cardData?.cardName}' 보존!");
                }
            }
        }
    }

    public void OnDamageTaken(ref int damage)
    {
        if (damage > 0)
        {
            if (HasRelic("tornCassock"))
            {
                if (PlayerStats.Instance != null && PlayerStats.Instance.Armor == 0)
                {
                    damagedWithoutArmorThisTurn = true;
                    Debug.Log("[RelicManager] 찢어진 수단 조건 충족: 방어도 없이 피해를 입음!");
                }
            }

            if (HasRelic("fadedStainedGlass") && PlayerStats.Instance.Armor == 0)
                ArmorNextTurnTracker.Add(12);

            if (PlayerDataManager.Instance != null && damage >= PlayerDataManager.Instance.currentHP)
            {
                if (HasRelic("giantPumpkin") && !giantPumpkinUsed)
                {
                    giantPumpkinUsed = true;
                    damage = PlayerDataManager.Instance.currentHP - 1;
                    Debug.Log("[RelicManager] Giant Pumpkin saved the player!");
                }
                else if (HasRelic("embers") && !embersUsed)
                {
                    embersUsed = true;
                    damage = 0;

                    int lostMaxHP = PlayerDataManager.Instance.maxHP / 2;
                    PlayerDataManager.Instance.ModifyMaxHP(-lostMaxHP);
                    PlayerDataManager.Instance.currentHP = PlayerDataManager.Instance.maxHP;
                    PlayerDataManager.Instance.UpdateAllUI();

                    RemoveSpecificRelicFromPlayer("embers");
                    Debug.Log("[RelicManager] Embers revived the player!");
                }
            }
        }
    }

    public void OnArmorGained(int amount)
    {
        if (amount <= 0) return;

        if (HasRelic("clunkyIronPlate"))
        {
            Enemy target = EnemyManager.Instance?.GetRandomActiveEnemy();
            if (target != null)
            {
                target.TakeDamage(2);
                target.enemyDisplay?.ShowDamagePopup(2);
            }
        }

        if (HasRelic("spikedShield"))
        {
            var enemies = EnemyManager.Instance?.GetActiveEnemies();
            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    if (e == null) continue;
                    e.TakeDamage(1);
                    e.enemyDisplay?.ShowDamagePopup(1);
                }
            }
        }
    }

    /// <summary>플레이어 HP가 실제로 감소했을 때 호출.</summary>
    public void OnPlayerHPLost()
    {
        if (isPlayerTurn && HasRelic("asceticsWhip"))
        {
            if (HandManager.Instance != null)
            {
                HandManager.Instance.StartCoroutine(HandManager.Instance.DrawCardsRoutine(1));
                Debug.Log("[RelicManager] 고행자의 채찍 효과 발동: 카드 1장 드로우!");
            }
        }

        if (HasRelic("bloodyBandage"))
        {
            PlayerStats.Instance?.Heal(2);
            Debug.Log("[RelicManager] 피 묻은 붕대 효과 발동: 체력 2 회복!");
        }
    }

    public void OnCardPlayed(Card card)
    {
        if (card == null) return;
        turnTotalCost += card.energyCost;
        lastPlayedCardType = card.cardType;
        lastPlayedCard = card;
    }

    public void OnMovePointUsed(int cost)
    {
        if (HasRelic("horseshoe")) PlayerStats.Instance?.AddArmor(3);
    }

    public void OnCardExhausted(Card card)
    {
        cardsExhaustedCount++;
        if (HasRelic("chaliceOfAshes") && cardsExhaustedCount % 3 == 0)
            PlayerStats.Instance?.Heal(1);

        if (HasRelic("heavyBible"))
        {
            int damage = card.energyCost * 3;
            Enemy target = EnemyManager.Instance?.GetRandomActiveEnemy();
            if (target != null)
            {
                target.TakeDamage(damage);
                target.enemyDisplay?.ShowDamagePopup(damage);
            }
        }

        if (HasRelic("brokenRosary"))
        {
            // ✅ StrengthTracker.AddStrength → PlayerStats.AddStrength으로 교체
            PlayerStats.Instance?.AddStrength(1);
            HandManager.Instance?.RefreshHandDisplay();
            Debug.Log($"[RelicManager] 끊어진 묵주 효과: 힘 +1 (현재 힘: {PlayerStats.Instance?.strength})");
        }
    }

    public void OnEnemyKilled()
    {
        if (HasRelic("saintsHandBone")) PlayerStats.Instance?.AddArmor(5);
    }

    /// <summary>이벤트 노드 방문 시 호출 (순례자의 짚신).</summary>
    public void OnEnterEventNode()
    {
        pilgrimsStrawShoesCount++;
        Debug.Log($"[RelicManager] 이벤트 노드 방문! 현재 누적 횟수: {pilgrimsStrawShoesCount}");
    }

    public void OnDeckShuffled()
    {
        if (HasRelic("easterEgg"))
        {
            var enemies = EnemyManager.Instance?.GetActiveEnemies();
            if (enemies != null)
            {
                foreach (var e in enemies) e?.ApplyDullness(1);
                Debug.Log("[RelicManager] 부활절 달걀 효과 발동: 모든 적에게 약화(무뎌짐) 1 부여!");
            }
        }
    }

    public void OnEnemyDamaged(Enemy enemy, int initialDamage, int realDamage, int initialArmor, int initialHP)
    {
        if (enemy == null) return;

        if (HasRelic("batteringRamCross"))
        {
            if (initialArmor > 0 && enemy.Armor == 0)
                PlayerStats.Instance?.GainEnergy(1);
        }

        if (HasRelic("maceOfPunishment"))
        {
            if (lastPlayedCard != null && lastPlayedCard.cardType == Card.CardType.Attack && lastPlayedCard.energyCost >= 2)
            {
                if (realDamage > initialHP)
                {
                    int overkill = realDamage - initialHP;
                    Enemy nextTarget = EnemyManager.Instance?.GetRandomActiveEnemy();
                    if (nextTarget != null && nextTarget != enemy)
                    {
                        nextTarget.TakeDamage(overkill);
                        nextTarget.enemyDisplay?.ShowDamagePopup(overkill);
                    }
                }
            }
        }
    }

    public void OnGoldGained(ref int amount)
    {
        if (HasRelic("bucketOfLuck")) amount *= 2;
    }

    public void OnPurchase(ref int price, bool isCardRemoval = false)
    {
        if (isCardRemoval)
        {
            if (HasRelic("archbishopsSeal")) price = Mathf.RoundToInt(price * 0.5f);
        }
        else
        {
            if (HasRelic("gamblersCoin")) price = Mathf.RoundToInt(price * 0.75f);
        }

        if (nothingBoughtInLastShop && HasRelic("tornOfferingPouch"))
            price = Mathf.RoundToInt(price * 0.8f);
    }

    public void OnShopClosed(bool boughtAnything)
    {
        nothingBoughtInLastShop = !boughtAnything;
    }

    public void OnRest()
    {
        if (HasRelic("proteinShaker")) PlayerDataManager.Instance?.ModifyMaxHP(4);
    }

    public void OnCardBought()
    {
        if (HasRelic("organizedIndulgence")) PlayerDataManager.Instance?.ModifyMaxHP(1);
    }

    public void OnCardRemoved()
    {
        if (HasRelic("bundleOfIndulgences")) PlayerDataManager.Instance?.ModifyMaxHP(2);
    }

    /// <summary>보유하지 않은 유물 중 랜덤으로 지급.</summary>
    public void AddRandomRelicToPlayer(int amount)
    {
        if (relicDatabase == null || relicDatabase.relics == null || PlayerDataManager.Instance == null) return;

        List<string> availableRelicIds = new List<string>();
        foreach (var relic in relicDatabase.relics)
            if (relic != null && !HasRelic(relic.itemId))
                availableRelicIds.Add(relic.itemId);

        if (availableRelicIds.Count == 0)
        {
            Debug.Log("[RelicManager] 더 이상 획득할 수 있는 새로운 유물이 없습니다.");
            return;
        }

        int rewardCount = Mathf.Min(amount, availableRelicIds.Count);
        for (int i = 0; i < rewardCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableRelicIds.Count);
            string selectedId = availableRelicIds[randomIndex];
            AddRelicToPlayer(selectedId);
            availableRelicIds.RemoveAt(randomIndex);
        }
    }

    /// <summary>보유 유물 중 랜덤으로 제거.</summary>
    public void RemoveRandomRelicFromPlayer(int amount)
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.currentRelics == null) return;

        List<RelicData> playerRelics = PlayerDataManager.Instance.currentRelics;
        if (playerRelics.Count == 0)
        {
            Debug.Log("[RelicManager] 잃을 유물이 없습니다.");
            return;
        }

        int removeCount = Mathf.Min(amount, playerRelics.Count);
        for (int i = 0; i < removeCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, playerRelics.Count);
            RelicData targetRelic = playerRelics[randomIndex];
            Debug.Log($"[RelicManager] 유물 분실: {targetRelic.nameKo} ({targetRelic.itemId})");
            playerRelics.RemoveAt(randomIndex);
        }

        RefreshEffects();
        PlayerDataManager.Instance.UpdateAllUI();
    }

    /// <summary>특정 유물을 인벤토리에서 즉시 제거.</summary>
    public bool RemoveSpecificRelicFromPlayer(string itemId)
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.currentRelics == null) return false;

        List<RelicData> playerRelics = PlayerDataManager.Instance.currentRelics;
        RelicData target = playerRelics.Find(r => r.itemId == itemId);

        if (target != null)
        {
            playerRelics.Remove(target);
            Debug.Log($"[RelicManager] 지정 유물 제거 성공: {target.nameKo}");
            RefreshEffects();
            PlayerDataManager.Instance.UpdateAllUI();
            return true;
        }

        Debug.LogWarning($"[RelicManager] 제거하려는 유물을 보유하고 있지 않습니다: {itemId}");
        return false;
    }
}

// ── 전역 트래커 ──────────────────────────────────────────────

public static class DrawNextTurnTracker
{
    private static int extraDraw = 0;
    public static void Add(int amount) => extraDraw += amount;
    public static int Consume() { int val = extraDraw; extraDraw = 0; return val; }
}

public static class ArmorNextTurnTracker
{
    private static int nextArmor = 0;
    public static void Add(int amount) => nextArmor += amount;
    public static int Consume() { int val = nextArmor; nextArmor = 0; return val; }
}