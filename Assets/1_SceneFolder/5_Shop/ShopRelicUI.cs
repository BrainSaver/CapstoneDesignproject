using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ?곸젏?먯꽌 ?좊Ъ 援щℓ瑜??대떦?섎뒗 UI 而댄룷?뚰듃?낅땲??
/// </summary>
public class ShopRelicUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI nameText; // ?좊Ъ ?대쫫 ?쒖떆??(?좏깮)

    private RelicData myRelicData;
    private int myPrice;
    private ShopManager shopManager;

    /// <summary>
    /// ?좊Ъ UI瑜?珥덇린 ?ㅼ젙?⑸땲??
    /// </summary>
    /// <param name="relic">?좊Ъ ?곗씠??/param>
    /// <param name="price">媛寃?/param>
    /// <param name="manager">?곸젏 留ㅻ땲? 李몄“</param>
    public void Setup(RelicData relic, int price, ShopManager manager)
    {
        myRelicData = relic;
        myPrice = price;
        shopManager = manager;

        if (iconImage != null)
        {
            iconImage.sprite = relic.relicIcon;
            // ?꾩씠肄섏씠 ?놁쑝硫?湲곕낯 ?됱긽?대굹 ?щ챸??議곗젅
            iconImage.color = (relic.relicIcon != null) ? Color.white : new Color(1, 1, 1, 0.5f);
        }

        if (priceText != null)
        {
            priceText.text = myPrice.ToString() + " G";
        }

        if (nameText != null)
        {
            nameText.text = relic.nameKo;
        }

        // ?댄똻 異붽?
        HoverTooltip tooltip = GetComponent<HoverTooltip>();
        if (tooltip == null) tooltip = gameObject.AddComponent<HoverTooltip>();
        tooltip.title = relic.nameKo;
        tooltip.description = relic.description;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (shopManager != null && myRelicData != null)
        {
            shopManager.TryBuyRelic(myRelicData, myPrice, this.gameObject);
        }
    }

    // ?좊Ъ ?뺣낫 ?댄똻 ?쒖떆 ?깆쓣 異붽??섍퀬 ?띕떎硫?IPointerEnterHandler ?깆쓣 援ы쁽?????덉뒿?덈떎.
}

