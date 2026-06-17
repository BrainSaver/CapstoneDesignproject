using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class UIDragController : MonoBehaviour
{
    private RectTransform rectTransform;
    private Canvas parentCanvas;

    private bool isDragging;
    private Vector2 velocity; // 관성을 위한 현재 이동 속도

    [Header("드래그 설정")]
    [Tooltip("마우스를 놓았을 때 감속되는 정도입니다. 값이 클수록 빨리 멈추고, 작을수록 멀리 미끄러집니다.")]
    public float decelerationRate = 10f;

    public void StopMovement()
    {
        velocity = Vector2.zero;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        // 1. 마우스 클릭 시 (드래그 시작)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            velocity = Vector2.zero; // 새로운 드래그 시작 시 기존 관성 초기화       
        }

        // 2. 마우스 떼었을 시 (드래그 종료)
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        // 3. 이동 로직 처리
        if (isDragging)
        {
            // 프레임 간 마우스 이동량 계산
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
            {
                mouseDelta /= parentCanvas.scaleFactor;
            }

            // 가로(X축) 방향의 이동량만 추출하여 Y축 이동을 차단합니다.
            Vector2 horizontalMove = new Vector2(mouseDelta.x, 0f);

            // UI 이동 적용
            rectTransform.anchoredPosition += horizontalMove;

            // 마우스를 놓았을 때의 관성을 계산하기 위해 현재 속도를 저장합니다.     
            velocity = horizontalMove / Time.deltaTime;
        }
        else
        {
            // 4. 드래그 중이 아닐 때 (관성에 의한 부드러운 이동)
            if (velocity.magnitude > 0.1f)
            {
                // 남은 속도만큼 UI를 계속 이동시킵니다.
                rectTransform.anchoredPosition += velocity * Time.deltaTime;

                // 마찰력을 적용하여 속도를 서서히 0으로 줄입니다 (Lerp 활용).       
                velocity = Vector2.Lerp(velocity, Vector2.zero, decelerationRate * Time.deltaTime);
            }
            else
            {
                // 완전히 멈추면 연산을 줄이기 위해 속도를 정확히 0으로 맞춥니다.    
                velocity = Vector2.zero;
            }
        }
    }
}
