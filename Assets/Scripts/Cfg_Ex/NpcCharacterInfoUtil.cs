using My.Config;

namespace My
{
    // CharacterKey 与 TbCharacterInfo 的显示名桥接；地图导出填 CharacterKey，任务 start_npc_id 用于地图显示，递交/接取对话见 quest_dialog_info。
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
