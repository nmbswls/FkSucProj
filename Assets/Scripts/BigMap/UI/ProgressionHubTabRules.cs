using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;
using My.Player;
using My.UI.BodyPart;

namespace My.UI
{
    public static class ProgressionHubTabRules
    {
        static readonly List<ProgressionHubTabDef> SortedDefsBuffer = new List<ProgressionHubTabDef>();

        public static IReadOnlyList<ProgressionHubTabDef> GetSortedTabDefs()
        {
            SortedDefsBuffer.Clear();
            var table = CfgMgr.Cfgs?.TbProgressionHubTab;
            if (table?.DataList == null || table.DataList.Count == 0)
            {
                return SortedDefsBuffer;
            }

            SortedDefsBuffer.AddRange(table.DataList);
            SortedDefsBuffer.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return SortedDefsBuffer;
        }

        public static bool IsTabOpen(ProgressionHubTab tab, GameLogicManager logicManager)
        {
            var cfgTab = ToCfgTab(tab);
            var def = CfgMgr.Cfgs?.TbProgressionHubTab?.GetOrDefault(cfgTab);
            if (def == null)
            {
                return true;
            }

            if (!IsFuncOpen(def.FuncOpenType, logicManager))
            {
                return false;
            }

            if (tab == ProgressionHubTab.BodyPart)
            {
                return BodyPartUiRules.HasAnySelectablePart(logicManager);
            }

            return true;
        }

        public static ProgressionHubTab ResolveInitialTab(ProgressionHubTab requested, GameLogicManager logicManager)
        {
            if (IsTabOpen(requested, logicManager))
            {
                return requested;
            }

            foreach (var def in GetSortedTabDefs())
            {
                var tab = FromCfgTab(def.TabId);
                if (IsTabOpen(tab, logicManager))
                {
                    return tab;
                }
            }

            return ProgressionHubTab.Skills;
        }

        public static ProgressionHubTab FromCfgTab(EProgressionHubTab tabId)
        {
            return (ProgressionHubTab)(int)tabId;
        }

        public static EProgressionHubTab ToCfgTab(ProgressionHubTab tab)
        {
            return (EProgressionHubTab)(int)tab;
        }

        static bool IsFuncOpen(EFuncOpenType type, GameLogicManager logicManager)
        {
            if (type == EFuncOpenType.Invalid)
            {
                return true;
            }

            var funcOpen = logicManager?.playerDataManager?.FuncOpenSystem;
            return funcOpen != null && funcOpen.IsFuncOpen(type);
        }
    }
}
