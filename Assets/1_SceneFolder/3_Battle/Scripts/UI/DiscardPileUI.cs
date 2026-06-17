using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 버린 카드 더미 카드 수를 UI에 표시한다.
/// DeckManager.OnDiscardPileChanged 이벤트로 자동 갱신된다.
/// </summary>
public class DiscardPileUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TMP_Text discardPileText;

    private void Start()
    {
        if (DeckManager.Instance != null)
            UpdateDiscardPileUI();
        else
            Logger.LogError("[DiscardPileUI] DeckManager 인스턴스를 찾을 수 없습니다.", this);
    }

    private void OnEnable() => DeckManager.OnDiscardPileChanged += UpdateDiscardPileUI;
    private void OnDisable() => DeckManager.OnDiscardPileChanged -= UpdateDiscardPileUI;

    private static readonly List<Card> DiscardPileCards = new();
    /// <summary>소멸 카드 목록 복사본을 반환한다.</summary>
    public static List<Card> GetDeck() => new List<Card>(DiscardPileCards);

    /// <summary>버린 카드 더미 카드 수를 텍스트에 반영한다.</summary>
    public void UpdateDiscardPileUI()
    {
        if (DeckManager.Instance == null || discardPileText == null) return;
        discardPileText.text = DeckManager.Instance.GetDiscardPileCount().ToString();
    }
}