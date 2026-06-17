using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 덱을 저장하고 관리하는 싱글턴.
/// StartingDeckData 에셋에서 Card를 직접 참조해 덱을 구성한다.
/// </summary>
public class PlayerDeck : MonoBehaviour
{
    public static PlayerDeck Instance { get; private set; }
    public IReadOnlyList<Card> CurrentDeck => playerDeck;

    [SerializeField] private List<Card> playerDeck = new List<Card>();

    [Header("시작 덱 에셋")]
    [SerializeField] private StartingDeckData startingDeckData;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        GameSession.Instance?.RegisterDeck(this);
    }

    private void Start()
    {
        InitializeStartingDeck();
    }

    /// <summary>
    /// StartingDeckData 에셋의 Card 목록을 그대로 덱에 복사한다.
    /// 이름 조회 없이 에셋을 직접 참조하므로 누락 걱정이 없다.
    /// </summary>
    public void InitializeStartingDeck()
    {
        playerDeck.Clear();

        if (startingDeckData == null)
        {
            Logger.LogError("[PlayerDeck] StartingDeckData 에셋이 연결되지 않았습니다. " +
                            "Inspector에서 startingDeckData 슬롯에 에셋을 연결해주세요.", this);
            return;
        }

        if (startingDeckData.startingCards == null || startingDeckData.startingCards.Count == 0)
        {
            Logger.LogWarning("[PlayerDeck] StartingDeckData에 카드가 없습니다.", this);
            return;
        }

        foreach (Card card in startingDeckData.startingCards)
        {
            if (card != null)
                playerDeck.Add(card);
            else
                Logger.LogWarning("[PlayerDeck] StartingDeckData에 null인 카드 슬롯이 있습니다.", this);
        }

        Logger.Log($"[PlayerDeck] 덱 초기화 완료: {playerDeck.Count}장", this);
    }

    /// <summary>현재 덱의 복사본을 반환한다.</summary>
    public List<Card> GetDeck() => new List<Card>(playerDeck);

    /// <summary>Card 에셋을 직접 받아 덱에 추가한다.</summary>
    public void AddCardToDeck(Card card)
    {
        if (card == null)
        {
            Logger.LogError("[PlayerDeck] 추가하려는 카드가 null입니다.", this);
            return;
        }

        playerDeck.Add(card);
        Logger.Log($"[PlayerDeck] '{card.cardName}' 덱에 추가. 총 {playerDeck.Count}장", this);
    }

    /// <summary>Card 에셋을 직접 받아 덱에서 제거한다.</summary>
    public void RemoveCardFromDeck(Card card)
    {
        if (card == null) return;

        if (playerDeck.Contains(card))
        {
            playerDeck.Remove(card);
            Logger.Log($"[PlayerDeck] '{card.cardName}' 덱에서 제거. 총 {playerDeck.Count}장", this);
        }
        else
        {
            Logger.LogWarning($"[PlayerDeck] 덱에 존재하지 않는 카드입니다: {card.cardName}", this);
        }
    }
}
