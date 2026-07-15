#if UNITY_EDITOR
using System.Collections.Generic;
using Animancer;
using UnityEditor;
using UnityEngine;

public static class VeraLocomotionAnimationAssembler
{
    const string PrefabPath = "Assets/Resources/Prefab/Presentations/Npc/vera_civil.prefab";
    const string AnimFolder = "Assets/Resources/Anim/npc/vera";

    [MenuItem("Tools/Maps/Home 01/Install Vera Locomotion Animation")]
    public static void Install()
    {
        EnsureFolder("Assets/Resources/Anim");
        EnsureFolder("Assets/Resources/Anim/npc");
        EnsureFolder(AnimFolder);

        if (!AssetDatabase.CopyAsset("Assets/Resources/Prefab/Presentations/Npc/civil.prefab", PrefabPath))
            Debug.LogWarning("[VeraLocomotion] vera_civil prefab already exists; updating it.");

        var workIdle = CreateLoopClip("vera_work_idle", 0.8f, 0.035f);
        var workMove = CreateLoopClip("vera_work_move", 0.32f, 0.08f);
        var sleepIdle = CreateLoopClip("vera_sleep_idle", 1.5f, -0.045f);
        var waitIdle = CreateLoopClip("vera_wait_idle", 2.0f, 0.018f);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = root.GetComponent<Animator>() ?? root.AddComponent<Animator>();
            var animancer = root.GetComponent<AnimancerComponent>() ?? root.AddComponent<AnimancerComponent>();
            var holder = root.GetComponent<UnitAnimHolder>() ?? root.AddComponent<UnitAnimHolder>();
            holder.AnimClips = new List<UnitAnimHolder.OneWrapper>
            {
                new() { Name = "idle", Clip = workIdle, Speed = 1f },
                new() { Name = "move", Clip = workMove, Speed = 1f },
                new() { Name = "vera_work_idle", Clip = workIdle, Speed = 1f },
                new() { Name = "vera_work_move", Clip = workMove, Speed = 1f },
                new() { Name = "vera_sleep_idle", Clip = sleepIdle, Speed = 1f },
                new() { Name = "vera_wait_idle", Clip = waitIdle, Speed = 1f },
            };

            var presenter = root.GetComponent<My.Map.Scene.SceneNpcPresenter>();
            if (presenter != null)
            {
                presenter.MainAgentAnimator = animancer;
                presenter.AnimHolder = holder;
                EditorUtility.SetDirty(presenter);
            }
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[VeraLocomotion] Installed vera_civil prefab and locomotion clips.");
    }

    static AnimationClip CreateLoopClip(string name, float duration, float offset)
    {
        var path = $"{AnimFolder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name, frameRate = 30f };
            AssetDatabase.CreateAsset(clip, path);
        }

        var binding = EditorCurveBinding.FloatCurve("view", typeof(Transform), "m_LocalPosition.y");
        var curve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(duration * 0.5f, offset),
            new Keyframe(duration, 0f));
        clip.SetCurve(binding.path, binding.type, binding.propertyName, curve);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = path.Substring(0, path.LastIndexOf('/'));
        var name = path.Substring(path.LastIndexOf('/') + 1);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
