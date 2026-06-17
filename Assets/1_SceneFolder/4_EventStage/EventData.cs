using UnityEngine;
using System.Collections.Generic;

public enum EventEffectType
{
    None, Heal, Damage, GainGold, LoseGold,
    GainCard, RemoveCard, UpgradeCard, GainRelic, LoseRelic,
    GainMaxHP, LoseMaxHP, DowngradeCard, TeleportToBeforeBoss, ChanceRoll,
    TransformCard, CopyCard, UpgradeNextRewards, HalveNextEnemyHP, GainPotion, LosePotion,
    MapPassive_FirstTurnBlock, MapPassive_FirstTurnEnergy, DoubleCardDamage, DoubleCardBlock,
    SneckoCardEffect,
    InnateCardEffect,                // 개시: 선택한 카드가 전투 시작 시 무조건 첫 드로우(첫 손패)에 들어온다
    GuaranteeNextEventRelic,   // 다음 ?방 유물 확정 패시브
    InvertHealth
}

[System.Serializable]
public class EventEffect
{
    public EventEffectType effectType;
    public int effectAmount;
    public string stringData;
}

[System.Serializable]
public class EventChoice
{
    [Header("버튼 텍스트")]
    public string choiceTitle;
    [TextArea(2, 3)]
    public string choiceDescription;
    [Header("발동할 효과들")]
    public List<EventEffect> effects = new List<EventEffect>();
}

[CreateAssetMenu(fileName = "New Event", menuName = "Game Data/Event Data")]
public class EventData : ScriptableObject
{
    [Header("이벤트 연출")]
    public string eventTitle;
    public Sprite eventImage;
    [TextArea(3, 10)]
    public string eventText;
    [Header("선택지 목록")]
    public List<EventChoice> choices = new List<EventChoice>();
}