using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity
{
    public enum ESkillProxyAnchorMode
    {
        FollowOwner,
        FixedWorld,
        MirrorOwnerFacing,
    }

    public class SkillProxySpec
    {
        public string Id;
        public ESkillProxyAnchorMode AnchorMode = ESkillProxyAnchorMode.FollowOwner;
        public Vector2 AnchorOffset;

        // resourceId -> 初始 max（current 初始等于 max）
        public Dictionary<string, int> InitialResources = new();

        // 生成时挂在 Proxy 自身上的 Buff
        public string[] SelfBuffIds;

        // 可选：挂在 Owner 上、用于生命周期绑定
        public string OwnerLinkBuffId;

        public string PeriodicAbilityId;
        public float CastInterval = 1.2f;
        public float CastAcquireRadius = 2f;
        public Vector2 CastDirOffset = Vector2.right;
        public float DefaultLifetime = 15f;
        public string PrefabName;
    }

    public static class SkillProxySpecRuntimeMap
    {
        static readonly Dictionary<string, SkillProxySpec> _map = new();

        static SkillProxySpecRuntimeMap()
        {
            Register(new SkillProxySpec
            {
                Id = "orb_skill_v1",
                AnchorMode = ESkillProxyAnchorMode.FollowOwner,
                AnchorOffset = new Vector2(0f, 0.55f),
                InitialResources = new Dictionary<string, int>
                {
                    { "ammo", 3 },
                },
                SelfBuffIds = new[] { "orb_skill_regen" },
                OwnerLinkBuffId = "orb_skill_owner_link",
                PeriodicAbilityId = "orb_skill_cast",
                CastInterval = 1.2f,
                CastAcquireRadius = 2f,
                DefaultLifetime = 15f,
                PrefabName = "orb_skill_v1",
            });
        }

        public static void Register(SkillProxySpec spec)
        {
            if (spec == null || string.IsNullOrEmpty(spec.Id))
            {
                return;
            }

            _map[spec.Id] = spec;
        }

        public static SkillProxySpec Get(string cfgId)
        {
            if (string.IsNullOrEmpty(cfgId))
            {
                return null;
            }

            return _map.TryGetValue(cfgId, out var spec) ? spec : null;
        }
    }
}
