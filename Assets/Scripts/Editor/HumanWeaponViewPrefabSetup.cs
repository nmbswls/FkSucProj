#if UNITY_EDITOR
using Animancer;
using My.Map.Scene;
using UnityEditor;
using UnityEngine;

// 创建人类武器通用表现 prefab（Animancer + MapUnitWeaponOne）
public static class HumanWeaponViewPrefabSetup
{
    const string PrefabPath = "Assets/Resources/Prefab/Presentations/HumanWeaponView.prefab";

    [MenuItem("Tools/HumanWeapon/Setup HumanWeaponView Prefab")]
    public static void SetupFromMenu()
    {
        if (BuildPrefab())
        {
            Debug.Log("[HumanWeaponViewPrefabSetup] Prefab created: " + PrefabPath);
        }
    }

    public static bool BuildPrefab()
    {
        EnsureDirectory();

        var root = new GameObject("HumanWeaponView");
        try
        {
            root.layer = LayerMask.NameToLayer("PlayerWeapon");
            if (root.layer < 0)
            {
                root.layer = 13;
            }

            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.2f);
            col.offset = new Vector2(0.35f, 0f);

            var animator = root.AddComponent<Animator>();
            var animancer = root.AddComponent<AnimancerComponent>();
            animancer.Animator = animator;

            var rotatorGo = new GameObject("Rotator");
            rotatorGo.transform.SetParent(root.transform, false);

            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(rotatorGo.transform, false);
            spriteGo.transform.localPosition = new Vector3(0.35f, 0f, 0f);
            var sr = spriteGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;

            var weaponOne = root.AddComponent<MapUnitWeaponOne>();
            weaponOne.weaponAnimancer = animancer;
            weaponOne.weaponAnim = animator;
            weaponOne.weaponParts = new[]
            {
                new WeaponPart
                {
                    rotator = rotatorGo.transform,
                    spriteVisual = sr,
                },
            };
            weaponOne.posLerpSpeed = 15f;
            weaponOne.radiusX = 0.2f;
            weaponOne.radiusY = 0.4f;
            weaponOne.maxAngle = 60f;
            weaponOne.minAngle = -60f;
            weaponOne.centerOffset = new Vector2(-0.15f, 0f);

            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            return true;
        }
        finally
        {
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    static void EnsureDirectory()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefab/Presentations"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Prefab"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Prefab");
            }

            AssetDatabase.CreateFolder("Assets/Resources/Prefab", "Presentations");
        }
    }
}
#endif
