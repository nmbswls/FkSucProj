using System;

namespace My.Player
{
    // 遗留占位类型：勿在未初始化字段上调用 EvaluateStats。装备养成请走 GearEquipProgressionProvider + PlayerEquipmentManager。
    public class PlayerGear : IProgressionSource
    {
        public event Action<IProgressionSource> OnStatsChanged;

        public EProgressionModule ModuleName => EProgressionModule.Gear;

        public void EvaluateStats(StatMap targetMap)
        {
        }
    }
}
