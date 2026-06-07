using cfg.demo;
using My.Config;
using System.Collections.Generic;

namespace My.Player
{
    // 供 UI 绑定的符文升级树快照（由 PlayerRuneSystem 构建）
    public sealed class RuneUpgradeTreeView
    {
        public string BaseRuneId;
        public RuneData BaseRune;
        public List<RuneUpgradeBranchView> Branches = new();
        public List<RuneUpgradeNodeView> RootUpgrades = new();
    }

    public sealed class RuneUpgradeBranchView
    {
        public string BranchId;
        public List<RuneUpgradeNodeView> Nodes = new();
    }
}
