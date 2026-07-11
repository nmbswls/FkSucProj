using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ForestMonsterResourceOrganizer
{
    const string SourceDir = "Assets/Arts/Generated/MonsterSprites/Forest";
    const string NpcRoot = "Assets/Resources/Prefab/Presentations/Npc";

    struct MonsterDef
    {
        public string Id;
        public string LeftSprite;
        public string RightSprite;
        public float SquashX;
        public float SquashY;
        public float Rotation;
    }

    static readonly MonsterDef[] Monsters =
    {
        new()
        {
            Id = "forest_honey_slime",
            LeftSprite = "forest_honey_slime_left.png",
            RightSprite = "forest_honey_slime_right.png",
            SquashX = 1.10f,
            SquashY = 0.92f,
            Rotation = 2f,
        },
        new()
        {
            Id = "forest_leaf_slime",
            LeftSprite = "forest_leaf_slime_left.png",
            RightSprite = "forest_leaf_slime_right.png",
            SquashX = 1.08f,
            SquashY = 0.94f,
            Rotation = 2.5f,
        },
        new()
        {
            Id = "forest_vine_sprout",
            LeftSprite = "forest_vine_sprout_left.png",
            RightSprite = "forest_vine_sprout_right.png",
            SquashX = 1.04f,
            SquashY = 1.08f,
            Rotation = 4f,
        },
        new()
        {
            Id = "forest_moth_swarm",
            LeftSprite = "forest_moth_swarm_left.png",
            RightSprite = "forest_moth_swarm_right.png",
            SquashX = 1.03f,
            SquashY = 1.03f,
            Rotation = 6f,
        },
    };

    [MenuItem("Window/Content/Organize Forest Monster Resources")]
    public static void RunFromMenu()
    {
        Run();
    }

    public static void Run()
    {
        foreach (var monster in Monsters)
        {
            OrganizeMonster(monster);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ForestMonsterResourceOrganizer] Done.");
    }

    static void OrganizeMonster(MonsterDef monster)
    {
        var monsterDir = EnsureFolder(NpcRoot, monster.Id);
        var spritesDir = EnsureFolder(monsterDir, "Sprites");
        var animDir = EnsureFolder(monsterDir, "Anim");

        var leftPath = MoveSpriteIfNeeded(monster.LeftSprite, spritesDir);
        var rightPath = MoveSpriteIfNeeded(monster.RightSprite, spritesDir);

        AssetDatabase.ImportAsset(leftPath);
        AssetDatabase.ImportAsset(rightPath);

        var leftSprite = AssetDatabase.LoadAssetAtPath<Sprite>(leftPath);
        var rightSprite = AssetDatabase.LoadAssetAtPath<Sprite>(rightPath);
        if (leftSprite == null || rightSprite == null)
        {
            throw new InvalidOperationException($"Missing sprites for {monster.Id}: {leftPath}, {rightPath}");
        }

        CreateIdleClip($"{animDir}/{monster.Id}_idle_left.anim", leftSprite, monster.SquashX, monster.SquashY, -monster.Rotation);
        CreateIdleClip($"{animDir}/{monster.Id}_idle_right.anim", rightSprite, monster.SquashX, monster.SquashY, monster.Rotation);
        CreateTurnPreviewClip($"{animDir}/{monster.Id}_turn_preview.anim", leftSprite, rightSprite, monster.Rotation);
    }

    static string MoveSpriteIfNeeded(string spriteFileName, string targetDir)
    {
        var targetPath = $"{targetDir}/{spriteFileName}";
        if (File.Exists(targetPath))
        {
            return targetPath;
        }

        var sourcePath = $"{SourceDir}/{spriteFileName}";
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source sprite not found: {sourcePath}");
        }

        var moveError = AssetDatabase.MoveAsset(sourcePath, targetPath);
        if (!string.IsNullOrEmpty(moveError))
        {
            throw new InvalidOperationException($"MoveAsset failed: {sourcePath} -> {targetPath}: {moveError}");
        }

        return targetPath;
    }

    static void CreateIdleClip(string path, Sprite sprite, float squashX, float squashY, float rotation)
    {
        var clip = LoadOrCreateClip(path);
        clip.ClearCurves();
        clip.frameRate = 12f;
        clip.wrapMode = WrapMode.Loop;

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
            new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = sprite },
                new ObjectReferenceKeyframe { time = 0.6f, value = sprite },
            });

        SetCurve(clip, "m_LocalScale.x", 1f, squashX, 1f);
        SetCurve(clip, "m_LocalScale.y", 1f, squashY, 1f);
        SetCurve(clip, "localEulerAnglesRaw.z", -rotation, rotation, -rotation);

        EditorUtility.SetDirty(clip);
    }

    static void CreateTurnPreviewClip(string path, Sprite leftSprite, Sprite rightSprite, float rotation)
    {
        var clip = LoadOrCreateClip(path);
        clip.ClearCurves();
        clip.frameRate = 12f;
        clip.wrapMode = WrapMode.Loop;

        AnimationUtility.SetObjectReferenceCurve(
            clip,
            EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"),
            new[]
            {
                new ObjectReferenceKeyframe { time = 0f, value = leftSprite },
                new ObjectReferenceKeyframe { time = 0.35f, value = rightSprite },
                new ObjectReferenceKeyframe { time = 0.7f, value = leftSprite },
            });

        SetCurve(clip, "localEulerAnglesRaw.z", -rotation, rotation, -rotation);
        EditorUtility.SetDirty(clip);
    }

    static AnimationClip LoadOrCreateClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null)
        {
            return clip;
        }

        clip = new AnimationClip();
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void SetCurve(AnimationClip clip, string property, float start, float middle, float end)
    {
        var curve = new AnimationCurve(
            new Keyframe(0f, start),
            new Keyframe(0.3f, middle),
            new Keyframe(0.6f, end));
        clip.SetCurve(string.Empty, typeof(Transform), property, curve);
    }

    static string EnsureFolder(string parent, string child)
    {
        var fullPath = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parent, child);
        }

        return fullPath;
    }
}
