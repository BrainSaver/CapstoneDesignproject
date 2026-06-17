using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 축성물 슬롯 UI. 드래그해서 사용한다.
/// 적 위에 드롭 → 적에게 사용
/// 빈 공간에 드롭 → 자신에게 사용 (단, SlotParent 영역 내 드롭 시 원위치)
/// </summary>
public class ConsecrationItemSlotUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI 참조")]
    [SerializeField] private Image potionIcon;
    [SerializeField] private CanvasGroup canvasGroup;

    // [중요] 포션 슬롯들을 감싸고 있는 최상위 부모 UI 패널을 인스펙터에서 넣어주세요.
    [SerializeField] private RectTransform slotParent;

    [Header("드래그 설정")]
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.7f, 1f);
    [SerializeField] private Color defaultColor = Color.white;

    private ConsecrationItemData consecrationItemData;
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private bool isDragging = false;

    private Canvas mainCanvas;
    private Transform originalParent;
    private int originalSiblingIndex;


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        // [수정] Image가 자식 오브젝트에 있을 수 있으므로 GetComponentInChildren 사용
        if (potionIcon == null) potionIcon = GetComponentInChildren<Image>();

        mainCanvas = GetComponentInParent<Canvas>();

        if (slotParent == null)
        {
            slotParent = transform.parent as RectTransform;
        }
    }

    /// <summary>축성물 데이터를 설정하고 UI를 갱신한다.</summary>
    public void SetPotion(ConsecrationItemData data)
    {
        consecrationItemData = data;

        if (consecrationItemData != null && potionIcon != null)
        {
            potionIcon.sprite = data.ConsecrationItemIcon;
            potionIcon.enabled = true;
            // [수정] 데이터가 있을 때 투명도를 1로 (필요시)
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }
        else if (potionIcon != null)
        {
            potionIcon.sprite = null;
            potionIcon.enabled = false;
            // [수정] 슬롯 자체를 끄지 않고 아이콘만 비활성화하여 배경은 보이게 함
        }

        // 슬롯 오브젝트는 항상 활성화 상태 유지 (빈 슬롯 배경 표시를 위함)
        gameObject.SetActive(true);
    }

    // ── 포인터 이벤트 ────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (consecrationItemData == null || isDragging || eventData.dragging) return;
        potionIcon.DOColor(hoverColor, 0.15f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging) return;
        potionIcon.DOColor(defaultColor, 0.15f);
    }

    // ── 드래그 이벤트 ────────────────────────────────────────────

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (consecrationItemData == null || !CanUse()) return;
        if (eventData.pointerDrag != gameObject) return;

        isDragging = true;

        originalParent = transform.parent;
        originalSiblingIndex = rectTransform.GetSiblingIndex();
        originalPosition = rectTransform.anchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }

        if (mainCanvas != null)
        {
            transform.SetParent(mainCanvas.transform, true);
        }
        rectTransform.SetAsLastSibling();

        AudioManager.Instance?.PlaySFX("Card_Select");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerDrag != gameObject) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (mainCanvas != null && mainCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mainCanvas.transform as RectTransform,
                mousePos,
                mainCanvas.worldCamera,
                out Vector2 localPoint
            );
            rectTransform.position = Vector3.Lerp(
                rectTransform.position,
                mainCanvas.transform.TransformPoint(localPoint),
                0.25f
            );
        }
        else
        {
            rectTransform.position = Vector3.Lerp(rectTransform.position, mousePos, 0.25f);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }

        potionIcon.DOColor(defaultColor, 0.15f);

        // 우선 원래 계층 구조로 안전하게 복귀시킵니다.
        if (originalParent != null)
        {
            transform.SetParent(originalParent, false);
            rectTransform.SetSiblingIndex(originalSiblingIndex);
        }

        // [수정] 마우스 위치가 SlotParent UI 사각형 영역 내부인지 정확한 좌표로 체크합니다.
        if (IsMouseInsideSlotParent())
        {
            ReturnToOriginal(); // SlotParent 위에서 놓았다면 원위치 후 종료
            return;
        }

        Enemy enemy = GetEnemyUnderCursor();

        // 적 대상 축성물 → 적 위에 드롭해야 사용 가능
        if (IsEnemyTargetItem())
        {
            if (enemy != null)
                UseItem(enemy);        // 적에게 사용
            else
                ReturnToOriginal();    // 적 아닌 곳에 드롭 → 원위치
        }
        // 자신 대상 축성물 → 적 이외의 게임 공간에 드롭 시 사용
        else if (IsSelfTargetItem())
        {
            if (enemy == null)
                UseItem(null);         // 자신에게 사용
            else
                ReturnToOriginal();    // 적 위에 드롭 → 원위치
        }
        // 그 외 → 어디든 드롭 시 사용
        else
        {
            UseItem(null);
        }
    }

    // ── 축성물 사용 ──────────────────────────────────────────────

    private void UseItem(Enemy target)
    {
        if (consecrationItemData == null) return;
        if (!CanUse()) { ReturnToOriginal(); return; }

        AudioManager.Instance?.PlaySFX("Potion_Use");
        ConsecrationItemManager.Instance.UsePotion(consecrationItemData, target);
    }

    private void ReturnToOriginal()
    {
        rectTransform.DOAnchorPos(originalPosition, 0.5f).SetEase(Ease.OutBack);
    }

    private bool CanUse()
    {
        if (consecrationItemData == null) return false;
        if (TurnManager.Instance == null) return false;
        if (!TurnManager.Instance.IsPlayerTurn) return false;
        if (BattleManager.Instance?.IsPlayerInputLocked ?? true) return false;
        return true;
    }

    /// <summary>적을 대상으로 하는 축성물인지 확인한다.</summary>
    private bool IsEnemyTargetItem()
    {
        if (consecrationItemData == null) return false;
        return consecrationItemData.description.Contains("적에게") ||
               consecrationItemData.description.Contains("적 전체") ||
               consecrationItemData.description.Contains("선택한 적") ||
               consecrationItemData.description.Contains("무작위 적") ||
               consecrationItemData.description.Contains("모든 적");
    }

    /// <summary>자신을 대상으로 하는 축성물인지 확인한다.</summary>
    private bool IsSelfTargetItem()
    {
        if (consecrationItemData == null) return false;
        return consecrationItemData.description.Contains("체력") ||
               consecrationItemData.description.Contains("회복") ||
               consecrationItemData.description.Contains("방어") ||
               consecrationItemData.description.Contains("에너지") ||
               consecrationItemData.description.Contains("드로우") ||
               consecrationItemData.description.Contains("카드") ||
               consecrationItemData.description.Contains("플레이어");
    }

    /// <summary>커서 아래 적을 레이캐스트로 찾는다.</summary>
    private Enemy GetEnemyUnderCursor()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            var enemy = r.gameObject.GetComponentInParent<Enemy>();
            if (enemy != null) return enemy;
        }
        return null;
    }

    /// <summary>[수정] 마우스 좌표가 SlotParent의 사각형 범위 안에 포함되는지 검사합니다.</summary>
    private bool IsMouseInsideSlotParent()
    {
        if (slotParent == null) return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // RectTransformUtility를 사용하여 스크린 좌표가 해당 Rect 안 영역에 속해있는지 판별합니다.
        return RectTransformUtility.RectangleContainsScreenPoint(slotParent, mousePos, mainCanvas.worldCamera);
    }
}