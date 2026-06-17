using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopManager : MonoBehaviour
{
    [Header("상점 설정")]
    public List<Card> shopCardPool = new List<Card>();
    public int cardsToDisplay = 5;
    public int relicsToDisplay = 3;
    public int potionsToDisplay = 2; // 추가: 표시할 축성물 수
    public int cardRemovePrice = 100;

    [Header("유물 가격 설정")]
    public int commonRelicPrice = 150;
    public int rareRelicPrice = 250;
    public int legendaryRelicPrice = 400;

    [Header("축성물 가격 설정")]
    public int commonPotionPrice = 50;
    public int rarePotionPrice = 100;
    public int legendaryPotionPrice = 200;

    [Header("상점 카드 UI 연결")]
    public Transform cardContainer;
    public GameObject shopCardUIPrefab;

    [Header("상점 유물 UI 연결")]
    public Transform relicContainer;
    public GameObject shopRelicUIPrefab;

    [Header("상점 축성물 UI 연결")]
    public Transform potionContainer; // 추가: 축성물이 생성될 부모
    public GameObject shopPotionUIPrefab; // 추가: ShopPotionUI가 붙은 축성물 프리팹

    [Header("카드 제거 UI 연결")]
    public GameObject deckViewPanel; // 카드 목록이 뜰 화면 전체 패널
    public Transform deckCardContainer; // 패널 안에서 덱 카드들이 생성될 부모
    public GameObject removeCardUIPrefab; // RemoveCardUI가 붙은 카드 제거용 프리팹
    public TMPro.TextMeshProUGUI removeServicePriceText; // 메인 상점 화면의 제거 비용 텍스트

    [Header("오디오 설정")]
    public AudioClip buySound;
    [Range(0f, 1f)] public float buySoundVolume = 0.5f;
    private AudioSource audioSource;

    private bool boughtSomethingThisVisit = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void Start()
    {
        GenerateShopCards();
        GenerateShopRelics(); // 유물 생성 추가
        GenerateShopPotions(); // 축성물 생성 추가
        UpdateRemoveServicePriceUI();

        // 시작할 때 제거 패널은 비활성화 상태로 둡니다.
        if (deckViewPanel != null)
        {
            deckViewPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 제거 패널이 열려있다면 패널만 닫고, 아니라면 로드맵으로 나갑니다.
            if (deckViewPanel != null && deckViewPanel.activeSelf)
            {
                CloseDeckView();
            }
            else
            {
                ReturnToRoadMap();
            }
        }
    }

    private void PlayBuySound()
    {
        if (audioSource != null && buySound != null)
        {
            audioSource.PlayOneShot(buySound, buySoundVolume);
        }
    }

    public void UpdateRemoveServicePriceUI()
    {
        if (removeServicePriceText != null)
        {
            int currentRemovePrice = cardRemovePrice;
            bool hasSeal = RelicManager.Instance != null && RelicManager.Instance.HasRelic("archbishopsSeal");
            Debug.Log($"[ShopManager] UpdateRemoveServicePriceUI - Before OnPurchase: {currentRemovePrice}, Has archbishopsSeal: {hasSeal}");
            if (RelicManager.Instance != null) RelicManager.Instance.OnPurchase(ref currentRemovePrice, isCardRemoval: true);
            Debug.Log($"[ShopManager] UpdateRemoveServicePriceUI - After OnPurchase: {currentRemovePrice}");
            removeServicePriceText.text = $"{currentRemovePrice} G";
        }
    }

    public void GenerateShopCards()
    {
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        List<Card> availableCards = new List<Card>(shopCardPool);
        ShuffleList(availableCards);

        int displayCount = Mathf.Min(cardsToDisplay, availableCards.Count);

        for (int i = 0; i < displayCount; i++)
        {
            Card selectedCard = availableCards[i];
            GameObject newCardObj = Instantiate(shopCardUIPrefab, cardContainer);

            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(selectedCard);

            int randomPrice = Random.Range(50, 100);
            if (RelicManager.Instance != null) RelicManager.Instance.OnPurchase(ref randomPrice);

            ShopCardUI shopUI = newCardObj.GetComponent<ShopCardUI>();
            if (shopUI != null)
            {
                shopUI.Setup(selectedCard, randomPrice, this);
            }
            else
            {
                Debug.LogError("[ShopManager] 카드 프리팹에 ShopCardUI 컴포넌트가 없습니다.");
            }
        }
    }

    public void TryBuyCard(Card cardToBuy, int price, GameObject cardUIObject)
    {
        if (PlayerDataManager.Instance.currentGold >= price)
        {
            PlayerDataManager.Instance.AddGold(-price);
            PlayerDeck.Instance.AddCardToDeck(cardToBuy);

            Debug.Log($"[상점] {cardToBuy.cardName} 구매 완료. 남은 골드: {PlayerDataManager.Instance.currentGold}");
            RelicManager.Instance?.OnCardBought();
            boughtSomethingThisVisit = true;
            PlayBuySound();

            Destroy(cardUIObject);
        }
        else
        {
            Debug.Log($"[상점] 골드가 부족합니다. 필요 골드: {price}");
        }
    }

    // --- 유물 관련 기능 ---

    public void GenerateShopRelics()
    {
        if (relicContainer == null || shopRelicUIPrefab == null) return;

        foreach (Transform child in relicContainer)
        {
            Destroy(child.gameObject);
        }

        if (RelicManager.Instance == null)
        {
            Debug.LogError("[ShopManager] RelicManager 인스턴스를 찾을 수 없습니다. PlayerDataManager가 RelicManager를 생성했는지, 혹은 씬에 RelicManager가 배치되어 있는지 확인해주세요.");
            return;
        }

        if (RelicManager.Instance.relicDatabase == null)
        {
            Debug.LogError("[ShopManager] RelicManager에 RelicDatabase가 할당되지 않았습니다. 인스펙터에서 'RelicDatabase' 에셋을 연결해주세요.");
            return;
        }

        // 1. 판매 가능한 유물 목록 필터링
        List<RelicData> availableRelics = new List<RelicData>();
        foreach (var relic in RelicManager.Instance.relicDatabase.relics)
        {
            //  Legendary 등급 제외, 플레이어가 이미 가진 유물 제외, 이벤트 전용 제외
            if (relic.rarity != "Legendary" &&
                !RelicManager.Instance.HasRelic(relic.itemId) &&
                !relic.isEventExclusive)
            {
                availableRelics.Add(relic);
            }
        }

        ShuffleList(availableRelics);

        int displayCount = Mathf.Min(relicsToDisplay, availableRelics.Count);

        for (int i = 0; i < displayCount; i++)
        {
            RelicData selectedRelic = availableRelics[i];
            GameObject newRelicObj = Instantiate(shopRelicUIPrefab, relicContainer);

            int price = GetRelicPrice(selectedRelic.rarity);
            if (RelicManager.Instance != null) RelicManager.Instance.OnPurchase(ref price); // 유물 효과 등에 의한 할인 적용

            ShopRelicUI relicUI = newRelicObj.GetComponent<ShopRelicUI>();
            if (relicUI != null)
            {
                relicUI.Setup(selectedRelic, price, this);
            }
            else
            {
                Debug.LogError("[ShopManager] 유물 프리팹에 ShopRelicUI 컴포넌트가 없습니다.");
            }
        }
    }

    private int GetRelicPrice(string rarity)
    {
        switch (rarity)
        {
            case "Common": return commonRelicPrice;
            case "Rare": return rareRelicPrice;
            case "Legendary": return legendaryRelicPrice;
            default: return commonRelicPrice;
        }
    }

    public void TryBuyRelic(RelicData relicToBuy, int price, GameObject relicUIObject)
    {
        if (PlayerDataManager.Instance.currentGold >= price)
        {
            PlayerDataManager.Instance.AddGold(-price);
            RelicManager.Instance.AddRelicToPlayer(relicToBuy.itemId);

            Debug.Log($"[상점] {relicToBuy.nameKo} 유물 구매 완료. 남은 골드: {PlayerDataManager.Instance.currentGold}");
            boughtSomethingThisVisit = true;
            PlayBuySound();
            
            UpdateRemoveServicePriceUI(); // 대주교의 인장 등을 구매했을 때 제거 비용 즉시 갱신

            Destroy(relicUIObject);
        }
        else
        {
            Debug.Log($"[상점] 골드가 부족하여 유물을 구매할 수 없습니다. 필요 골드: {price}");
        }
    }

    // --- 축성물(Potion) 관련 기능 ---

    public void GenerateShopPotions()
    {
        if (potionContainer == null || shopPotionUIPrefab == null) return;

        foreach (Transform child in potionContainer)
        {
            Destroy(child.gameObject);
        }

        if (ConsecrationItemManager.Instance == null || ConsecrationItemManager.Instance.database == null)
        {
            Debug.LogError("[ShopManager] ConsecrationItemManager 또는 데이터베이스를 찾을 수 없습니다.");
            return;
        }

        // 1. 전체 축성물 목록 가져와서 레전더리 등급은 제외
        List<ConsecrationItemData> availablePotions = new List<ConsecrationItemData>();
        foreach (var potion in ConsecrationItemManager.Instance.database.items)
        {
            if (potion.rarity != "Legendary")
            {
                availablePotions.Add(potion);
            }
        }
        
        // (선택 사항) 이미 인벤토리가 꽉 찼는지 확인하거나, 중복 구매 가능 여부 결정
        // 여기서는 그냥 필터된 포션 풀에서 랜덤하게 뽑습니다.

        ShuffleList(availablePotions);

        int displayCount = Mathf.Min(potionsToDisplay, availablePotions.Count);

        for (int i = 0; i < displayCount; i++)
        {
            ConsecrationItemData selectedPotion = availablePotions[i];
            GameObject newPotionObj = Instantiate(shopPotionUIPrefab, potionContainer);

            int price = GetPotionPrice(selectedPotion.rarity);
            if (RelicManager.Instance != null) RelicManager.Instance.OnPurchase(ref price); // gamblersCoin 등의 할인 적용

            ShopPotionUI potionUI = newPotionObj.GetComponent<ShopPotionUI>();
            if (potionUI != null)
            {
                potionUI.Setup(selectedPotion, price, this);
            }
            else
            {
                Debug.LogError("[ShopManager] 축성물 프리팹에 ShopPotionUI 컴포넌트가 없습니다.");
            }
        }
    }

    private int GetPotionPrice(string rarity)
    {
        switch (rarity)
        {
            case "Common": return commonPotionPrice;
            case "Rare": return rarePotionPrice;
            case "Legendary": return legendaryPotionPrice;
            default: return commonPotionPrice;
        }
    }

    public void TryBuyPotion(ConsecrationItemData potionToBuy, int price, GameObject potionUIObject)
    {
        if (PlayerDataManager.Instance.currentGold < price)
        {
            Debug.Log($"[상점] 골드가 부족하여 축성물을 구매할 수 없습니다. 필요 골드: {price}");
            return;
        }

        if (ConsecrationItemManager.Instance.IsFull)
        {
            Debug.Log("[상점] 축성물 슬롯이 가득 찼습니다.");
            return;
        }

        PlayerDataManager.Instance.AddGold(-price);
        ConsecrationItemManager.Instance.AddPotion(potionToBuy);

        Debug.Log($"[상점] {potionToBuy.nameKo} 축성물 구매 완료. 남은 골드: {PlayerDataManager.Instance.currentGold}");
        boughtSomethingThisVisit = true;
        PlayBuySound();

        Destroy(potionUIObject);
    }

    // --- 새로 추가된 카드 제거 관련 기능 ---

    public void OpenDeckViewForRemoval()
    {
        if (deckViewPanel == null) return;

        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;

        int currentRemovePrice = cardRemovePrice;
        if (RelicManager.Instance != null) RelicManager.Instance.OnPurchase(ref currentRemovePrice, isCardRemoval: true);

        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);

            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            RemoveCardUI removeUI = newCardObj.GetComponent<RemoveCardUI>();
            if (removeUI != null)
            {
                removeUI.Setup(myCard, this, currentRemovePrice);
            }
            else
            {
                Debug.LogError("[ShopManager] 제거용 카드 프리팹에 RemoveCardUI 컴포넌트가 없습니다.");
            }
        }
    }

    public void CloseDeckView()
    {
        if (deckViewPanel != null)
        {
            deckViewPanel.SetActive(false);
        }
    }

    public void TryRemoveCard(Card cardToRemove, GameObject cardUIObject)
    {
        int currentRemovePrice = cardRemovePrice;
        if (RelicManager.Instance != null) RelicManager.Instance.OnPurchase(ref currentRemovePrice, isCardRemoval: true);

        if (PlayerDataManager.Instance.currentGold >= currentRemovePrice)
        {
            PlayerDataManager.Instance.AddGold(-currentRemovePrice);
            PlayerDeck.Instance.RemoveCardFromDeck(cardToRemove);

            Debug.Log($"[상점] {cardToRemove.cardName} 삭제 완료. 남은 골드: {PlayerDataManager.Instance.currentGold}");
            RelicManager.Instance?.OnCardRemoved();
            boughtSomethingThisVisit = true;
            PlayBuySound();

            Destroy(cardUIObject);
        }
        else
        {
            Debug.Log($"[상점] 골드가 부족하여 카드를 삭제할 수 없습니다. 필요 골드: {currentRemovePrice}");
        }
    }

    public void ReturnToRoadMap()
    {
        RelicManager.Instance?.OnShopClosed(boughtSomethingThisVisit);
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(SceneType.RoadMap);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}