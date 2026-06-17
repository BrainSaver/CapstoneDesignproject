using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 드로우 더미 카드 수를 UI에 표시한다.
/// DeckManager.OnDrawPileChanged 이벤트로 자동 갱신된다.
/// </summary>
public class DrawPileUI : MonoBehaviour
{
    [Header("UI 참조")]
    public TMP_Text drawPileText;

    private void Start()
    {
        if (DeckManager.Instance != null)
            UpdateDrawPileUI();
        else
            Logger.LogError("[DrawPileUI] DeckManager 인스턴스를 찾을 수 없습니다.", this);
    }

    private void OnEnable() => DeckManager.OnDrawPileChanged += UpdateDrawPileUI;
    private void OnDisable() => DeckManager.OnDrawPileChanged -= UpdateDrawPileUI;

    /// <summary>드로우 더미 카드 수를 텍스트에 반영한다.</summary>
    public void UpdateDrawPileUI()
    {
        if (DeckManager.Instance == null || drawPileText == null) return;
        drawPileText.text = DeckManager.Instance.GetDrawPileCount().ToString();
    }
}