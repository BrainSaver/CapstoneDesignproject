using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 후 보상 패널을 관리한다.
/// BattleScene 내에서 전투 종료 후 패널 형태로 표시된다.
/// </summary>
public class RewardManager : SceneSingleton<RewardManager>
{
    [Header("보상 설정")]
    [SerializeField] private int MaxGold = 30;  // 최대 골드 보상
    [SerializeField] private int cardChoiceCount = 3;   // 카드 선택지 수
    [SerializeField] private List<Card> cardPool = new(); // 보상 카드 풀

    [Header("UI 참조")]
    [SerializeField] private GameObject rewardPanel;       // 보상 패널 루트
    [SerializeField] private Transform cardChoiceParent;  // 카드 선택지 배치 부모
    [SerializeField] private GameObject cardChoicePrefab;  // 카드 선택지 프리팹
    [SerializeField] private GameObject cardChoicePanel;   // 카드 선택지 패널 (열기 전 숨김)
    [SerializeField] private Button openCardButton;    // 카드 보상 열기 버튼
    [SerializeField] private Button closeCardButton; // 카드 보상 끄기 버튼
    [SerializeField] private Button goldButton;        // 골드 획득 버튼
    [SerializeField] private TMPro.TextMeshProUGUI goldText; // 골드 보상 텍스트
    [SerializeField] private Button skipButton;        // 카드 스킵 버튼

    private int goldReward = 0; // 이번 전투 골드 보상
    private bool goldClaimed = false; // 골드 이미 획득 여부
    private bool cardClaimed = false; // 카드 이미 선택 여부
    private bool cardGenerated = false;

    protected override void Awake()
    {
        base.Awake();
        if (rewardPanel != null) rewardPanel.SetActive(false);
        if (cardChoicePanel != null) cardChoicePanel.SetActive(false);
    }

    /// <summary>보상 패널을 표시한다. BattleManager.HandleBattleVictory()에서 호출.</summary>
    public void ShowReward()
    {
        goldClaimed = false;
        cardClaimed = false;
        cardGenerated = false;

        if (rewardPanel != null) rewardPanel.SetActive(true);
        if (cardChoicePanel != null) cardChoicePanel.SetActive(false);

        goldReward = UnityEngine.Random.Range(20, MaxGold + 1);
        if (goldText != null) goldText.text = $"{goldReward}";

        if (goldButton != null)
        {
            goldButton.onClick.RemoveAllListeners();
            goldButton.onClick.AddListener(OnGoldClicked);
            goldButton.interactable = true;
        }

        if (openCardButton != null)
        {
            openCardButton.onClick.RemoveAllListeners();
            openCardButton.onClick.AddListener(OnOpenCardClicked);
            openCardButton.interactable = true;
        }

        if (closeCardButton != null)
        {
            closeCardButton.onClick.RemoveAllListeners();
            closeCardButton.onClick.AddListener(OnCloseCardClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
        }
    }

    /// <summary>골드 버튼 클릭 시 골드를 획득한다.</summary>
    public void OnGoldClicked()
    {
        if (goldClaimed) return;

        goldClaimed = true;
        PlayerDataManager.Instance?.AddGold(goldReward);
        if (goldText != null) goldText.text = $"{goldReward}";

        // 골드 버튼 비활성화
        if (goldButton != null) goldButton.gameObject.SetActive(false);

        CheckAllClaimed();
    }

    /// <summary>카드 보상 열기 버튼 클릭 시 카드 선택지를 표시하고 다른 창을 비활성화한다.</summary>
    public void OnOpenCardClicked()
    {
        if (cardClaimed) return;

        if (cardChoicePanel != null) cardChoicePanel.SetActive(true);
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        if (openCardButton != null) openCardButton.gameObject.SetActive(false);
        if (goldButton != null && !goldClaimed) goldButton.gameObject.SetActive(false);

        // 처음 열 때만 생성
        if (!cardGenerated)
        {
            GenerateCardChoices();
            cardGenerated = true;
        }
    }

    /// <summary>카드 보상 창 닫기 버튼 클릭 시 카드 선택지 패널을 숨긴다.</summary>
    public void OnCloseCardClicked()
    {
        if (cardChoicePanel != null) cardChoicePanel.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(true);
        if (openCardButton != null && !cardClaimed) openCardButton.gameObject.SetActive(true);
        if (goldButton != null && !goldClaimed) goldButton.gameObject.SetActive(true);
    }


    /// <summary>카드 선택지를 랜덤으로 생성한다.</summary>
    private void GenerateCardChoices()
    {
        Logger.Log($"[RewardManager] GenerateCardChoices 호출. " +
           $"cardChoiceParent={cardChoiceParent}, " +
           $"cardChoicePrefab={cardChoicePrefab}, " +
           $"cardPool 수={cardPool?.Count}");

        if (cardChoiceParent == null || cardChoicePrefab == null) return;

        foreach (Transform child in cardChoiceParent)
            Destroy(child.gameObject);

        if (cardPool == null || cardPool.Count == 0)
        {
            Logger.LogWarning("[RewardManager] cardPool이 비어있습니다.");
            return;
        }

        var pool = new List<Card>(cardPool);
        int count = Mathf.Min(cardChoiceCount, pool.Count);

        for (int i = 0; i < count; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            Card card = pool[index];
            pool.RemoveAt(index);

            GameObject cardObj = Instantiate(cardChoicePrefab, cardChoiceParent);
            var display = cardObj.GetComponent<CardDisplay>();
            var button = cardObj.GetComponent<Button>();
            var movement = cardObj.GetComponent<CardMovement>();

            if (movement != null) movement.enabled = false;
            if (display != null) display.SetData(card);

            if (button != null)
            {
                Card capturedCard = card;
                button.onClick.AddListener(() => OnCardSelected(capturedCard));
            }
        }
    }

    /// <summary>카드 선택 시 덱에 추가한다.</summary>
    private void OnCardSelected(Card card)
    {
        if (cardClaimed) return;

        cardClaimed = true;
        PlayerDeck.Instance?.AddCardToDeck(card);
        Logger.Log($"[RewardManager] '{card.cardName}' 덱에 추가.");

        if (cardChoicePanel != null) cardChoicePanel.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        // 골드 아직 안 받았으면 골드 버튼 다시 보여주기
        if (!goldClaimed && goldButton != null)
            goldButton.gameObject.SetActive(true);

        CheckAllClaimed();
    }

    /// <summary>카드 선택을 스킵한다.</summary>
    public void OnSkipClicked()
    {
        if (cardClaimed) return;

        cardClaimed = true;

        if (cardChoicePanel != null) cardChoicePanel.SetActive(false);
        if (openCardButton != null) openCardButton.gameObject.SetActive(false);
        if (skipButton != null) skipButton.gameObject.SetActive(false);

        // 골드 아직 안 받았으면 골드 버튼 다시 보여주기
        if (!goldClaimed && goldButton != null)
            goldButton.gameObject.SetActive(true);

        CheckAllClaimed();
    }

    /// <summary>골드와 카드 보상을 모두 처리했으면 다음 씬으로 이동한다.</summary>
    private void CheckAllClaimed()
    {
        if (goldClaimed && cardClaimed)
        {
            Logger.Log("[RewardManager] 보상 모두 획득. 다음 씬으로 이동.");
            GoToNextScene();
        }
    }

    private void GoToNextScene()
    {
        SceneFlowManager.Instance?.LoadScene(SceneType.RoadMap);
    }
}