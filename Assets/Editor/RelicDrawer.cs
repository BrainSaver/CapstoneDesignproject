#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(RelicIdAttribute))]
public class RelicIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 대상 변수가 string이 아닐 경우 기본 필드로 그립니다.
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        // 프로젝트 내부에서 생성된 RelicDatabase 에셋을 자동으로 검색합니다.
        string[] guids = AssetDatabase.FindAssets("t:RelicDatabase");
        if (guids == null || guids.Length == 0)
        {
            // 데이터베이스 에셋을 찾을 수 없으면 직접 타이핑할 수 있게 기본 텍스트 필드로 표시합니다.
            property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        RelicDatabase db = AssetDatabase.LoadAssetAtPath<RelicDatabase>(path);

        if (db == null || db.relics == null || db.relics.Count == 0)
        {
            property.stringValue = EditorGUI.TextField(position, label, property.stringValue);
            return;
        }

        // 드롭다운 메뉴에 표시될 텍스트 목록과 실제 매핑될 ID 목록을 구성합니다.
        List<string> displayNames = new List<string> { "None (비어있음)" };
        List<string> itemIds = new List<string> { "" };

        foreach (var relic in db.relics)
        {
            if (relic == null || string.IsNullOrEmpty(relic.itemId)) continue;

            // "한글 이름 (영어 이름) [등급]" 형태로 가독성 좋게 포맷팅합니다.
            displayNames.Add($"{relic.nameKo} ({relic.nameEn}) [{relic.rarity}]");
            itemIds.Add(relic.itemId);
        }

        // 현재 string 변수에 저장되어 있는 값의 인덱스를 구합니다.
        int currentIndex = itemIds.IndexOf(property.stringValue);
        if (currentIndex < 0) currentIndex = 0; // 찾지 못하면 None으로 초기화

        // 유니티 인스펙터에 드롭다운(Popup)을 출력합니다.
        int nextIndex = EditorGUI.Popup(position, label.text, currentIndex, displayNames.ToArray());

        // 사용자가 다른 유물을 선택했다면 실제 string 값을 itemId로 갱신합니다.
        if (nextIndex != currentIndex)
        {
            property.stringValue = itemIds[nextIndex];
        }
    }
}
#endif