using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 전진/후퇴 UI 버튼.
/// 항상 활성화 상태이며 코스트 부족 시 흔들림 피드백을 제공한다.
/// </summary>
public class MoveButton : MonoBehaviour
{
    public enum MoveDirection { Forward = -1, Backward = 1 }

    [Header("이동 설정")]
    [SerializeField] private MoveDirection direction = MoveDirection.Forward;
    [SerializeField] private int moveCost = 1;

    [Header("UI 참조")]
    [SerializeField] private Button button;
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private TextMeshProUGUI costText;

    private void Start()
    {
        if (button == null) button = GetComponent<Button>();
        if (buttonRect == null) buttonRect = GetComponent<RectTransform>();

        button?.onClick.AddListener(OnMoveButtonClicked);

        if (costText != null) costText.text = $"{costText.text}";
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveListener(OnMoveButtonClicked);
    }

    private void OnMoveButtonClicked()
    {
        // 플레이어 턴 아니면 무시
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn) return;
        if (BattleManager.Instance?.IsPlayerInputLocked ?? true) return;

        // 코스트 부족 시 흔들림 피드백
        if (!MovePointManager.Instance.HasEnoughPoints(moveCost))
        {
            Logger.Log("[MoveButton] 이동 코스트 부족.");
            ShakeButton();
            return;
        }

        // 거리 한계 체크
        if (direction == MoveDirection.Forward &&
            DistanceManager.Instance.CurrentDistance <= 1)
        {
            Logger.Log("[MoveButton] 이미 최소 거리입니다.");
            ShakeButton();
            return;
        }

        if (direction == MoveDirection.Backward &&
            DistanceManager.Instance.CurrentDistance >= 2)
        {
            Logger.Log("[MoveButton] 이미 최대 거리입니다.");
            ShakeButton();
            return;
        }

        // 이동 코스트 소모 + 거리 이동
        MovePointManager.Instance.UsePoints(moveCost);
        int actual = DistanceManager.Instance.Move((int)direction);

        Logger.Log($"[MoveButton] {(direction == MoveDirection.Forward ? "전진" : "후퇴")} " +
                   $"실제 이동={actual}, 현재 거리={DistanceManager.Instance.CurrentDistance}, " +
                   $"남은 코스트={MovePointManager.Instance.CurrentPoints}");

        AudioManager.Instance?.PlaySFX("Move_Step");
    }

    /// <summary>사용 불가 시 버튼을 흔드는 피드백.</summary>
    private void ShakeButton()
    {
        if (buttonRect == null) return;
        DOTween.Kill(buttonRect);
        buttonRect.DOShakePosition(0.3f, strength: 8f, vibrato: 20);
        AudioManager.Instance?.PlaySFX("Card_Invalid");
    }
}