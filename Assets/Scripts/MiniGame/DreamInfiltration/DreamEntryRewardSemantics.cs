using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;

namespace My.MiniGame.Dream
{
    public static class DreamEntryRewardSemantics
    {
        public const string PasserbyKindLabel = "浅梦路人";
        public const string CharacterKindLabel = "角色梦境";
        public const string AbstractGroupKindLabel = "小团体";

        public static string GradeLabel(ECommonGrade grade)
        {
            return grade switch
            {
                ECommonGrade.Common => "凡品",
                ECommonGrade.Uncommon => "良品",
                ECommonGrade.Rare => "稀品",
                ECommonGrade.Epic => "上品",
                ECommonGrade.Legendary => "极品",
                _ => "未知",
            };
        }

        public static string BuildPasserbyDetail(
            DreamPasserby cfg,
            DreamPasserbyRegion region,
            DreamPasserbyDailyEntryPersist entry)
        {
            if (cfg == null) return "无效路人";
            var regionName = region?.DisplayName ?? entry?.RegionId ?? "未知区域";
            var theme = string.IsNullOrEmpty(cfg.DreamThemeDisplayName) ? "浅梦" : cfg.DreamThemeDisplayName;
            var reward = BuildPasserbyRewardLine(cfg);
            return
                $"{PasserbyKindLabel}\n" +
                $"品级：{GradeLabel(cfg.Grade)} · 形象档 {cfg.VisualVariant}\n" +
                $"区域概念：{regionName}\n" +
                $"主题：{theme}\n" +
                $"通关：{reward}\n" +
                $"<size=70%><#aab4c0>薄奖，无秘会/剧情档</size>";
        }

        public static string BuildPasserbyRewardLine(DreamPasserby cfg)
        {
            if (cfg == null) return "薄奖";
            var parts = new List<string>(3);
            if (cfg.RewardDesireShard > 0) parts.Add($"欲望碎片×{cfg.RewardDesireShard}");
            if (cfg.RewardJingyuan > 0) parts.Add($"精元池+{cfg.RewardJingyuan}");
            if (cfg.RewardFallenBaseAmount > 0) parts.Add($"基础沉沦+{cfg.RewardFallenBaseAmount}");
            return parts.Count > 0 ? string.Join(" / ", parts) : "无实质奖励";
        }

        public static string BuildCharacterDetail(string characterName)
        {
            var name = string.IsNullOrEmpty(characterName) ? "角色" : characterName;
            return
                $"{name}的梦境\n" +
                $"{CharacterKindLabel}\n" +
                $"通关：记录倾向胜负 · 推进任务/关系\n" +
                $"<size=70%><#a8cfc0>不发经营物资</size>";
        }

        public static string BuildAbstractGroupDetail(
            AbstractGroup groupCfg,
            AbstractGroupStage stageCfg,
            int maxStage,
            bool nearSecretUnit)
        {
            var groupName = groupCfg?.DisplayName ?? "小团体";
            var stageName = stageCfg?.DisplayName ?? "";
            var stage = stageCfg?.Stage ?? 0;
            var rewardLine = BuildAbstractGroupRewardLine(groupCfg?.GroupId, stage);
            if (string.IsNullOrEmpty(rewardLine) && !string.IsNullOrEmpty(stageCfg?.RewardPreviewDesc))
                rewardLine = stageCfg.RewardPreviewDesc;

            var endHint = nearSecretUnit
                ? "\n本阶通关后可获得秘会"
                : (maxStage > 0 ? $"\n进度 {stage}/{maxStage}" : "");

            return
                $"{AbstractGroupKindLabel}·{groupName}\n" +
                $"阶段{stage} · {stageName}\n" +
                $"通关：{rewardLine}{endHint}\n" +
                $"<size=70%><#cbb8d8>经营向：基础沉沦 / 精元 / 信仰 / 秘会</size>";
        }

        public static string BuildAbstractGroupRewardLine(string groupId, int stage)
        {
            if (string.IsNullOrEmpty(groupId) || stage <= 0) return "阶段小奖";
            var table = CfgMgr.Cfgs?.TbAbstractGroupStageReward;
            if (table?.DataList == null) return "阶段小奖";

            var parts = new List<string>(4);
            foreach (var row in table.DataList)
            {
                if (row == null) continue;
                if (!string.Equals(row.GroupId, groupId, System.StringComparison.Ordinal)) continue;
                if (row.Stage != stage) continue;

                if (row.FallenBaseAmount > 0) parts.Add($"基础沉沦+{row.FallenBaseAmount}");
                if (row.Jingyuan > 0) parts.Add($"精元池+{row.Jingyuan}");
                if (row.Faith > 0) parts.Add($"信仰+{row.Faith}");
                if (!string.IsNullOrEmpty(row.ItemId) && row.ItemCount > 0)
                    parts.Add($"{row.ItemId}×{row.ItemCount}");
            }

            return parts.Count > 0 ? string.Join(" / ", parts) : "阶段小奖";
        }

        public static string ApplyCharacterOutcomeNote(DreamSettlementPayload payload)
        {
            if (payload == null) return string.Empty;
            if (!payload.Won)
                return "已记录本次尝试（任务可按尝试/胜利条件查询）";

            var tendency = payload.VictoryTendency switch
            {
                DreamTendencyKind.Force => "暴力",
                DreamTendencyKind.Soothing => "安抚",
                DreamTendencyKind.Trick => "计谋",
                _ => "通关",
            };
            return $"已记录角色梦境结果（{tendency}）· 无经营物资，供任务/关系推进";
        }
    }
}
