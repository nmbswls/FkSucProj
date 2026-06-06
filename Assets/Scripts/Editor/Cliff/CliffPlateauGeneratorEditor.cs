#if UNITY_EDITOR
using My.Map.DualGrid;
using My.Map.DualGrid.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.Cliff.Editor
{
    [CustomEditor(typeof(CliffPlateauGenerator))]
    public class CliffPlateauGeneratorEditor : UnityEditor.Editor
    {
        CliffPlateauGenerator Gen => (CliffPlateauGenerator)target;

        SerializedProperty _tileSet;
        SerializedProperty _height;
        SerializedProperty _autoDualGridOffset;
        SerializedProperty _useDualGridOffset;
        SerializedProperty _clearBeforeGenerate;
        SerializedProperty _drawCliffGizmo;

        void OnEnable()
        {
            _tileSet = serializedObject.FindProperty("TileSet");
            _height = serializedObject.FindProperty("Height");
            _autoDualGridOffset = serializedObject.FindProperty("AutoDualGridOffset");
            _useDualGridOffset = serializedObject.FindProperty("UseDualGridOffset");
            _clearBeforeGenerate = serializedObject.FindProperty("ClearBeforeGenerate");
            _drawCliffGizmo = serializedObject.FindProperty("DrawCliffGizmo");
        }

        public override void OnInspectorGUI()
        {
            Gen.EnsureCliffChild();

            serializedObject.Update();

            EditorGUILayout.LabelField("Mode", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Plateau Mode", Gen.PlateauMode);
            }

            if (Gen.IsDualGridPlateau)
            {
                EditorGUILayout.HelpBox(
                    "Dual Grid: paint on Data child; Cliff uses +0.5 offset (same as View).\n" +
                    "Use DualTileMap → Create Hierarchy if Data/View are missing.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Standard: paint on Tilemap on this GameObject.\n" +
                    "Cliff child uses zero offset.",
                    MessageType.None);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Source Tilemap", Gen.SourceTilemap, typeof(Tilemap), true);
                EditorGUILayout.ObjectField("Cliff Tilemap", Gen.CliffTilemap, typeof(Tilemap), true);
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_tileSet);
            EditorGUILayout.PropertyField(_height);
            EditorGUILayout.PropertyField(_autoDualGridOffset);
            EditorGUILayout.PropertyField(_useDualGridOffset);
            EditorGUILayout.PropertyField(_clearBeforeGenerate);
            EditorGUILayout.PropertyField(_drawCliffGizmo);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            if (Gen.SourceTilemap == null)
            {
                if (Gen.IsDualGridPlateau)
                {
                    EditorGUILayout.HelpBox("DualTileMap.DataTilemap is not assigned. Click Create Hierarchy on DualTileMap.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Add a Tilemap on this GameObject, or add DualTileMap for dual grid mode.", MessageType.Warning);
                }
            }

            if (Gen.IsDualGridPlateau && !Gen.UseDualGridOffset)
            {
                EditorGUILayout.HelpBox(
                    "Dual Grid plateau: enable Use Dual Grid Offset (+0.5) to align cliff with grass view.",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Cliffs", GUILayout.Height(28)))
            {
                GenerateWithUndo();
            }

            if (GUILayout.Button("Clear Cliffs", GUILayout.Height(28)))
            {
                ClearWithUndo();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Ensure Cliff Child"))
            {
                Undo.RegisterFullObjectHierarchyUndo(Gen.gameObject, "Ensure Cliff Child");
                Gen.EnsureCliffChild();
                Gen.SyncDualGridOffset();
                EditorUtility.SetDirty(Gen);
            }

            if (GUILayout.Button("Sync Dual Grid Offset"))
            {
                var cliff = Gen.CliffTilemap;
                if (cliff != null)
                {
                    Undo.RecordObject(cliff.transform, "Sync Cliff Offset");
                    Gen.SyncDualGridOffset();
                    EditorUtility.SetDirty(cliff.transform);
                }
            }
        }

        void GenerateWithUndo()
        {
            if (!Validate())
            {
                return;
            }

            if (!Gen.IsDualGridPlateau)
            {
                Gen.EnsureStandardComponents();
            }

            Gen.EnsureCliffChild();
            Undo.RegisterCompleteObjectUndo(Gen.CliffTilemap, "Generate Cliffs");
            Gen.SyncDualGridOffset();
            Gen.GenerateCliffs();
            EditorUtility.SetDirty(Gen.CliffTilemap);
            SceneView.RepaintAll();
        }

        void ClearWithUndo()
        {
            Gen.EnsureCliffChild();
            if (Gen.CliffTilemap == null)
            {
                return;
            }

            Undo.RegisterCompleteObjectUndo(Gen.CliffTilemap, "Clear Cliffs");
            Gen.ClearCliffs();
            EditorUtility.SetDirty(Gen.CliffTilemap);
            SceneView.RepaintAll();
        }

        bool Validate()
        {
            if (Gen.SourceTilemap == null)
            {
                var msg = Gen.IsDualGridPlateau
                    ? "Assign DualTileMap Data layer (Create Hierarchy)."
                    : "Add a Tilemap on this GameObject.";
                EditorUtility.DisplayDialog("Cliff Generator", msg, "OK");
                return false;
            }

            if (Gen.TileSet == null)
            {
                EditorUtility.DisplayDialog("Cliff Generator", "Assign TileSet.", "OK");
                return false;
            }

            if (Gen.Height < 1)
            {
                EditorUtility.DisplayDialog("Cliff Generator", "Height must be >= 1.", "OK");
                return false;
            }

            return true;
        }

        [MenuItem("Assets/Create/Map/Cliff/Basic01 Cliff Tile Set", priority = 11)]
        public static void CreateBasic01CliffTileSet()
        {
            const string folder = "Assets/Arts/ClifTile";
            const string path = folder + "/Basic01CliffTileSet.asset";

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/Arts", "ClifTile");
            }

            if (AssetDatabase.LoadAssetAtPath<CliffTileSet>(path) != null)
            {
                EditorUtility.DisplayDialog("Cliff Tile Set", path + " already exists.", "OK");
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<CliffTileSet>(path);
                return;
            }

            var set = ScriptableObject.CreateInstance<CliffTileSet>();
            set.DefaultTile = LoadTile("ground_grasss_64");
            set.Body_Mid = LoadTile("ground_grasss_64");
            set.Body_LeftEnd = LoadTile("ground_grasss_63");
            set.Body_RightEnd = LoadTile("ground_grasss_57");
            set.Body_DepthJunctionLeft = LoadTile("ground_grasss_84");
            set.Body_DepthJunctionRight = LoadTile("ground_grasss_86");
            set.Body_ConvexLeft = LoadTile("ground_grasss_85");
            set.Body_ConvexRight = LoadTile("ground_grasss_71");
            set.Bottom_Mid = LoadTile("ground_grasss_81");
            set.Bottom_LeftEnd = LoadTile("ground_grasss_80");
            set.Bottom_RightEnd = LoadTile("ground_grasss_26");
            set.Bottom_DepthJunctionLeft = LoadTile("ground_grasss_59");
            set.Bottom_DepthJunctionRight = LoadTile("ground_grasss_60");

            AssetDatabase.CreateAsset(set, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = set;
            Debug.Log("[Cliff] Created " + path);
        }

        static TileBase LoadTile(string tileName)
        {
            var assetPath = $"Assets/Arts/Tile/basic_01/tile_asset/{tileName}.asset";
            return AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
        }

        [MenuItem("GameObject/2D Object/Cliff Dual Grid Plateau", false, 8)]
        static void CreateDualGridPlateau(MenuCommand command)
        {
            var parent = command.context as GameObject;
            var go = new GameObject("Plateau");
            Undo.RegisterCreatedObjectUndo(go, "Create Cliff Dual Grid Plateau");
            if (parent != null)
            {
                go.transform.SetParent(parent.transform, false);
            }

            go.AddComponent<DualTileMap>();
            go.AddComponent<CliffPlateauGenerator>();

            var dual = go.GetComponent<DualTileMap>();
            DualTileMapEditor.CreateHierarchy(dual);

            var tileSet = AssetDatabase.LoadAssetAtPath<CliffTileSet>("Assets/Arts/ClifTile/Basic01CliffTileSet.asset");
            var gen = go.GetComponent<CliffPlateauGenerator>();
            gen.TileSet = tileSet;

            Selection.activeGameObject = go;
        }
    }
}
#endif
