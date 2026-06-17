using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 현재 거리와 구간 이름을 UI에 표시한다.
/// </summary>
public class DistanceUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private TextMeshProUGUI distanceText;  // 거리 숫자 텍스트

    private void OnEnable() => DistanceManager.OnDistanceChanged += OnDistanceChanged;
    private void OnDisable() => DistanceManager.OnDistanceChanged -= OnDistanceChanged;

    private void Start()
    {
        if (DistanceManager.Instance != null)
            UpdateUI(DistanceManager.Instance.CurrentDistance);
    }

    private void OnDistanceChanged(int prev, int current) => UpdateUI(current);

    private void UpdateUI(int distance)
    {
        if (distanceText != null) distanceText.text = $"거리: {distance}";
    }
}