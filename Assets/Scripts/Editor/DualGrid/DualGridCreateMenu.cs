#if UNITY_EDITOR
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;

namespace My.Map.DualGrid.Editor
{
    public static class DualGridCreateMenu
    {
        const string AssetMenuRoot = "Assets/Create/Map/Dual Grid/";

        [MenuItem(AssetMenuRoot + "Brush Registry", false, 1)]
        static void CreateBrushRegistry() => CreateAsset<DualGridBrushRegistry>("DualGridBrushRegistry");

        [MenuItem(AssetMenuRoot + "Palette", false, 2)]
        static void CreatePalette() => CreateAsset<DualGridTilePalette>("DualGridTilePalette");

        [MenuItem(AssetMenuRoot + "Display Tile", false, 3)]
        static void CreateDisplayTile() => CreateAsset<DualGridTile>("DualGridTile");

        [MenuItem("GameObject/2D Object/Dual Tile Map", false, 10)]
        static void CreateDualTileMap(MenuCommand command)
        {
            var parent = command.context as GameObject;
            var go = new GameObject("DualTileMap");
            Undo.RegisterCreatedObjectUndo(go, "Create Dual Tile Map");
            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }

            var map = go.AddComponent<DualTileMap>();
            DualTileMapEditor.CreateHierarchy(map);
            Selection.activeGameObject = go;
        }

        static void CreateAsset<T>(string defaultName) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            var path = AssetDatabase.GenerateUniqueAssetPath($"{defaultName}.asset");
            ProjectWindowUtil.CreateAsset(asset, path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif
