using System.Collections.Generic;
using My.Dialog;
using My.Map;
using My.Map.Logic;
using My.Player;

namespace My.UI
{
    public enum NpcInteractHubOptionKind
    {
        TalkPeace = 0,
        Quest = 1,
        Shop = 2,
        Seduce = 3,
    }

    public sealed class NpcInteractHubOption
    {
        public string DisplayText;
        public NpcInteractHubOptionKind Kind;
        public int SortOrder;
        public string TargetDialogId;
        public DialogueSessionContext QuestSession;
    }

    // 汇总 NPC 当前可交互项（和平对话 / 任务 / 商店 / 诱惑），供门闩与 Hub UI 共用
    public static class NpcInteractHubCatalog
    {
        const int SortTalk = 0;
        const int SortQuestBase = 10;
        const int SortShop = 100;
        const int SortSeduce = 110;

        public static bool HasAny(NpcUnitLogicEntity npc, GameLogicManager glm)
        {
            return Count(npc, glm) > 0;
        }

        public static int Count(NpcUnitLogicEntity npc, GameLogicManager glm)
        {
            var list = Build(npc, glm);
            return list.Count;
        }

        public static List<NpcInteractHubOption> Build(NpcUnitLogicEntity npc, GameLogicManager glm)
        {
            var result = new List<NpcInteractHubOption>();
            if (npc == null || glm == null)
            {
                return result;
            }

            var peaceDialogId = npc.GetCurrentDialogId();
            if (!string.IsNullOrEmpty(peaceDialogId))
            {
                result.Add(new NpcInteractHubOption
                {
                    DisplayText = "交谈",
                    Kind = NpcInteractHubOptionKind.TalkPeace,
                    SortOrder = SortTalk,
                    TargetDialogId = peaceDialogId,
                });
            }

            var characterKey = npc.NpcRecord?.CharacterKey;
            var questSystem = glm.playerDataManager?.QuestSystem;
            if (!string.IsNullOrEmpty(characterKey) && questSystem != null)
            {
                foreach (var hubOpt in QuestHubOptionBuilder.Build(characterKey, questSystem, glm))
                {
                    result.Add(new NpcInteractHubOption
                    {
                        DisplayText = hubOpt.OptionText,
                        Kind = NpcInteractHubOptionKind.Quest,
                        SortOrder = SortQuestBase + hubOpt.SortOrder,
                        TargetDialogId = hubOpt.EntryDialogId,
                        QuestSession = hubOpt.Session,
                    });
                }
            }

            var shop = glm.shopDataManager?.GetShopProviderByNpcId(npc.CfgId);
            if (shop != null)
            {
                result.Add(new NpcInteractHubOption
                {
                    DisplayText = "交易",
                    Kind = NpcInteractHubOptionKind.Shop,
                    SortOrder = SortShop,
                });
            }

            result.Add(new NpcInteractHubOption
            {
                DisplayText = "诱惑",
                Kind = NpcInteractHubOptionKind.Seduce,
                SortOrder = SortSeduce,
                TargetDialogId = "player_gouyin",
            });

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }
    }
}
