using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HP바 필 이미지와 수치 텍스트를 갱신하는 UI 컴포넌트.
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TextMeshProUGUI healthText;

    /// <summary>현재/최대 체력으로 HP바를 갱신한다.</summary>
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if (healthFillImage == null)
        {
            Logger.LogError("[HealthBar] healthFillImage가 연결되지 않았습니다.", this);
            return;
        }

        if (maxHealth <= 0)
        {
            healthFillImage.fillAmount = 0f;
            if (healthText != null) healthText.text = "0 / 0";
            return;
        }

        healthFillImage.fillAmount = Mathf.Clamp01((float)currentHealth / maxHealth);

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }

    /// <summary>HP바 오브젝트를 활성화/비활성화한다.</summary>
    public void SetHealthBarActive(bool isActive) => gameObject.SetActive(isActive);
}