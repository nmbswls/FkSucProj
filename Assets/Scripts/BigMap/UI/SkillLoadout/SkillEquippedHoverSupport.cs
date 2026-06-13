using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public static class SkillEquippedHoverSupport
    {
        public static void Apply(
            Component host,
            SkillEquippedHoverProvider hover,
            string skillId,
            params Image[] raycastImages)
        {
            if (host == null)
            {
                return;
            }

            if (hover == null)
            {
                Debug.LogError("[SkillEquippedHover] Missing SkillEquippedHoverProvider reference.", host);
                return;
            }

            hover.Configure(skillId);

            if (raycastImages == null)
            {
                return;
            }

            for (var i = 0; i < raycastImages.Length; i++)
            {
                var image = raycastImages[i];
                if (image == null)
                {
                    Debug.LogError("[SkillEquippedHover] Raycast image reference is null.", host);
                    continue;
                }

                image.raycastTarget = true;
            }
        }
    }
}
