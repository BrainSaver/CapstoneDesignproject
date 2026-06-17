using UnityEngine;
using TMPro;
using UnityEngine.UI; // Image 컴포넌트 추가
using System.Collections.Generic;

public class LocalSceneUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI newHPText;
    public TextMeshProUGUI newGoldText;

    [Header("Relic UI References")]
    public Transform relicContainer;
    public GameObject relicIconPrefab;

    private void Start()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnHPChanged += UpdateNewHPUI;
            PlayerDataManager.Instance.OnGoldChanged += UpdateNewGoldUI;
            PlayerDataManager.Instance.OnRelicsChanged += UpdateRelicUI; // 구독 추가

            PlayerDataManager.Instance.UpdateAllUI();
        }
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnHPChanged -= UpdateNewHPUI;
            PlayerDataManager.Instance.OnGoldChanged -= UpdateNewGoldUI;
            PlayerDataManager.Instance.OnRelicsChanged -= UpdateRelicUI; // 구독 해제
        }
    }

    private void UpdateNewHPUI(int current, int max)
    {
        if (newHPText != null) newHPText.text = $"{current} / {max}";
    }

    private void UpdateNewGoldUI(int gold)
    {
        if (newGoldText != null) newGoldText.text = $"{gold}";
    }

    // --- 유물 업데이트 로직 ---
    private void UpdateRelicUI(List<RelicData> relics)
    {
        if (relicContainer == null || relicIconPrefab == null) return;

        foreach (Transform child in relicContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (RelicData relic in relics)
        {
            GameObject newIcon = Instantiate(relicIconPrefab, relicContainer);
            Image iconImage = newIcon.GetComponent<Image>();

            if (iconImage != null && relic.relicIcon != null)
            {
                iconImage.sprite = relic.relicIcon;
            }
        }
    }
}