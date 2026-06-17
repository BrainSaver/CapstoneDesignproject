using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class RemoveCardUI : MonoBehaviour, IPointerClickHandler
{
    private Card myCardData;
    private ShopManager shopManager;

    [Header("UI 연결")]
    public TextMeshProUGUI priceText; // 카드 제거 비용 표시 텍스트

    public void Setup(Card card, ShopManager manager, int price)
    {
        myCardData = card;
        shopManager = manager;

        if (priceText != null)
        {
            priceText.text = $"{price} G";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopManager != null && myCardData != null)
        {
            shopManager.TryRemoveCard(myCardData, this.gameObject);
        }
    }
}