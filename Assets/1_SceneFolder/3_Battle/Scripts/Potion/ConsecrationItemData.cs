using UnityEngine;

[System.Serializable]
public class ConsecrationItemData
{
    public string ConsecrationItemId;    
    public string nameKo;
    public string nameEn;
    public string rarity;
    public string description;

    public Sprite ConsecrationItemIcon;
}

[System.Serializable]
public class ConsecrationItemDataList
{
    public ConsecrationItemData[] ConsecrationItem;
}