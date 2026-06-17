#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[CustomEditor(typeof(RelicDatabase))]
public class RelicDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // ���� �ν����� UI �׸���
        DrawDefaultInspector();

        RelicDatabase db = (RelicDatabase)target;

        GUILayout.Space(20);
        if (GUILayout.Button("relic.json���� ���� ������ ����ȭ", GUILayout.Height(30)))
        {
            ImportFromJSON(db);
        }
    }

    private void ImportFromJSON(RelicDatabase db)
    {
        // JSON ���� ���� â ����
        string path = EditorUtility.OpenFilePanel("Select Relic JSON", Application.dataPath, "json");

        if (!string.IsNullOrEmpty(path))
        {
            string json = File.ReadAllText(path);

            // ���� �ۼ��صν� RelicDataList ������ ���� �Ľ�
            RelicDataList loadedData = JsonUtility.FromJson<RelicDataList>(json);

            if (loadedData != null && loadedData.relics != null)
            {
                foreach (var newRelic in loadedData.relics)
                {
                    // ���� �����Ϳ� ���� itemId�� �ִ��� Ȯ��
                    var existingRelic = db.relics.FirstOrDefault(r => r.itemId == newRelic.itemId);

                    if (existingRelic != null)
                    {
                        // �̹� �����Ѵٸ� �̹���(relicIcon)�� �����ϰ� �ؽ�Ʈ �����͸� ����
                        existingRelic.nameKo = newRelic.nameKo;
                        existingRelic.nameEn = newRelic.nameEn;
                        existingRelic.rarity = newRelic.rarity;
                        existingRelic.isEventExclusive = newRelic.isEventExclusive;
                        existingRelic.description = newRelic.description;
                    }
                    else
                    {
                        // ���ο� �����̶�� ����Ʈ�� �߰�
                        db.relics.Add(newRelic);
                    }
                }

                // ����Ƽ�� ���� ���� ���� �� ������ �˸�
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                Debug.Log($"[RelicDatabase] ���������� {loadedData.relics.Length}���� ���� �����͸� ����ȭ�߽��ϴ�.");
            }
        }
    }
}
#endif