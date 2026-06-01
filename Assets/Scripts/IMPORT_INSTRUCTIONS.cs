// ===== IMPORTING scene.gltf INTO UNITY =====
//
// Unity does NOT natively import .gltf / .glb files (as of Unity 6 / 2022 LTS).
// You need one of these two free methods:
//
// ─────────────────────────────────────────────────────────────────────────────
// METHOD A — UnityGLTF (Recommended, open-source)
// ─────────────────────────────────────────────────────────────────────────────
// 1. Open Unity → Window → Package Manager
// 2. Click the "+" button → "Add package from git URL"
// 3. Paste:   com.unity.cloud.gltfast
//    (or the older fork:  https://github.com/KhronosGroup/UnityGLTF.git )
// 4. Press Add. Unity will download and install it.
// 5. Drag scene.gltf into your Project Assets folder.
//    It will now be recognized and imported as a Model prefab.
// 6. Drag the imported model into your scene.
//
// ─────────────────────────────────────────────────────────────────────────────
// METHOD B — glTFast (Unity's own package, Unity 2020.3+)
// ─────────────────────────────────────────────────────────────────────────────
// 1. Window → Package Manager → search "glTFast" (by Unity)
// 2. Install it.
// 3. Drop scene.gltf into Assets — it imports automatically.
//
// ─────────────────────────────────────────────────────────────────────────────
// AFTER IMPORT — Assigning to FurnitureItem ScriptableObjects
// ─────────────────────────────────────────────────────────────────────────────
// 1. In Project, right-click → Create → Furniture → Furniture Item  (×6)
// 2. Name each one: Chair, Sofa, Table, Lamp, Bookshelf, Bed  (or your names)
// 3. For each FurnitureItem:
//      • Prefab  → drag the imported model (or a prefab you made from it)
//      • Icon    → a square Sprite (128×128) of the item
//      • Price   → set in Inspector
// 4. In Store scene, on each display object add StoreItem.cs component
//      → drag the matching FurnitureItem ScriptableObject into "Item Data"
//
// ─────────────────────────────────────────────────────────────────────────────
// BUILD SETTINGS — Add both scenes
// ─────────────────────────────────────────────────────────────────────────────
// File → Build Settings → drag Store.unity and House.unity into "Scenes In Build"
// Store should be index 0 (or set as default), House any other index.
// The exact scene names in those files must match:
//   CartUI.houseSceneName    = "House"
//   HouseInventoryUI.storeSceneName = "Store"
