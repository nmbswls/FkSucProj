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
    // 从 Fang Auto Tile 烘焙 Rule Tile：选中 Texture2D（图集）或 FangAutoTile 资产即可，输出路径随套装名推导
    public static class FangRuleTileBaker
    {
        const string TileOutputRoot = "Assets/Arts/Tile";

        static readonly int[] FangBitByUnityNeighborIndex = { 6, 5, 4, 7, 3, 0, 1, 2 };

        // Unity RuleTile 默认 8 邻居顺序：0 TL, 1 T, 2 TR, 3 L, 4 R, 5 BL, 6 B, 7 BR
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

        // 对角格两侧正交格为 NotThis(X) 时，对角必须为 Don't Care（Inspector 留空）
        const int NeighborDontCare = 0;

        sealed class BakeContext
        {
            public string SetId;
            public string OutputFolder;
            public string PngPath;
            public string RuleTilePath;
            public string SpriteNamePrefix;
            public int PixelsPerUnit;
            public FangAutoTile Fang;
            public Texture2D ExportAtlas;
        }

        [MenuItem("Assets/Tools/Tile/Bake Rule Tile From Fang", priority = 200)]
        static void BakeMenu()
        {
            if (!TryResolveBakeContext(out var ctx))
                return;
            Bake(ctx);
        }

        [MenuItem("Assets/Tools/Tile/Bake Rule Tile From Fang", true)]
        static bool BakeMenuValidate()
        {
            return Selection.activeObject is Texture2D or FangAutoTile;
        }

        static bool TryResolveBakeContext(out BakeContext ctx)
        {
            ctx = null;
            var selected = Selection.activeObject;

            FangAutoTile fang = selected as FangAutoTile;
            Texture2D atlas = selected as Texture2D;

            if (fang == null && atlas != null)
            {
                if (!TryFindFangForTexture(atlas, out fang, out _))
                    return false;
            }
            else if (fang != null && atlas == null)
            {
                atlas = GetFangAtlasTexture(fang);
                if (atlas == null)
                {
                    Debug.LogError(
                        "Fang tile has no mainChannel / compiled texture. Select the atlas Texture2D in Project, or assign a channel on the Fang asset.");
                    return false;
                }
            }
            else if (fang == null)
            {
                Debug.LogError("Select a Texture2D or FangAutoTile asset in the Project window.");
                return false;
            }

            var fangPath = AssetDatabase.GetAssetPath(fang);
            var texturePath = AssetDatabase.GetAssetPath(atlas);
            var setId = ResolveSetId(fangPath, texturePath);
            if (string.IsNullOrEmpty(setId))
            {
                Debug.LogError($"Invalid Fang asset path: {fangPath}");
                return false;
            }

            ResolveOutputPaths(fangPath, texturePath, setId,
                out var outputFolder, out var pngPath, out var ruleTilePath);

            ctx = new BakeContext
            {
                SetId = setId,
                OutputFolder = outputFolder,
                PngPath = pngPath,
                RuleTilePath = ruleTilePath,
                SpriteNamePrefix = setId + "_",
                PixelsPerUnit = ReadPixelsPerUnit(fang),
                Fang = fang,
                ExportAtlas = atlas,
            };
            return true;
        }

        // 套装 ID：优先用 Assets/Arts/Tile/{套装名}/ 目录名，避免 grass_fang.asset 输出到 grass_fang 子目录
        static string ResolveSetId(string fangPath, string texturePath)
        {
            var fromTexDir = GetTileSetFolderName(texturePath);
            if (!string.IsNullOrEmpty(fromTexDir))
                return fromTexDir;

            var fromFangDir = GetTileSetFolderName(fangPath);
            if (!string.IsNullOrEmpty(fromFangDir))
                return fromFangDir;

            return Path.GetFileNameWithoutExtension(fangPath);
        }

        static string GetTileSetFolderName(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return null;

            var dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || !dir.StartsWith(TileOutputRoot + "/", StringComparison.Ordinal))
                return null;

            return Path.GetFileName(dir);
        }

        static void ResolveOutputPaths(string fangPath, string texturePath, string setId,
            out string outputFolder, out string pngPath, out string ruleTilePath)
        {
            var folder = GetTileSetFolderName(texturePath) ?? GetTileSetFolderName(fangPath);
            outputFolder = !string.IsNullOrEmpty(folder)
                ? $"{TileOutputRoot}/{folder}"
                : $"{TileOutputRoot}/{setId}";

            pngPath = $"{outputFolder}/{setId}Tiles.png";
            ruleTilePath = $"{outputFolder}/{setId}TerrainRuleTile.asset";
        }

        static void Bake(BakeContext ctx)
        {
            if (!EditorUtility.DisplayDialog(
                    "Bake Rule Tile From Fang",
                    $"套装: {ctx.SetId}\n" +
                    $"图集: {AssetDatabase.GetAssetPath(ctx.ExportAtlas)}\n" +
                    $"Fang: {AssetDatabase.GetAssetPath(ctx.Fang)}\n" +
                    $"输出:\n  {ctx.PngPath}\n  {ctx.RuleTilePath}\n\n" +
                    "将覆盖 PNG 切图与 Rule Tile 的全部规则/Sprite。若已手调规则请先备份。",
                    "继续烘焙",
                    "取消"))
                return;

            EnsureFolder(ctx.OutputFolder);

            var spriteRects = CollectSpriteRects(ctx.Fang, out int combinationCount);
            if (spriteRects.Count == 0)
            {
                Debug.LogError($"[{ctx.SetId}] Fang tile has no combinations. Run Fang Generate! first.");
                return;
            }

            ExportPng(ctx.ExportAtlas, ctx.PngPath);
            ApplySpriteSheetMeta(ctx, spriteRects);
            var sprites = LoadSpritesByIndex(ctx, combinationCount);
            var ruleTile = BuildRuleTile(ctx.Fang, sprites, combinationCount);
            SaveRuleTile(ruleTile, ctx.RuleTilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[{ctx.SetId}] Baked Rule Tile: {ctx.RuleTilePath}, png: {ctx.PngPath}");
        }

        static int ReadPixelsPerUnit(FangAutoTile fang)
        {
            var so = new SerializedObject(fang);
            var oneTilePerUnit = so.FindProperty("oneTilePerUnit")?.boolValue ?? true;
            var explicitPpu = so.FindProperty("pixelsPerUnit")?.intValue ?? 16;

            var combinations = so.FindProperty("combinations");
            if (combinations != null && combinations.arraySize > 0)
            {
                var frames = combinations.GetArrayElementAtIndex(0).FindPropertyRelative("frames");
                if (frames != null && frames.arraySize > 0)
                {
                    var sprite = frames.GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
                    if (sprite != null && sprite.pixelsPerUnit > 0)
                        return Mathf.RoundToInt(sprite.pixelsPerUnit);
                }
            }

            if (oneTilePerUnit && combinations != null && combinations.arraySize > 0)
            {
                var main = so.FindProperty("mainChannel")?.objectReferenceValue as Texture2D;
                if (main != null && main.height > 0)
                {
                    int numTilesInFrame = 5;
                    int numSlopes = so.FindProperty("numSlopes")?.intValue ?? 0;
                    numTilesInFrame += 4 * numSlopes * (numSlopes + 1);
                    if (numSlopes >= 1) numTilesInFrame -= 4;
                    int tileSize = main.height / Mathf.Max(1, numTilesInFrame);
                    if (tileSize > 0)
                        return tileSize;
                }
            }

            return Mathf.Max(1, explicitPpu);
        }

        static Texture2D GetFangAtlasTexture(FangAutoTile fang)
        {
            var so = new SerializedObject(fang);
            var compiled = so.FindProperty("compiledChannels");
            if (compiled != null && compiled.arraySize > 0)
            {
                var tex = compiled.GetArrayElementAtIndex(0).objectReferenceValue as Texture2D;
                if (tex != null)
                    return tex;
            }

            return so.FindProperty("mainChannel")?.objectReferenceValue as Texture2D;
        }

        static bool TryFindFangForTexture(Texture2D atlas, out FangAutoTile fang, out string fangPath)
        {
            fang = null;
            fangPath = null;
            var texturePath = AssetDatabase.GetAssetPath(atlas);
            if (string.IsNullOrEmpty(texturePath))
            {
                Debug.LogError("Selected Texture2D has no asset path.");
                return false;
            }

            var textureGuid = AssetDatabase.AssetPathToGUID(texturePath);
            var refMatches = new List<string>();

            foreach (var guid in AssetDatabase.FindAssets("t:FangAutoTile"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<FangAutoTile>(path);
                if (candidate == null)
                    continue;

                if (FangReferencesTexture(candidate, textureGuid, texturePath))
                    refMatches.Add(path);
            }

            if (refMatches.Count > 0)
            {
                if (refMatches.Count > 1)
                    Debug.LogWarning($"Multiple Fang tiles reference this texture; using: {refMatches[0]}");
                fangPath = refMatches[0];
                fang = AssetDatabase.LoadAssetAtPath<FangAutoTile>(fangPath);
                return fang != null;
            }

            // 编译图集常为独立 .texture2D，与 mainChannel(源图) 不是同一文件；允许同目录下的 Fang 资产
            if (TryFindFangInFolder(texturePath, out fang, out fangPath))
            {
                Debug.Log(
                    $"Using Fang asset in same folder as atlas (atlas is not on Main Channel): {fangPath}");
                return true;
            }

            Debug.LogError(
                $"No FangAutoTile for texture \"{texturePath}\".\n" +
                "- Put the Fang .asset in the same folder as the atlas (e.g. grass_01/grass_fang.asset), or\n" +
                "- Assign this texture on the Fang tile Main Channel / compiled channel, or\n" +
                "- Select the FangAutoTile .asset directly.");
            return false;
        }

        static bool TryFindFangInFolder(string texturePath, out FangAutoTile fang, out string fangPath)
        {
            fang = null;
            fangPath = null;
            var dir = Path.GetDirectoryName(texturePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir))
                return false;

            var guids = AssetDatabase.FindAssets("t:FangAutoTile", new[] { dir });
            if (guids == null || guids.Length == 0)
                return false;

            if (guids.Length > 1)
                Debug.LogWarning($"Multiple FangAutoTile in {dir}; using: {AssetDatabase.GUIDToAssetPath(guids[0])}");

            fangPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            fang = AssetDatabase.LoadAssetAtPath<FangAutoTile>(fangPath);
            return fang != null;
        }

        static bool FangReferencesTexture(FangAutoTile fang, string textureGuid, string texturePath)
        {
            var so = new SerializedObject(fang);
            var main = so.FindProperty("mainChannel")?.objectReferenceValue;
            if (ReferenceEqualsTexture(main, textureGuid, texturePath))
                return true;

            var compiled = so.FindProperty("compiledChannels");
            if (compiled == null)
                return false;

            for (int i = 0; i < compiled.arraySize; i++)
            {
                var tex = compiled.GetArrayElementAtIndex(i).objectReferenceValue;
                if (ReferenceEqualsTexture(tex, textureGuid, texturePath))
                    return true;
            }

            return false;
        }

        static bool ReferenceEqualsTexture(UnityEngine.Object obj, string textureGuid, string texturePath)
        {
            if (obj == null)
                return false;

            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
                return false;

            if (path == texturePath)
                return true;

            return AssetDatabase.AssetPathToGUID(path) == textureGuid;
        }

        static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parts = folderPath.Split('/');
            if (parts.Length < 2 || parts[0] != "Assets")
                throw new InvalidOperationException($"Invalid folder path: {folderPath}");

            var current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        static void ExportPng(Texture2D texture, string path)
        {
            var readable = GetReadableCopy(texture);
            try
            {
                File.WriteAllBytes(Path.GetFullPath(path), readable.EncodeToPNG());
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

        static List<Rect> CollectSpriteRects(FangAutoTile fang, out int combinationCount)
        {
            var rects = new List<Rect>();
            var so = new SerializedObject(fang);
            var combinations = so.FindProperty("combinations");
            if (combinations == null || combinations.arraySize == 0)
            {
                combinationCount = 0;
                return rects;
            }

            combinationCount = combinations.arraySize;
            for (int i = 0; i < combinationCount; i++)
            {
                var frames = combinations.GetArrayElementAtIndex(i).FindPropertyRelative("frames");
                if (frames == null || frames.arraySize == 0)
                    throw new InvalidOperationException($"[{fang.name}] Combination {i} has no frames.");

                var sprite = frames.GetArrayElementAtIndex(0).objectReferenceValue as Sprite;
                if (sprite == null)
                    throw new InvalidOperationException($"[{fang.name}] Combination {i} frame sprite is null.");

                rects.Add(sprite.textureRect);
            }

            return rects;
        }

        static void ApplySpriteSheetMeta(BakeContext ctx, List<Rect> rects)
        {
            var importer = AssetImporter.GetAtPath(ctx.PngPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"TextureImporter missing for {ctx.PngPath}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = ctx.PixelsPerUnit;
            importer.wrapMode = TextureWrapMode.Clamp;

            var sheet = new SpriteMetaData[rects.Count];
            for (int i = 0; i < rects.Count; i++)
            {
                sheet[i] = new SpriteMetaData
                {
                    name = $"{ctx.SpriteNamePrefix}{i:D2}",
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

        static Sprite[] LoadSpritesByIndex(BakeContext ctx, int count)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(ctx.PngPath).OfType<Sprite>().ToList();
            var result = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                var name = $"{ctx.SpriteNamePrefix}{i:D2}";
                result[i] = all.FirstOrDefault(s => s.name == name);
                if (result[i] == null)
                    Debug.LogError($"Sprite not found after import: {name}");
            }

            return result;
        }

        static RuleTile BuildRuleTile(FangAutoTile fang, Sprite[] sprites, int combinationCount)
        {
            var so = new SerializedObject(fang);
            var tableProp = so.FindProperty("combinationTable");
            if (tableProp == null || tableProp.arraySize != 256)
                throw new InvalidOperationException($"[{fang.name}] combinationTable must have 256 entries.");

            var table = new int[256];
            for (int i = 0; i < 256; i++)
                table[i] = tableProp.GetArrayElementAtIndex(i).intValue;

            var colliderProp = so.FindProperty("colliderType");
            var collider = colliderProp != null
                ? (Tile.ColliderType)colliderProp.enumValueIndex
                : Tile.ColliderType.Sprite;

            var ruleTile = ScriptableObject.CreateInstance<RuleTile>();
            ruleTile.name = fang.name + "TerrainRuleTile";
            ruleTile.m_DefaultColliderType = collider;
            ruleTile.m_TilingRules = new List<RuleTile.TilingRule>();

            int defaultComboIndex = table[255];
            if (defaultComboIndex >= 0 && defaultComboIndex < sprites.Length && sprites[defaultComboIndex] != null)
                ruleTile.m_DefaultSprite = sprites[defaultComboIndex];

            for (int comboIndex = 0; comboIndex < combinationCount; comboIndex++)
            {
                if (!TryGetRepresentativeMask(table, comboIndex, out byte mask))
                {
                    Debug.LogError($"[{fang.name}] No representative neighbor mask for combination {comboIndex}.");
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

                ApplyDiagonalDontCareRules(rule.m_Neighbors);

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
                existing.name = Path.GetFileNameWithoutExtension(path);
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(ruleTile);
            }
            else
            {
                AssetDatabase.CreateAsset(ruleTile, path);
            }
        }

        // 正交邻格任一为 NotThis 时，夹角对角格不参与匹配（Rule Tile 留空）
        static void ApplyDiagonalDontCareRules(List<int> neighbors)
        {
            if (neighbors == null || neighbors.Count != DefaultNeighborPositions.Length)
                return;

            ClearDiagonalIfOrthogonalNotThis(neighbors, diagonalUi: 0, orthoUiA: 1, orthoUiB: 3);
            ClearDiagonalIfOrthogonalNotThis(neighbors, diagonalUi: 2, orthoUiA: 1, orthoUiB: 4);
            ClearDiagonalIfOrthogonalNotThis(neighbors, diagonalUi: 5, orthoUiA: 6, orthoUiB: 3);
            ClearDiagonalIfOrthogonalNotThis(neighbors, diagonalUi: 7, orthoUiA: 6, orthoUiB: 4);
        }

        static void ClearDiagonalIfOrthogonalNotThis(List<int> neighbors, int diagonalUi, int orthoUiA, int orthoUiB)
        {
            if (neighbors[orthoUiA] == Neighbor.NotThis || neighbors[orthoUiB] == Neighbor.NotThis)
                neighbors[diagonalUi] = NeighborDontCare;
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
