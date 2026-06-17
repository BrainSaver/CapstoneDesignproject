using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // ESC 처리를 위해 추가
using UnityEngine.UI;

public class RandomStageManager : MonoBehaviour
{
    [Header("이벤트 데이터베이스")]
    public List<EventData> eventDatabase = new List<EventData>();

    private EventData currentEvent;

    [Header("UI 연결")]
    public Image backgroundUI;
    public TextMeshProUGUI eventTitleUI;
    public TextMeshProUGUI eventTextUI;
    public Transform choiceGroup;
    public GameObject choiceButtonPrefab;

    [Header("★ 카드 조작 UI 연결")]
    public GameObject deckViewPanel;       // 카드 목록이 뜰 화면 전체 패널 (DeckViewPanel)
    public Transform deckCardContainer;    // 패널 안에서 덱 카드들이 생성될 부모 (★꼭 Content 오브젝트 연결!)
    public GameObject removeCardUIPrefab;  // 카드 표시 및 버튼용 프리팹 (CardDisplay 필수)

    private bool isRemovalUIOpen = false;  // 현재 카드 관련 팝업 창이 열려있는지 여부

    private void Start()
    {
        // 시작할 때 카드 제거 패널은 비활성화 상태로 둡니다.
        if (deckViewPanel != null)
        {
            deckViewPanel.SetActive(false);
        }

        if (eventDatabase != null && eventDatabase.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, eventDatabase.Count);
            currentEvent = eventDatabase[randomIndex];
            SetupEventUI();
        }
    }

    private void Update()
    {
        // ESC 키를 눌렀을 때 카드 조작 패널이 열려있다면 패널만 닫아줍니다.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (deckViewPanel != null && deckViewPanel.activeSelf)
            {
                CloseDeckView();
            }
        }
    }

    private void SetupEventUI()
    {
        backgroundUI.sprite = currentEvent.eventImage;
        eventTitleUI.text = currentEvent.eventTitle;
        eventTextUI.text = currentEvent.eventText;

        foreach (Transform child in choiceGroup)
        {
            Destroy(child.gameObject);
        }

        foreach (EventChoice choice in currentEvent.choices)
        {
            GameObject newButton = Instantiate(choiceButtonPrefab, choiceGroup);
            TextMeshProUGUI btnText = newButton.GetComponentInChildren<TextMeshProUGUI>();
            btnText.text = $"{choice.choiceTitle}\n<size=60%>{choice.choiceDescription}</size>";

            Button btnComponent = newButton.GetComponent<Button>();
            btnComponent.onClick.AddListener(() => OnChoiceClicked(choice));
        }
    }

    private void OnChoiceClicked(EventChoice selectedChoice)
    {
        Debug.Log("선택됨: " + selectedChoice.choiceTitle);

        isRemovalUIOpen = false; // 플래그 초기화

        foreach (EventEffect effect in selectedChoice.effects)
        {
            ExecuteEffect(effect.effectType, effect.effectAmount, effect.stringData);
        }

        // ★ 중요: 만약 카드 선택 조작 UI가 열린 것이 아니라면, 즉시 이벤트를 끝내고 로드맵으로 갑니다.
        if (!isRemovalUIOpen)
        {
            FinishEvent();
        }
    }

    private void ExecuteEffect(EventEffectType type, int amount, string stringData)
    {
        switch (type)
        {
            case EventEffectType.Heal:
                PlayerDataManager.Instance.ModifyHP(amount);
                break;
            case EventEffectType.Damage:
                PlayerDataManager.Instance.ModifyHP(-amount);
                break;
            case EventEffectType.GainGold:
                PlayerDataManager.Instance.AddGold(amount);
                break;
            case EventEffectType.LoseGold:
                PlayerDataManager.Instance.AddGold(-amount);
                break;
            case EventEffectType.GainMaxHP:
                PlayerDataManager.Instance.ModifyMaxHP(amount);
                break;
            case EventEffectType.LoseMaxHP:
                PlayerDataManager.Instance.ModifyMaxHP(-amount);
                break;
            case EventEffectType.ChanceRoll:
                {
                    int roll = UnityEngine.Random.Range(0, 100);
                    string[] outcomes = stringData.Split('/');
                    string successRaw = outcomes.Length > 0 ? outcomes[0] : "None";
                    string failRaw = outcomes.Length > 1 ? outcomes[1] : "None";

                    if (roll < amount)
                    {
                        Debug.Log("확률 판정 성공 (" + roll + " / " + amount + ") -> 실행: " + successRaw);
                        ExecuteCommand(successRaw);
                    }
                    else
                    {
                        Debug.Log("확률 판정 실패 (" + roll + " / " + amount + ") -> 실행: " + failRaw);
                        ExecuteCommand(failRaw);
                    }
                }
                break;

            // 유물 획득 연동 분기
            case EventEffectType.GainRelic:
                if (RelicManager.Instance == null)
                {
                    Debug.LogError("씬에 RelicManager가 존재하지 않습니다!");
                    break;
                }

                if (stringData == "Random")
                {
                    Debug.Log("무작위 유물 랜덤 획득: " + amount + "개");
                    RelicManager.Instance.AddRandomRelicToPlayer(amount);
                }
                else
                {
                    Debug.Log($"지정된 특정 유물 [{stringData}] 확정 획득! 수량: {amount}개");
                    for (int i = 0; i < amount; i++)
                    {
                        RelicManager.Instance.AddRelicToPlayer(stringData);
                    }
                }
                break;

            // 유물 잃기 연동 분기
            case EventEffectType.LoseRelic:
                if (RelicManager.Instance == null)
                {
                    Debug.LogError("씬에 RelicManager가 존재하지 않습니다!");
                    break;
                }

                if (stringData == "Random")
                {
                    Debug.Log("보유 유물 중 랜덤 분실: " + amount + "개");
                    RelicManager.Instance.RemoveRandomRelicFromPlayer(amount);
                }
                else
                {
                    Debug.Log($"지정된 특정 유물 [{stringData}] 삭제 실행");
                    RelicManager.Instance.RemoveSpecificRelicFromPlayer(stringData);
                }
                break;

            // 축성물(Consecration Item) 획득 연동
            case EventEffectType.GainPotion:
                if (ConsecrationItemManager.Instance == null)
                {
                    Debug.LogError("씬에 ConsecrationItemManager가 존재하지 않습니다!");
                    break;
                }

                if (stringData == "Random")
                {
                    Debug.Log($"[축성물 보상] 무작위 축성물 랜덤 획득 실행: {amount}개");
                    ConsecrationItemManager.Instance.AddRandomPotionToPlayer(amount);
                }
                else
                {
                    Debug.Log($"[축성물 보상] 지정된 특정 축성물 [{stringData}] 확정 획득! 수량: {amount}개");
                    for (int i = 0; i < amount; i++)
                    {
                        ConsecrationItemManager.Instance.AddPotion(stringData);
                    }
                }
                break;

            // 축성물(Consecration Item) 잃기 연동
            case EventEffectType.LosePotion:
                if (ConsecrationItemManager.Instance == null)
                {
                    Debug.LogError("씬에 ConsecrationItemManager가 존재하지 않습니다!");
                    break;
                }

                if (stringData == "Random")
                {
                    Debug.Log($"[축성물 디버프] 보유 축성물 중 랜덤 분실 실행: {amount}개");
                    ConsecrationItemManager.Instance.RemoveRandomPotionFromPlayer(amount);
                }
                break;

            // 카드 제거 (선택 / 랜덤)
            case EventEffectType.RemoveCard:
                if (stringData == "Random")
                {
                    Debug.Log("카드 랜덤 제거 실행: " + amount + "장");
                    RemoveRandomCard(amount);
                }
                else
                {
                    Debug.Log("카드 선택 제거 UI 오픈: " + amount + "장");
                    OpenDeckViewForEvent();
                }
                break;

            // 카드 상태 부여 효과들
            case EventEffectType.InnateCardEffect:
                Debug.Log("카드 개시(선천성) 부여 UI 오픈: " + amount + "장의 카드 선택");
                OpenDeckViewForInnate();
                break;
            case EventEffectType.SneckoCardEffect:
                Debug.Log("카드 혼돈(스네코) 부여 UI 오픈: " + amount + "장의 카드 선택");
                OpenDeckViewForSnecko();
                break;

            // 카드 성능 배수 강화 효과
            case EventEffectType.DoubleCardDamage:
                Debug.Log("카드 공격력 2배 강화 UI 오픈: " + amount + "장 선택");
                OpenDeckViewForDoubleDamage();
                break;
            case EventEffectType.DoubleCardBlock:
                Debug.Log("카드 방어도 2배 강화 UI 오픈: " + amount + "장 선택");
                OpenDeckViewForDoubleBlock();
                break;

            // 카드 변화
            case EventEffectType.TransformCard:
                Debug.Log("카드 변화 UI 오픈: 수량 " + amount);
                OpenDeckViewForTransform();
                break;

            // 카드 복사
            case EventEffectType.CopyCard:
                Debug.Log("카드 복사 UI 오픈: 수량 " + amount);
                OpenDeckViewForCopy();
                break;

            case EventEffectType.HalveNextEnemyHP:
                PlayerDataManager.Instance.halveNextEnemyHP = true;
                Debug.Log("다음 전투 적 HP 절반 디버프 활성화");
                break;

            // ★ 다음 ?방 무조건 유물방 확정 패시브 효과 연동
            case EventEffectType.GuaranteeNextEventRelic:
                if (PlayerDataManager.Instance != null)
                {
                    PlayerDataManager.Instance.guaranteeRelicInNextEventRoom = true;
                    Debug.Log("다음 ?방 진입 시 유물 확정 지급 패시브 활성화 완료.");
                }
                break;

            // ★ 현재 체력 / 잃은 체력 수치 역전 효과 연동
            case EventEffectType.InvertHealth:
                if (PlayerDataManager.Instance != null)
                {
                    int maxHP = PlayerDataManager.Instance.maxHP;
                    int currentHP = PlayerDataManager.Instance.currentHP;
                    int newCurrentHP = maxHP - currentHP;

                    if (newCurrentHP <= 0) newCurrentHP = 1;

                    PlayerDataManager.Instance.currentHP = newCurrentHP;
                    PlayerDataManager.Instance.UpdateAllUI();
                    Debug.Log($"체력 반전 완료: {currentHP} -> {newCurrentHP} (최대: {maxHP})");
                }
                break;

            // 카드 획득 (지정 획득 / 랜덤 10장 중 선택 획득)
            case EventEffectType.GainCard:
                {
                    if (stringData == "Random")
                    {
                        Debug.Log("무작위 카드 랜덤 획득: " + amount + "장");
                    }
                    else if (stringData == "Choice")
                    {
                        Debug.Log("카드 선택 보상 UI 오픈: 10장 중 1장 선택 (공격/스킬/저주 통합 풀)");
                        OpenDeckViewForCardChoice(10);
                    }
                    else
                    {
                        // ★ [저주 카드 경로 추가]: Attack, Skill 폴더를 찾고 없다면 Curse 폴더까지 싹 뒤집니다!
                        Card localCardToAdd = Resources.Load<Card>("CardsSkill/" + stringData)
                                           ?? Resources.Load<Card>("CardsAttack/" + stringData)
                                           ?? Resources.Load<Card>("CardsCurse/" + stringData); // ◀ 이 줄 추가

                        if (localCardToAdd != null)
                        {
                            for (int i = 0; i < amount; i++)
                            {
                                PlayerDeck.Instance.AddCardToDeck(localCardToAdd);
                            }
                            Debug.Log($"[이벤트 보상] 덱에 [{localCardToAdd.cardName}] 카드가 {amount}장 추가되었습니다!");
                        }
                        else
                        {
                            Debug.LogError($"'{stringData}' 카드를 찾을 수 없습니다! Resources의 CardsSkill, CardsAttack, 또는 CardsCurse 폴더를 확인해 주세요.");
                        }
                    }
                }
                break;
        }
    }

    // =====================================================================================
    // ★ 카드 조작 관련 UI 빌드 및 데이터 연동 연산 로직들
    // =====================================================================================

    #region 카드 보상 선택 (Card Reward Choice)

    private void OpenDeckViewForCardChoice(int choiceCount)
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null)
        {
            Debug.LogError("[RandomStageManager] 카드 보상 선택 UI 자리가 인스펙터에서 비어있습니다!");
            return;
        }

        Card[] attackCards = Resources.LoadAll<Card>("CardsAttack");
        Card[] skillCards = Resources.LoadAll<Card>("CardsSkill");

        List<Card> allLoadedCards = new List<Card>();
        if (attackCards != null) allLoadedCards.AddRange(attackCards);
        if (skillCards != null) allLoadedCards.AddRange(skillCards);

        if (allLoadedCards.Count == 0)
        {
            Debug.LogError("보상 카드 풀(Resources/CardsAttack 및 CardsSkill)이 모두 비어있습니다!");
            return;
        }

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer)
        {
            Destroy(child.gameObject);
        }

        List<Card> tempPool = new List<Card>(allLoadedCards);
        int finalCount = Mathf.Min(choiceCount, tempPool.Count);

        for (int i = 0; i < finalCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, tempPool.Count);
            Card rewardCard = tempPool[randomIndex];
            tempPool.RemoveAt(randomIndex);

            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);

            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(rewardCard);

            CardMovement cardMovement = newCardObj.GetComponent<CardMovement>();
            if (cardMovement != null) cardMovement.enabled = false;

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => OnRewardCardSelected(rewardCard));
        }
    }

    private void OnRewardCardSelected(Card chosenCard)
    {
        if (PlayerDeck.Instance != null)
        {
            PlayerDeck.Instance.AddCardToDeck(chosenCard);
            Debug.Log($"[이벤트 보상 선택] [{chosenCard.cardName}] 카드가 영구 덱에 추가되었습니다.");

            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 카드 선택 제거 (Remove Choice)

    private void OpenDeckViewForEvent()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null)
        {
            Debug.LogError("[RandomStageManager] 카드 제거 UI 자리가 인스펙터에서 비어있습니다!");
            return;
        }

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer)
        {
            Destroy(child.gameObject);
        }

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);

            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryRemoveCardForEvent(myCard, newCardObj));
        }
    }

    public void TryRemoveCardForEvent(Card cardToRemove, GameObject cardUIObject)
    {
        if (PlayerDeck.Instance != null)
        {
            PlayerDeck.Instance.RemoveCardFromDeck(cardToRemove);
            Debug.Log($"[이벤트 제거 성공] {cardToRemove.cardName} 카드가 제거되었습니다.");

            Destroy(cardUIObject);

            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 카드 랜덤 제거 (Remove Random)

    private void RemoveRandomCard(int amount)
    {
        if (PlayerDeck.Instance == null) return;

        List<Card> currentDeck = new List<Card>(PlayerDeck.Instance.CurrentDeck);
        if (currentDeck.Count == 0) return;

        int removeCount = Mathf.Min(amount, currentDeck.Count);
        for (int i = 0; i < removeCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, currentDeck.Count);
            Card targetCard = currentDeck[randomIndex];

            PlayerDeck.Instance.RemoveCardFromDeck(targetCard);
            currentDeck.RemoveAt(randomIndex);
        }

        FinishEvent();
    }

    #endregion

    #region 카드 변화 (Transform)

    private void OpenDeckViewForTransform()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null) return;

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer) Destroy(child.gameObject);

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);
            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryTransformCardForEvent(myCard, newCardObj));
        }
    }

    private void TryTransformCardForEvent(Card cardToTransform, GameObject cardUIObject)
    {
        if (PlayerDeck.Instance != null)
        {
            Card[] attackCards = Resources.LoadAll<Card>("CardsAttack");
            Card[] skillCards = Resources.LoadAll<Card>("CardsSkill");

            List<Card> allCards = new List<Card>();
            if (attackCards != null) allCards.AddRange(attackCards);
            if (skillCards != null) allCards.AddRange(skillCards);

            if (allCards.Count == 0)
            {
                CloseDeckView();
                FinishEvent();
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, allCards.Count);
            Card randomNewCard = allCards[randomIndex];

            PlayerDeck.Instance.RemoveCardFromDeck(cardToTransform);
            PlayerDeck.Instance.AddCardToDeck(randomNewCard);

            Destroy(cardUIObject);
            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 카드 복사 (Copy)

    private void OpenDeckViewForCopy()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null) return;

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer) Destroy(child.gameObject);

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);
            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();

            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryCopyCardForEvent(myCard));
        }
    }

    private void TryCopyCardForEvent(Card cardToCopy)
    {
        if (PlayerDeck.Instance != null)
        {
            PlayerDeck.Instance.AddCardToDeck(cardToCopy);
            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 선천성 부여 (Innate)

    private void OpenDeckViewForInnate()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null) return;

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer) Destroy(child.gameObject);

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);
            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryApplyInnate(myCard));
        }
    }

    private void TryApplyInnate(Card targetCard)
    {
        if (PlayerDeck.Instance != null)
        {
            targetCard.isInnate = true;
            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 스네코 부여 (Snecko)

    private void OpenDeckViewForSnecko()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null) return;

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer) Destroy(child.gameObject);

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);
            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryApplySnecko(myCard));
        }
    }

    private void TryApplySnecko(Card targetCard)
    {
        if (PlayerDeck.Instance != null)
        {
            targetCard.isSnecko = true;
            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 데미지 2배 영구 강화 (Double Damage)

    private void OpenDeckViewForDoubleDamage()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null) return;

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer) Destroy(child.gameObject);

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);
            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryApplyDoubleDamage(myCard));
        }
    }

    private void TryApplyDoubleDamage(Card targetCard)
    {
        if (PlayerDeck.Instance != null)
        {
            targetCard.bonusDamageMultiplier *= 2;
            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    #region 방어도 2배 영구 강화 (Double Block)

    private void OpenDeckViewForDoubleBlock()
    {
        if (deckViewPanel == null || deckCardContainer == null || removeCardUIPrefab == null) return;

        isRemovalUIOpen = true;
        deckViewPanel.SetActive(true);

        foreach (Transform child in deckCardContainer) Destroy(child.gameObject);

        IReadOnlyList<Card> currentDeck = PlayerDeck.Instance.CurrentDeck;
        foreach (Card myCard in currentDeck)
        {
            GameObject newCardObj = Instantiate(removeCardUIPrefab, deckCardContainer);
            CardDisplay cardDisplay = newCardObj.GetComponent<CardDisplay>();
            if (cardDisplay != null) cardDisplay.SetData(myCard);

            Button cardButton = newCardObj.GetComponent<Button>() ?? newCardObj.GetComponentInChildren<Button>() ?? newCardObj.AddComponent<Button>();
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => TryApplyDoubleBlock(myCard));
        }
    }

    private void TryApplyDoubleBlock(Card targetCard)
    {
        if (PlayerDeck.Instance != null)
        {
            targetCard.bonusBlockMultiplier *= 2;
            CloseDeckView();
            FinishEvent();
        }
    }

    #endregion

    public void CloseDeckView()
    {
        if (deckViewPanel != null)
        {
            deckViewPanel.SetActive(false);
        }
    }

    public void ExecuteCommand(string commandRaw)
    {
        string[] parts = commandRaw.Split(':');
        string action = parts[0].Trim();

        int value = 0;
        if (parts.Length > 1)
        {
            int.TryParse(parts[1], out value);
        }

        string extraData = parts.Length > 2 ? parts[2].Trim() : "";

        if (action == "GainRelicSpecific") ExecuteEffect(EventEffectType.GainRelic, value, extraData);
        else if (action == "GainRelicRandom") ExecuteEffect(EventEffectType.GainRelic, value, "Random");
        else if (action == "LoseRelicRandom") ExecuteEffect(EventEffectType.LoseRelic, value, "Random");
        else if (action == "LoseRelicSpecific") ExecuteEffect(EventEffectType.LoseRelic, value, extraData);

        else if (action == "UpgradeCardRandom") ExecuteEffect(EventEffectType.UpgradeCard, value, "Random");
        else if (action == "UpgradeCardChoice") ExecuteEffect(EventEffectType.UpgradeCard, value, "Choice");

        // 축성물 커맨드 매핑
        else if (action == "GainPotionSpecific") ExecuteEffect(EventEffectType.GainPotion, value, extraData);
        else if (action == "GainPotionRandom") ExecuteEffect(EventEffectType.GainPotion, value, "Random");
        else if (action == "LosePotionRandom") ExecuteEffect(EventEffectType.LosePotion, value, "Random");

        else if (action == "GainCardChoice") ExecuteEffect(EventEffectType.GainCard, value, "Choice");
        else if (action == "RemoveCardRandom") ExecuteEffect(EventEffectType.RemoveCard, value, "Random");
        else if (action == "RemoveCardChoice") ExecuteEffect(EventEffectType.RemoveCard, value, "Choice");

        else if (action == "InnateCardEffect") ExecuteEffect(EventEffectType.InnateCardEffect, value, "");
        else if (action == "SneckoCardEffect") ExecuteEffect(EventEffectType.SneckoCardEffect, value, "");
        else if (action == "DoubleCardDamage") ExecuteEffect(EventEffectType.DoubleCardDamage, value, "");
        else if (action == "DoubleCardBlock") ExecuteEffect(EventEffectType.DoubleCardBlock, value, "");

        // ★ 신규 커맨드 데이터베이스 매핑
        else if (action == "GuaranteeNextEventRelic") ExecuteEffect(EventEffectType.GuaranteeNextEventRelic, value, "");
        else if (action == "InvertHealth") ExecuteEffect(EventEffectType.InvertHealth, value, "");

        else if (action == "LoseMaxHP") ExecuteEffect(EventEffectType.LoseMaxHP, value, "");
        else if (action == "GainMaxHP") ExecuteEffect(EventEffectType.GainMaxHP, value, "");
        else if (action == "Heal") ExecuteEffect(EventEffectType.Heal, value, "");
        else if (action == "Damage") ExecuteEffect(EventEffectType.Damage, value, "");
        else if (action == "GainGold") ExecuteEffect(EventEffectType.GainGold, value, "");
        else if (action == "TransformCard") ExecuteEffect(EventEffectType.TransformCard, value, "");
        else if (action == "CopyCard") ExecuteEffect(EventEffectType.CopyCard, value, "");
        else if (action == "HalveNextEnemyHP") ExecuteEffect(EventEffectType.HalveNextEnemyHP, value, "");
        else if (action == "None") ExecuteEffect(EventEffectType.None, 0, "");
    }

    private void FinishEvent()
    {
        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(SceneType.RoadMap);
        }
        else
        {
            Debug.LogError("SceneFlowManager가 없어 씬을 이동할 수 없습니다.");
        }
    }
}