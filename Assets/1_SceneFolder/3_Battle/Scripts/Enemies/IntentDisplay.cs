using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 적의 다음 행동 인텐트(아이콘 + 수치 텍스트)를 UI에 표시한다.
/// </summary>
public class IntentDisplay : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private Image intentIconImage;      // 인텐트 아이콘 이미지
    [SerializeField] private TextMeshProUGUI intentDescriptionText; // 인텐트 수치 텍스트

    /// <summary>EnemyIntent를 받아 UI를 갱신한다.</summary>
    public void SetIntent(EnemyIntent intent)
    {
        if (intent == null) { ClearIntent(); return; }

        // 아이콘 설정
        if (intentIconImage != null)
        {
            intentIconImage.sprite = intent.Icon;
            intentIconImage.enabled = (intent.Icon != null);
        }
        else
            Logger.LogWarning("[IntentDisplay] intentIconImage가 연결되지 않았습니다.", this);

        // 텍스트 설정
        if (intentDescriptionText != null)
        {
            intentDescriptionText.text = intent.Description;
            intentDescriptionText.gameObject.SetActive(!string.IsNullOrEmpty(intent.Description));
        }
        else
            Logger.LogWarning("[IntentDisplay] intentDescriptionText가 연결되지 않았습니다.", this);

        gameObject.SetActive(intent.Icon != null || !string.IsNullOrEmpty(intent.Description));
    }

    /// <summary>인텐트 UI를 초기화하고 숨긴다.</summary>
    public void ClearIntent()
    {
        if (intentIconImage != null)
        {
            intentIconImage.sprite = null;
            intentIconImage.enabled = false;
        }
        if (intentDescriptionText != null)
        {
            intentDescriptionText.text = string.Empty;
            intentDescriptionText.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }

    public void ShowIntent() => gameObject.SetActive(true);
    public void HideIntent() => gameObject.SetActive(false);
}