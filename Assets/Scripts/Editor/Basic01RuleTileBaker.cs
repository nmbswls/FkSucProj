#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ruccho.Fang;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.EditorTools
{
    // 从 Fang basic_01 烘焙 Unity Rule Tile（47 邻接规则 + Sprite）
    public static class Basic01RuleTileBaker
    {
        const string FangAssetPath = "Assets/Arts/AutoTile/basic_01.asset";
        const string DefaultAtlasPath = "Assets/Arts/AutoTile/tile_47_basic.texture2D";
        const string OutputFolder = "Assets/Arts/Tile/basic_01";
        const string PngPath = OutputFolder + "/Basic01Tiles.png";
        const string RuleTilePath = OutputFolder + "/Basic01TerrainRuleTile.asset";

        const int CombinationCount = 47;
        const int PixelsPerUnit = 32;

        // Unity RuleTile 默认邻居顺序 → Fang GetNeighborValue 位索引
        static readonly int[] FangBitByUnityNeighborIndex = { 6, 5, 4, 7, 3, 0, 1, 2 };

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

        // batchmode: -executeMethod My.EditorTools.Basic01RuleTileBaker.BakeFromFang
        public static void BakeFromFang()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultAtlasPath);
            if (atlas == null)
            {
                Debug.LogError($"Default atlas not found: {DefaultAtlasPath}");
                return;
            }

            BakeFromFang(atlas);
        }

        [MenuItem("Assets/Tools/Tile/Bake Basic01 Rule Tile From Fang", priority = 200)]
        static void BakeFromFangMenu()
        {
            var atlas = ResolveAtlasTexture(Selection.activeObject as Texture2D);
            if (atlas == null)
                return;
            BakeFromFang(atlas);
        }

        [MenuItem("Assets/Tools/Tile/Bake Basic01 Rule Tile From Fang", true)]
        static bool BakeFromFangMenuValidate()
        {
            return ResolveAtlasTexture(Selection.activeObject as Texture2D, silent: true) != null;
        }

        public static void BakeFromFang(Texture2D atlasTexture)
        {
            if (atlasTexture == null)
            {
                Debug.LogError("BakeFromFang requires a Texture2D atlas.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Bake Basic01 Rule Tile",
                    $"图集: {AssetDatabase.GetAssetPath(atlasTexture)}\n" +
                    "将导出 PNG、切图并覆盖 " + RuleTilePath + " 的全部 Rule 与 Sprite。\n" +
                    "若已手动调整邻居规则，请先备份该 asset。",
                    "继续烘焙",
                    "取消"))
                return;

            EnsureOutputFolder();
            var fang = LoadFangAsset();

            ExportPng(atlasTexture, PngPath);
            var spriteRects = CollectSpriteRects(fang);
            if (spriteRects.Count != CombinationCount)
            {
                Debug.LogError($"Expected {CombinationCount} combination sprites, got {spriteRects.Count}.");
                return;
            }

            ApplySpriteSheetMeta(PngPath, spriteRects);
            var sprites = LoadSpritesByIndex(PngPath, CombinationCount);
            var ruleTile = BuildRuleTile(fang, sprites);
            SaveRuleTile(ruleTile, RuleTilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Baked Rule Tile: {RuleTilePath}, atlas: {AssetDatabase.GetAssetPath(atlasTexture)}, png: {PngPath}");
        }

        static Texture2D ResolveAtlasTexture(Texture2D selected, bool silent = false)
        {
            if (selected != null)
                return selected;

            var fallback = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultAtlasPath);
            if (fallback != null)
                return fallback;

            if (!silent)
                Debug.LogError(
                    "Select a Texture2D in Project, or place atlas at " + DefaultAtlasPath);
            return null;
        }

        static void EnsureOutputFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                const string parent = "Assets/Arts/Tile";
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets/Arts", "Tile");
                AssetDatabase.CreateFolder(parent, "basic_01");
            }
        }

        static FangAutoTile LoadFangAsset()
        {
            var fang = AssetDatabase.LoadAssetAtPath<FangAutoTile>(FangAssetPath);
            if (fang == null)
                throw new InvalidOperationException($"Fang asset not found: {FangAssetPath}");
            return fang;
        }

        static void ExportPng(Texture2D texture, string path)
        {
            var readable = GetReadableCopy(texture);
            try
            {
                var bytes = readable.EncodeToPNG();
                File.WriteAllBytes(Path.GetFullPath(path), bytes);
            }
            finally
            {
                if (readable != texture)
                    UnityEngine.Object.DestroyImmediate(readable);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        static Texture2D GetReadableCopy(Texture2D source)
        {
            if (source.isReadable)
                return source;

            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return copy;
        }

        static List<Rect> CollectSpriteRects(FangAutoTile fang)
        {
            var rects = new List<Rect>(CombinationCount);
            var so = new SerializedObject(fang);
            var combinations = so.FindProperty("combinations");
            if (combinations == null || combinations.arraySize != CombinationCount)
                throw new InvalidOperationException($"combinations count must be {CombinationCount}.");

            for (int i = 0; i < CombinationCount; i++)
            {
                var frames = combinations.GetArrayElementAtIndex(i).FindPropertyRelative("frames");
                if (frames == null || frames.arraySize == 0)
                    throw new InvalidOperationException($"Combination {i} has no frames.");

                var sprite = frames.GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
                if (sprite == null)
                    throw new InvalidOperationException($"Combination {i} frame sprite is null.");

                rects.Add(sprite.textureRect);
            }

            return rects;
        }

        static void ApplySpriteSheetMeta(string pngPath, List<Rect> rects)
        {
            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"TextureImporter missing for {pngPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.wrapMode = TextureWrapMode.Clamp;

            var sheet = new SpriteMetaData[rects.Count];
            for (int i = 0; i < rects.Count; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"basic01_{i:D2}",
                    rect = rects[i],
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero,
                };
            }

            importer.spritesheet = sheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        static Sprite[] LoadSpritesByIndex(string pngPath, int count)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>().ToList();
            var result = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var name = $"basic01_{i:D2}";
                result[i] = all.FirstOrDefault(s => s.name == name);
                if (result[i] == null)
                    Debug.LogError($"Sprite not found after import: {name}");
            }

            return result;
        }

        static RuleTile BuildRuleTile(FangAutoTile fang, Sprite[] sprites)
        {
            var so = new SerializedObject(fang);
            var tableProp = so.FindProperty("combinationTable");
            if (tableProp == null || tableProp.arraySize != 256)
                throw new InvalidOperationException("combinationTable must have 256 entries.");

            var table = new int[256];
            for (int i = 0; i < 256; i++)
                table[i] = tableProp.GetArrayElementAtIndex(i).intValue;

            var colliderProp = so.FindProperty("colliderType");
            var collider = colliderProp != null
                ? (Tile.ColliderType)colliderProp.enumValueIndex
                : Tile.ColliderType.Sprite;

            var ruleTile = ScriptableObject.CreateInstance<RuleTile>();
            ruleTile.m_DefaultColliderType = collider;
            ruleTile.m_TilingRules = new List<RuleTile.TilingRule>();

            int defaultComboIndex = table[255];
            if (defaultComboIndex >= 0 && defaultComboIndex < sprites.Length && sprites[defaultComboIndex] != null)
                ruleTile.m_DefaultSprite = sprites[defaultComboIndex];

            for (int comboIndex = 0; comboIndex < CombinationCount; comboIndex++)
            {
                if (!TryGetRepresentativeMask(table, comboIndex, out byte mask))
                {
                    Debug.LogError($"No representative neighbor mask for combination index {comboIndex}.");
                    continue;
                }

                var rule = new RuleTile.TilingRule
                {
                    m_Id = comboIndex,
                    m_NeighborPositions = new List<Vector3Int>(DefaultNeighborPositions),
                    m_Neighbors = new List<int>(),
                    m_Sprites = new Sprite[1],
                    m_Output = OutputSprite.Single,
                    m_ColliderType = collider,
                    m_RuleTransform = RuleTile.TilingRule.Transform.Fixed,
                };

                for (int ui = 0; ui < DefaultNeighborPositions.Length; ui++)
                {
                    int fangBit = FangBitByUnityNeighborIndex[ui];
                    bool connected = (mask & (1 << fangBit)) != 0;
                    rule.m_Neighbors.Add(connected ? Neighbor.This : Neighbor.NotThis);
                }

                if (comboIndex < sprites.Length && sprites[comboIndex] != null)
                    rule.m_Sprites[0] = sprites[comboIndex];

                ruleTile.m_TilingRules.Add(rule);
            }

            return ruleTile;
        }

        static void SaveRuleTile(RuleTile ruleTile, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(ruleTile, existing);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(ruleTile);
            }
            else
            {
                AssetDatabase.CreateAsset(ruleTile, path);
            }
        }

        static bool TryGetRepresentativeMask(int[] combinationTable, int comboIndex, out byte mask)
        {
            for (int nc = 0; nc < 256; nc++)
            {
                if (combinationTable[nc] != comboIndex)
                    continue;
                mask = (byte)nc;
                return true;
            }

            mask = 0;
            return false;
        }
    }
}
#endif
