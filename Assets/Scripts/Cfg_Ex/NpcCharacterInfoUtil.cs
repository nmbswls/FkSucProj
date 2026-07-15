using My.Config;

namespace My
{
    // CharacterKey 与 TbCharacterInfo 的显示名桥接。
    // 接取/提醒/目标推进入口以 Accept/Remind/ObjectiveDialog.character_key 为准；StartNpcId 无运行时消费方。
    public static class NpcCharacterInfoUtil
    {
        public static string GetDisplayName(string characterKey, string fallback = null)
        {
            if (!string.IsNullOrEmpty(characterKey))
            {
                var info = CfgMgr.Cfgs?.TbCharacterInfo?.GetOrDefault(characterKey);
                if (info != null && !string.IsNullOrEmpty(info.Name))
                {
                    return info.Name;
                }
            }

            return string.IsNullOrEmpty(fallback) ? characterKey ?? string.Empty : fallback;
        }
    }
}
