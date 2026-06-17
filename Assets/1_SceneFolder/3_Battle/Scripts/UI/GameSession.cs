using UnityEngine;

/// <summary>
/// 런타임 세션 통계(턴 수, 누적 데미지)를 관리하는 싱글턴.
/// DontDestroyOnLoad로 씬 전환에도 유지된다.
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public int turnsTaken;       // 경과 턴 수
    public int totalDamageDealt; // 총 가한 데미지
    public int totalDamageTaken; // 총 받은 데미지

    private PlayerDeck playerDeck;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>가한 데미지를 누적한다.</summary>
    public void AddDamageDealt(int amount)
    {
        if (amount > 0) totalDamageDealt += amount;
    }

    /// <summary>받은 데미지를 누적한다.</summary>
    public void AddDamageTaken(int amount)
    {
        if (amount > 0) totalDamageTaken += amount;
    }

    /// <summary>PlayerDeck을 세션에 등록한다.</summary>
    public void RegisterDeck(PlayerDeck deck)
    {
        playerDeck = deck;
        Logger.Log("[GameSession] PlayerDeck 등록 완료.");
    }

    /// <summary>세션 통계와 덱을 초기화한다.</summary>
    public void ResetStats()
    {
        turnsTaken = 0;
        totalDamageDealt = 0;
        totalDamageTaken = 0;

        if (playerDeck != null)
        {
            playerDeck.InitializeStartingDeck();
            Logger.Log("[GameSession] 덱 리셋 완료.");
        }
        else
            Logger.Log("[GameSession] PlayerDeck 없음. 배틀 시작 시 리셋됩니다.");
    }
}