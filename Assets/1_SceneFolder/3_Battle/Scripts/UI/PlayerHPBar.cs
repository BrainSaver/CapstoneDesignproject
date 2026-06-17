using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 플레이어 HP바를 갱신한다.
/// PlayerDataManager.OnHPChanged 이벤트로 자동 갱신된다.
/// </summary>
public class PlayerHPBar : MonoBehaviour
{
    [SerializeField] private Image hpSlider;  // HP 슬라이더
    [SerializeField] private TextMeshProUGUI hpText;    // HP 숫자 텍스트

    private void Start()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnHPChanged += UpdateHPBar;
            UpdateHPBar(PlayerDataManager.Instance.currentHP, PlayerDataManager.Instance.maxHP);
        }
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnHPChanged -= UpdateHPBar;
    }

    private void UpdateHPBar(int current, int max)
    {
        if (hpSlider != null)
            hpSlider.fillAmount = max > 0 ? (float)current / max : 0f;

        if (hpText != null)
            hpText.text = $"{current} / {max}";
    }
}