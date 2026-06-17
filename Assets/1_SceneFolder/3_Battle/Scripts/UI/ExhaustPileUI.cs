using UnityEngine;
using TMPro;

/// <summary>
/// 소멸 묘지에 있는 카드 수를 UI에 표시한다.
/// ExhaustPile.OnExhaustChanged 이벤트로 자동 갱신된다.
/// </summary>
public class ExhaustPileUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI exhaustCountText; // 소멸 카드 수 텍스트

    private void OnEnable() => ExhaustPile.OnExhaustChanged += UpdateUI;
    private void OnDisable() => ExhaustPile.OnExhaustChanged -= UpdateUI;

    private void Start() => UpdateUI();

    /// <summary>소멸 카드 수를 텍스트에 반영한다.</summary>
    private void UpdateUI()
    {
        if (exhaustCountText != null)
            exhaustCountText.text = ExhaustPile.Count.ToString();
    }
}