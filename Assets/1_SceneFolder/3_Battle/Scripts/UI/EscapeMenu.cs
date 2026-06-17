using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ESC 키 입력 시 지정된 씬으로 이동한다.
/// </summary>
public class EscapeMenu : MonoBehaviour
{
    [Header("이동할 씬")]
    [SerializeField] private SceneType targetScene; // ESC 시 이동할 씬

    [Header("설정")]

    private bool isEscaping = false; // 중복 호출 방지

    private void Update()
    {
        // ESC 키 입력 감지
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame &&
            !isEscaping)
        {
            OnEscapePressed();
        }
    }

    private void OnEscapePressed()
    {
        isEscaping = true;

        Logger.Log($"[EscapeMenu] ESC 입력 → {targetScene} 씬으로 이동.", this);

        // 핵심 수정: 씬을 전환할 때는 반드시 시간이 흘러야 코루틴과 페이드 애니메이션이 작동합니다.
        Time.timeScale = 1f;

        if (SceneFlowManager.Instance != null)
        {
            SceneFlowManager.Instance.LoadScene(targetScene);
        }
        else
        {
            Logger.LogError("[EscapeMenu] SceneFlowManager.Instance가 씬에 존재하지 않습니다!");
        }
    }
}