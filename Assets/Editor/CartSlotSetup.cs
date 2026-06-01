// CartSlotSetup.cs
// Place in Assets/Editor/
// Select your ShoppingCart3D GameObject, then run
// Tools > Setup Cart Slots to auto-position all slots inside the basket.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class CartSlotSetup : EditorWindow
{
    [MenuItem("Tools/Setup Cart Slots")]
    public static void ShowWindow()
    {
        GetWindow<CartSlotSetup>("Cart Slot Setup");
    }

    // Tweak these until items sit nicely inside your basket
    private float basketCenterY  = 0.45f;  // height inside basket
    private float spreadX        = 0.28f;  // left-right spread
    private float spreadZ        = 0.18f;  // front-back spread

    void OnGUI()
    {
        GUILayout.Label("Cart Slot Positioner", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Select your ShoppingCart3D in the hierarchy, " +
            "adjust the values below, then click Apply.", MessageType.Info);

        EditorGUILayout.Space();
        basketCenterY = EditorGUILayout.FloatField("Basket Center Y (height)", basketCenterY);
        spreadX       = EditorGUILayout.FloatField("Spread X (left-right)",    spreadX);
        spreadZ       = EditorGUILayout.FloatField("Spread Z (front-back)",    spreadZ);

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply to Selected ShoppingCart3D", GUILayout.Height(40)))
            Apply();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Resulting slot positions:", EditorStyles.boldLabel);
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = GetSlotLocalPos(i);
            EditorGUILayout.LabelField($"  Slot{i}: ({p.x:F2}, {p.y:F2}, {p.z:F2})");
        }
    }

    Vector3 GetSlotLocalPos(int index)
    {
        // 2 rows of 3: front row (z+), back row (z-)
        // columns: left (-x), center (0), right (+x)
        float x = (index % 3 - 1) * spreadX;
        float z = (index < 3 ? 1f : -1f) * spreadZ;
        return new Vector3(x, basketCenterY, z);
    }

    void Apply()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("No Selection",
                "Please select your ShoppingCart3D GameObject first.", "OK");
            return;
        }

        ShoppingCart3D cart = selected.GetComponent<ShoppingCart3D>();
        if (cart == null)
        {
            EditorUtility.DisplayDialog("Wrong Object",
                "Selected object doesn't have a ShoppingCart3D component.", "OK");
            return;
        }

        if (cart.slotPositions == null || cart.slotPositions.Length < 6)
        {
            EditorUtility.DisplayDialog("No Slots",
                "ShoppingCart3D.slotPositions needs 6 entries. " +
                "Make sure Slot0-5 are assigned in the Inspector.", "OK");
            return;
        }

        for (int i = 0; i < 6; i++)
        {
            Transform slot = cart.slotPositions[i];
            if (slot == null)
            {
                Debug.LogWarning($"Slot{i} is null — skipping.");
                continue;
            }

            slot.localPosition = GetSlotLocalPos(i);
            slot.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(slot);
        }

        EditorUtility.SetDirty(selected);
        Debug.Log("Cart slots positioned successfully!");
    }
}
#endif