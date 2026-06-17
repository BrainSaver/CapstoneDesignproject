using UnityEngine;

[System.Serializable]
public class RelicData
{
    public string itemId;
    public string nameKo;
    public string nameEn;
    public string rarity;
    public bool isEventExclusive;

    public string description;

    // Sprite is not in JSON, but useful for UI. 
    // We can load it from Resources using itemId if needed.
    public Sprite relicIcon; 
}

[System.Serializable]
public class RelicDataList
{
    public RelicData[] relics;
}