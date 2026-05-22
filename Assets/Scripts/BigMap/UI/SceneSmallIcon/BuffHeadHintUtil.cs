using My.Map;
using My.Map.Entity;

namespace My.UI
{
    public static class BuffHeadHintUtil
    {
        public static BuffInstance ResolveTopHeadHintBuff(IEntityBuffOwner owner)
        {
            if (owner?.BuffContainer == null)
            {
                return null;
            }

            BuffInstance best = null;
            var bestPriority = 0;

            foreach (var kv in owner.BuffContainer)
            {
                var buff = kv.Value;
                if (buff == null || buff.MarkedForRemove)
                {
                    continue;
                }

                var def = buff.Def;
                if (def == null || def.IsHidden || def.HeadHintPriority <= 0)
                {
                    continue;
                }

                if (def.HeadHintPriority > bestPriority)
                {
                    bestPriority = def.HeadHintPriority;
                    best = buff;
                }
            }

            return best;
        }

        public static UnityEngine.Sprite ResolveBuffIcon(BuffInstance buff)
        {
            if (buff?.Def == null || string.IsNullOrEmpty(buff.Def.Icon))
            {
                return SimpleResManager.Load<UnityEngine.Sprite>("Sprites/BuffIcons/fallback");
            }

            var icon = SimpleResManager.Load<UnityEngine.Sprite>($"Sprites/BuffIcons/{buff.Def.Icon}");
            if (icon == null)
            {
                icon = SimpleResManager.Load<UnityEngine.Sprite>("Sprites/BuffIcons/fallback");
            }

            return icon;
        }
    }
}
