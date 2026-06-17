using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardDisplay : MonoBehaviour
{
    [Header("Card Data")]
    public Card cardData;

    [Header("UI References")]
    public Image CardImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text manaCostText;
    public Image CardCoverImage;

    [Header("Rarity Sprites")]
    public Sprite commonGemSprite;
    public Sprite uncommonGemSprite;
    public Sprite rareGemSprite;

    private void Start()
    {
        if (cardData != null) UpdateCardDisplay();
    }

    public void SetData(Card card) { cardData = card; UpdateCardDisplay(); }
    public void Refresh() => UpdateCardDisplay();

    public void UpdateCardDisplay()
    {
        if (cardData == null) return;

        int finalCost = cardData.energyCost;
        if (Application.isPlaying && RelicManager.Instance != null)
        {
            RelicManager.Instance.OnGetCardCost(cardData, ref finalCost);
        }

        if (nameText) nameText.text = cardData.cardName;
        if (manaCostText) manaCostText.text = finalCost.ToString();

        int damage = 0, armor = 0, cards = 0, energy = 0, hpLost = 0, aoeDamage = 0, healthSet = 0;
        int bleed = 0, weaken = 0; int strength = 0; int dexterity = 0;

        bool hasDamageEffect = false;

        int currentStrength = 0;
        if (Application.isPlaying && PlayerStats.Instance != null)
            currentStrength = PlayerStats.Instance.TotalStrength;

        if (cardData.GetCardEffects() != null)
        {
            foreach (var effect in cardData.GetCardEffects())
            {
                if (effect == null) continue;
                Debug.Log($"[CardDisplay] effect.effectType={effect.effectType}"); // ✅ 추가

                int calculatedAmount = Application.isPlaying
                    ? cardData.GetCalculatedAmount(effect) : effect.amount;

                switch (effect.effectType)
                {
                    case CardEffectType.Damage:
                        int rawDmg = calculatedAmount + currentStrength;
                        rawDmg = Mathf.Max(0, rawDmg);
                        damage += Application.isPlaying
                            ? DullnessTracker.ApplyToDamage(rawDmg) : rawDmg;
                        hasDamageEffect = true;
                        break;

                    case CardEffectType.AOEDamage:
                        int rawAoe = calculatedAmount + currentStrength;
                        int appliedAoe = Application.isPlaying
                            ? DullnessTracker.ApplyToDamage(rawAoe) : rawAoe;
                        aoeDamage += appliedAoe;

                        damage += appliedAoe;
                        hasDamageEffect = true;
                        break;

                    case CardEffectType.DelayedDamage:
                        damage += calculatedAmount;
                        hasDamageEffect = true;
                        break;

                    case CardEffectType.AntiArmorDamage:
                    case CardEffectType.IgnoreArmorDamage:
                        int rawAnti = calculatedAmount;
                        damage += Application.isPlaying
                            ? DullnessTracker.ApplyToDamage(rawAnti) : rawAnti;
                        hasDamageEffect = true; // ✅ 추가
                        break;

                    case CardEffectType.DamageEqualArmor:
                        // ✅ 런타임에만 방어도 값 반영 가능
                        if (Application.isPlaying && PlayerStats.Instance != null)
                            damage += PlayerStats.Instance.Armor;
                        break;

                    case CardEffectType.LoseHealth:
                        hpLost += effect.amount;
                        break;

                    case CardEffectType.Move:
                        damage += calculatedAmount;
                        break;

                    case CardEffectType.GainStrength:
                        strength += effect.amount;
                        break;

                    case CardEffectType.GainDexterity:
                        dexterity += effect.amount;
                        break;

                    case CardEffectType.Armor:
                        int rawArmor = calculatedAmount;
                        if (Application.isPlaying && PlayerStats.Instance != null)
                            rawArmor += PlayerStats.Instance.TotalDexterity;
                        armor += rawArmor;
                        break;
                    case CardEffectType.DemonsMuscle:
                        damage += effect.amount;
                        hasDamageEffect = true; // ✅ 추가
                        break;

                    case CardEffectType.DemonsDust:
                        damage += effect.amount;
                        hasDamageEffect = true; // ✅ 추가
                        break;
                    case CardEffectType.DrawCard: cards += effect.amount; break;
                    case CardEffectType.GainEnergy: energy += effect.amount; break;
                    case CardEffectType.SetHealth: healthSet = effect.amount; break;
                    case CardEffectType.ApplyBleed: bleed += effect.amount; break;
                    case CardEffectType.ApplyDullnessToEnemy: weaken += effect.amount; break;
                }
            }
        }

        string desc = cardData.cardDescription ?? "";
        if (!string.IsNullOrEmpty(desc))
        {
            desc = desc.Replace("{damage}", hasDamageEffect ? damage.ToString() : "-");
            desc = desc.Replace("{armor}", armor > 0 ? armor.ToString() : "-");
            desc = desc.Replace("{cards}", cards > 0 ? cards.ToString() : "-");
            desc = desc.Replace("{energy}", energy > 0 ? energy.ToString() : "-");
            desc = desc.Replace("{hpLost}", hpLost > 0 ? hpLost.ToString() : "-");
            desc = desc.Replace("{aoeDamage}", aoeDamage > 0 ? aoeDamage.ToString() : "-");
            desc = desc.Replace("{healthSet}", healthSet > 0 ? healthSet.ToString() : "-");
            desc = desc.Replace("{bleed}", bleed > 0 ? bleed.ToString() : "-");
            desc = desc.Replace("{weaken}", weaken > 0 ? weaken.ToString() : "-");
            desc = desc.Replace("{strength}", strength > 0 ? strength.ToString() : "-");
            desc = desc.Replace("{dexterity}", dexterity > 0 ? dexterity.ToString() : "-");
        }

        if (descriptionText) descriptionText.text = desc;
        if (CardImage) CardImage.sprite = cardData.cardSprite;
    }
}
