using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class ShopCardUI : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI priceText;
    private Card myCardData;
    private int myPrice;
    private ShopManager shopManager;

    public void Setup(Card card, int price, ShopManager manager)
    {
        myCardData = card;
        myPrice = price;
        shopManager = manager;

        // 프리팹 자식들 중에서 이름이 "Price"인 오브젝트를 자동으로 찾습니다.
        Transform priceObj = transform.Find("Price");

        if (priceObj != null)
        {
            priceText = priceObj.GetComponent<TextMeshProUGUI>();

            if (priceText != null)
            {
                // 찾은 Text칸에 가격을 표시합니다.
                priceText.text = myPrice.ToString() + " G";
            }
            else
            {
                Debug.LogError("[ShopCardUI] 'Price' 오브젝트를 찾았으나 TextMeshProUGUI 컴포넌트가 없습니다.");
            }
        }
        else
        {
            Debug.LogError("[ShopCardUI] 프리팹 안에 'Price'라는 이름의 자식 오브젝트가 없습니다.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopManager != null && myCardData != null)
        {
            shopManager.TryBuyCard(myCardData, myPrice, this.gameObject);
        }
    }
}