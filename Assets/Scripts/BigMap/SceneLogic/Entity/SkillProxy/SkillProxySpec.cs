using UnityEngine;

namespace My.Map.Entity
{
    public enum ESkillProxyAnchorMode
    {
        FollowOwner,
        OrbitOwner,
        FixedWorld,
        MirrorOwnerFacing,
    }

    public class SkillProxySpec
    {
        public string Id;
        public ESkillProxyAnchorMode AnchorMode = ESkillProxyAnchorMode.FollowOwner;
        public Vector2 AnchorOffset;
        public float OrbitRadius = 1.2f;
        public float OrbitAngularSpeed = 1.5f;
        public float OrbitInitialAngle;
        public string AmmoResourceId = AttrIdConsts.SkillProxyOrbAmmo;
        public int MaxAmmo = 3;
        public string OrbRegenBuffId = "orb_skill_regen";
        public string OwnerLinkMarkBuffId = "orb_skill_owner_link";
        public string PeriodicAbilityId = "orb_skill_cast";
        public float CastInterval = 1.2f;
        public float CastAcquireRadius = 1f;
        public Vector2 CastDirOffset = Vector2.right;
        public float DefaultLifetime = 15f;
        public string PrefabName;
    }

    public static class SkillProxySpecRuntimeMap
    {
        static readonly System.Collections.Generic.Dictionary<string, SkillProxySpec> _map = new();

        static SkillProxySpecRuntimeMap()
        {
            Register(new SkillProxySpec
            {
                Id = "orb_skill_v1",
                AnchorMode = ESkillProxyAnchorMode.FollowOwner,
                AnchorOffset = new Vector2(0f, 0.55f),
                OrbitRadius = 1.2f,
                OrbitAngularSpeed = 120f,
                MaxAmmo = 3,
                CastInterval = 1.2f,
                CastAcquireRadius = 8f,
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
