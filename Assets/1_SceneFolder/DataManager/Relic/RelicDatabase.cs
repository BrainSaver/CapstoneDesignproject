using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RelicDatabase", menuName = "Database/Relic Database")]
public class RelicDatabase : ScriptableObject
{
    // �ν����Ϳ��� ���� ���� ����Ʈ
    public List<RelicData> relics = new List<RelicData>();

    // ��Ÿ�ӿ� itemId�� ������ �˻��ϱ� ���� ��ųʸ�
    private Dictionary<string, RelicData> relicDict = new Dictionary<string, RelicData>();

    // ���� ���� �� �ʱ�ȭ
    public void Initialize()
    {
        relicDict.Clear();
        foreach (var relic in relics)
        {
            if (!relicDict.ContainsKey(relic.itemId))
            {
                relicDict.Add(relic.itemId, relic);
            }
        }
    }

    // itemId�� ���� ������ ã��
    public RelicData GetRelic(string itemId)
    {
        if (relicDict.Count == 0) Initialize();
        if (relicDict.TryGetValue(itemId, out RelicData data))
        {
            return data;
        }
        return null;
    }
}