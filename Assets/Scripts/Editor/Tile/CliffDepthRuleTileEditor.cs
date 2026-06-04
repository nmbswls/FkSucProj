#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using My.Map.CliffDepth;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.Map.CliffDepth.Editor
{
    [CustomEditor(typeof(CliffDepthRuleTile))]
    public class CliffDepthRuleTileEditor : RuleTileEditor
    {
        const float ExtraRuleHeight = 36f;

        CliffDepthRuleTile CliffTile => target as CliffDepthRuleTile;

        SerializedProperty TilingRulesProperty => serializedObject.FindProperty("m_TilingRules");

        public override void OnEnable()
        {
            base.OnEnable();
            MigrateAllRulesIfNeeded();
            HookReorderableList();
        }

        void HookReorderableList()
        {
            var listField = typeof(RuleTileEditor).GetField(
                "m_ReorderableList",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (listField?.GetValue(this) is not ReorderableList list)
            {
                return;
            }

            list.elementHeightCallback = index =>
            {
                if (CliffTile == null || index < 0 || index >= CliffTile.m_TilingRules.Count)
                {
                    return k_DefaultElementHeight + ExtraRuleHeight;
                }

                return GetElementHeight(CliffTile.m_TilingRules[index]) + ExtraRuleHeight;
            };

            var previous = list.onChangedCallback;
            list.onChangedCallback = reorderableList =>
            {
                previous?.Invoke(reorderableList);
                MigrateAllRulesIfNeeded();
            };
        }

        protected override void OnDrawElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var baseRect = new Rect(rect.x, rect.y, rect.width, rect.height - ExtraRuleHeight);
            base.OnDrawElement(baseRect, index, isactive, isfocused);

            if (TilingRulesProperty == null || index < 0 || index >= TilingRulesProperty.arraySize)
            {
                return;
            }

            var ruleProperty = TilingRulesProperty.GetArrayElementAtIndex(index);
            var leftProperty = ruleProperty.FindPropertyRelative(nameof(CliffDepthRuleTile.TilingRule.m_LeftDepthCheck));
            var rightProperty = ruleProperty.FindPropertyRelative(nameof(CliffDepthRuleTile.TilingRule.m_RightDepthCheck));
            if (leftProperty == null || rightProperty == null)
            {
                return;
            }

            float y = rect.yMax - ExtraRuleHeight;
            EditorGUI.PropertyField(
                new Rect(rect.xMin, y, rect.width, k_SingleLineHeight),
                leftProperty,
                new GUIContent("Left Depth"));
            y += k_SingleLineHeight;
            EditorGUI.PropertyField(
                new Rect(rect.xMin, y, rect.width, k_SingleLineHeight),
                rightProperty,
                new GUIContent("Right Depth"));
        }

        void MigrateAllRulesIfNeeded()
        {
            if (CliffTile?.m_TilingRules == null)
            {
                return;
            }

            bool changed = false;
            for (int i = 0; i < CliffTile.m_TilingRules.Count; i++)
            {
                if (CliffTile.m_TilingRules[i] is CliffDepthRuleTile.TilingRule)
                {
                    continue;
                }

                CliffTile.m_TilingRules[i] = CreateCliffRuleFrom(CliffTile.m_TilingRules[i]);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            EditorUtility.SetDirty(CliffTile);
            serializedObject.Update();
            DestroyPreview();
            CreatePreview();
        }

        static CliffDepthRuleTile.TilingRule CreateCliffRuleFrom(RuleTile.TilingRule oldRule)
        {
            var newRule = oldRule.Clone() as CliffDepthRuleTile.TilingRule;
            if (newRule != null)
            {
                return newRule;
            }

            return new CliffDepthRuleTile.TilingRule
            {
                m_Id = oldRule.m_Id,
                m_Neighbors = new List<int>(oldRule.m_Neighbors),
                m_NeighborPositions = new List<Vector3Int>(oldRule.m_NeighborPositions),
                m_RuleTransform = oldRule.m_RuleTransform,
                m_Sprites = oldRule.m_Sprites != null ? (Sprite[])oldRule.m_Sprites.Clone() : null,
                m_GameObject = oldRule.m_GameObject,
                m_MinAnimationSpeed = oldRule.m_MinAnimationSpeed,
                m_MaxAnimationSpeed = oldRule.m_MaxAnimationSpeed,
                m_PerlinScale = oldRule.m_PerlinScale,
                m_Output = oldRule.m_Output,
                m_ColliderType = oldRule.m_ColliderType,
                m_RandomTransform = oldRule.m_RandomTransform,
            };
        }

        static readonly Vector3Int[] DefaultNeighborPositions =
        {
            new Vector3Int(-1, 1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(1, 1, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, -1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, -1, 0),
        };

        const int NeighborDontCare = 0;

        const string AssetFolder = "Assets/Arts/Tile/basic_01/cliff_depth";
        const string SpriteFolder = "Assets/Arts/Tile/basic_01/tile_asset";

        [MenuItem("Assets/Create/Map/Tile/Basic01 Cliff Depth Tiles (x16)", priority = 12)]
        public static void CreateBasic01CliffDepthTiles()
        {
            Directory.CreateDirectory(AssetFolder);

            var template = BuildTemplateRuleTile();
            for (int depth = 0; depth < 16; depth++)
            {
                string path = $"{AssetFolder}/CliffDepth_D{depth}.asset";
                if (AssetDatabase.LoadAssetAtPath<CliffDepthRuleTile>(path) != null)
                {
                    continue;
                }

                var tile = Instantiate(template);
                tile.name = $"CliffDepth_D{depth}";
                tile.Depth = depth;
                AssetDatabase.CreateAsset(tile, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Created CliffDepth_D0..D15 under {AssetFolder}");
        }

        static CliffDepthRuleTile BuildTemplateRuleTile()
        {
            var sprite77 = LoadSprite("ground_grasss_77");
            var sprite63 = LoadSprite("ground_grasss_63");

            var tile = CreateInstance<CliffDepthRuleTile>();
            tile.Terrain = "basic_01";
            tile.m_DefaultSprite = sprite63;
            tile.m_DefaultColliderType = Tile.ColliderType.Sprite;
            tile.m_TilingRules = new List<RuleTile.TilingRule>
            {
                CreateTopEdgeRule(
                    sprite77,
                    CliffDepthRuleTile.DepthCheck.GreaterDepth,
                    CliffDepthRuleTile.DepthCheck.SameDepth,
                    NeighborDontCare),
                CreateTopEdgeRule(
                    sprite63,
                    CliffDepthRuleTile.DepthCheck.DontCare,
                    CliffDepthRuleTile.DepthCheck.SameDepth,
                    RuleTile.TilingRuleOutput.Neighbor.NotThis),
            };
            return tile;
        }

        static CliffDepthRuleTile.TilingRule CreateTopEdgeRule(
            Sprite sprite,
            CliffDepthRuleTile.DepthCheck leftDepth,
            CliffDepthRuleTile.DepthCheck rightDepth,
            int leftNeighbor)
        {
            return new CliffDepthRuleTile.TilingRule
            {
                m_Sprites = new[] { sprite },
                m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single,
                m_ColliderType = Tile.ColliderType.Sprite,
                m_RuleTransform = RuleTile.TilingRuleOutput.Transform.Fixed,
                m_NeighborPositions = new List<Vector3Int>(DefaultNeighborPositions),
                m_Neighbors = new List<int>
                {
                    RuleTile.TilingRuleOutput.Neighbor.NotThis,
                    RuleTile.TilingRuleOutput.Neighbor.NotThis,
                    RuleTile.TilingRuleOutput.Neighbor.NotThis,
                    leftNeighbor,
                    NeighborDontCare,
                    NeighborDontCare,
                    NeighborDontCare,
                    NeighborDontCare,
                },
                m_LeftDepthCheck = leftDepth,
                m_RightDepthCheck = rightDepth,
            };
        }

        static Sprite LoadSprite(string tileName)
        {
            var path = $"{SpriteFolder}/{tileName}.asset";
            var tileAsset = AssetDatabase.LoadAssetAtPath<Tile>(path);
            return tileAsset != null ? tileAsset.sprite : null;
        }
    }
}
#endif
