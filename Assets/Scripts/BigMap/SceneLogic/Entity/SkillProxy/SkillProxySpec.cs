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
        // 纯表现偏移：逻辑 Pos 始终与 Owner 脚底 XY 同步，Presenter 再叠加此偏移
        public Vector2 FollowOffset;

        // attrId -> 初始值（数值属性直接注册；资源 current 注册，max 由同表内 *Max 键或框架映射决定）
        public Dictionary<string, long> InitialAttrs = new();

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
                FollowOffset = new Vector2(0f, 0.55f),
                InitialAttrs = new Dictionary<string, long>
                {
                    { AttrIdConsts.AmmoMax, 5 },
                    { AttrIdConsts.Ammo, 3 },
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
