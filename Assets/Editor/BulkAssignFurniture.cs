using UnityEditor;
using UnityEngine;

public class BulkAssignFurniture : EditorWindow
{
    [MenuItem("Tools/Bulk Assign Furniture")]
    static void Open() => GetWindow<BulkAssignFurniture>("Bulk Assign");

    FurnitureItem target;
    string folderPath = "Assets/Furniture Mega Pack/Prefabs/Beds";

    void OnGUI()
    {
        GUILayout.Label("Bulk Assign Prefabs to Furniture SO", EditorStyles.boldLabel);
        GUILayout.Space(10);

        target = (FurnitureItem)EditorGUILayout.ObjectField(
            "Target SO", target, typeof(FurnitureItem), false);

        folderPath = EditorGUILayout.TextField("Prefab Folder", folderPath);

        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Assign All Prefabs in Folder"))
        {
            if (target == null) { Debug.LogError("No target SO selected!"); return; }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
            if (guids.Length == 0) { Debug.LogError($"No prefabs found in {folderPath}"); return; }

            var prefabs = new GameObject[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));

            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty("prefabVariants");
            prop.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            so.ApplyModifiedProperties();

            Debug.Log($"Assigned {prefabs.Length} prefabs to {target.name}");
        }
    }
}