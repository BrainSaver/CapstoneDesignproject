using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 축성물 데이터베이스 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "ConsecrationItemDatabase", menuName = "Database/Consecration Item Database")]
public class ConsecrationItemDatabase : ScriptableObject
{
    [Header("축성물 목록")]
    public List<ConsecrationItemData> items = new List<ConsecrationItemData>();

    private Dictionary<string, ConsecrationItemData> itemDict = new();

    public void Initialize()
    {
        itemDict.Clear();
        foreach (var item in items)
        {
            if (item != null && !string.IsNullOrEmpty(item.ConsecrationItemId))
            {
                if (!itemDict.ContainsKey(item.ConsecrationItemId))
                    itemDict.Add(item.ConsecrationItemId, item);
            }
        }
    }

    public ConsecrationItemData GetItem(string ConsecrationItemId)
    {
        if (itemDict == null || itemDict.Count == 0) Initialize();
        return itemDict.TryGetValue(ConsecrationItemId, out var data) ? data : null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ConsecrationItemDatabase))]
public class ConsecrationItemDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ConsecrationItemDatabase db = (ConsecrationItemDatabase)target;

        GUILayout.Space(20);
        if (GUILayout.Button("ConsecrationItem.json에서 데이터 동기화", GUILayout.Height(30)))
            ImportFromJSON(db);
    }

    private void ImportFromJSON(ConsecrationItemDatabase db)
    {
        string path = EditorUtility.OpenFilePanel("Select Potion JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        ConsecrationItemDataList loadedData = JsonUtility.FromJson<ConsecrationItemDataList>(json);

        if (loadedData?.ConsecrationItem == null) return;

        foreach (var newItem in loadedData.ConsecrationItem)
        {
            var existing = db.items.FirstOrDefault(i => i.ConsecrationItemId == newItem.ConsecrationItemId);
            if (existing != null)
            {
                // 기존 항목 텍스트 데이터만 업데이트 (아이콘은 유지)
                existing.nameKo = newItem.nameKo;
                existing.nameEn = newItem.nameEn;
                existing.rarity = newItem.rarity;
                existing.description = newItem.description;
            }
            else
            {
                // 새 항목 추가
                db.items.Add(newItem);
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ConsecrationItemDatabase] {loadedData.ConsecrationItem.Length}개 동기화 완료.");
    }
}
#endif