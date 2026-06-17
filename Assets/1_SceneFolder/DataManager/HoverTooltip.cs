using UnityEngine;
using UnityEngine.EventSystems;

public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string title;
    [TextArea(3, 10)]
    public string description;

    private bool isHovering = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (TooltipManager.Instance != null && !string.IsNullOrEmpty(title))
        {
            TooltipManager.Instance.ShowTooltip(title, description);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideTooltip();
        }
    }
    
    private void OnDisable()
    {
        if (isHovering && TooltipManager.Instance != null)
        {
            isHovering = false;
            TooltipManager.Instance.HideTooltip();
        }
    }
}