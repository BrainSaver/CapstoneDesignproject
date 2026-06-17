using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ?곸젏?먯꽌 異뺤꽦臾?Potion) 援щℓ瑜??대떦?섎뒗 UI 而댄룷?뚰듃?낅땲??
/// </summary>
public class ShopPotionUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI nameText;

    private ConsecrationItemData myPotionData;
    private int myPrice;
    private ShopManager shopManager;

    public void Setup(ConsecrationItemData potion, int price, ShopManager manager)
    {
        myPotionData = potion;
        myPrice = price;
        shopManager = manager;

        if (iconImage != null)
        {
            iconImage.sprite = potion.ConsecrationItemIcon;
            iconImage.color = (potion.ConsecrationItemIcon != null) ? Color.white : new Color(1, 1, 1, 0.5f);
        }

        if (priceText != null)
        {
            priceText.text = myPrice.ToString() + " G";
        }

        if (nameText != null)
        {
            nameText.text = potion.nameKo;
        }

        // ?댄똻 異붽?
        HoverTooltip tooltip = GetComponent<HoverTooltip>();
        if (tooltip == null) tooltip = gameObject.AddComponent<HoverTooltip>();
        tooltip.title = potion.nameKo;
        tooltip.description = potion.description;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopManager != null && myPotionData != null)
        {
            shopManager.TryBuyPotion(myPotionData, myPrice, this.gameObject);
        }
    }
}

