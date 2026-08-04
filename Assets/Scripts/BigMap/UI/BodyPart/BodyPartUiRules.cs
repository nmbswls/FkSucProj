using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;

namespace My.UI.BodyPart
{
    // 部位界面：可见/可选规则（打开界面时应至少有一个可选部位）
    public static class BodyPartUiRules
    {
        public static readonly EBodyPart[] VisibleParts =
        {
            EBodyPart.Mouth,
            EBodyPart.Breast,
            EBodyPart.FrontHole,
            EBodyPart.BackHole,
            EBodyPart.Limb,
        };

        public static bool IsVisiblePart(EBodyPart part)
        {
            for (int i = 0; i < VisibleParts.Length; i++)
            {
                if (VisibleParts[i] == part)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsSelectablePart(EBodyPart part, GameLogicManager glm)
        {
            return IsVisiblePart(part) && BodyPartCatalog.IsPartUnlocked(part, glm);
        }

        public static bool TryGetFirstSelectablePart(GameLogicManager glm, out EBodyPart part)
        {
            for (int i = 0; i < VisibleParts.Length; i++)
            {
                if (IsSelectablePart(VisibleParts[i], glm))
                {
                    part = VisibleParts[i];
                    return true;
                }
            }

            part = EBodyPart.None;
            return false;
        }

        public static int CollectSelectableParts(GameLogicManager glm, List<EBodyPart> buffer)
        {
            buffer?.Clear();
            if (buffer == null)
            {
                return 0;
            }

            for (int i = 0; i < VisibleParts.Length; i++)
            {
                if (IsSelectablePart(VisibleParts[i], glm))
                {
                    buffer.Add(VisibleParts[i]);
                }
            }

            return buffer.Count;
        }

        public static bool HasAnySelectablePart(GameLogicManager glm)
        {
            return TryGetFirstSelectablePart(glm, out _);
        }
    }
}
