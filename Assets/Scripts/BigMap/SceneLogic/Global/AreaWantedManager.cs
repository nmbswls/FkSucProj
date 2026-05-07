
using System.Linq;
using cfg.demo;
using My.Config;
using My.Map;

namespace My
{
    public class AreaWantedManager
    {
        public const int WantedValScale = 1000;

        public int CurrentWantedVal;
        public float LastWantedTime;

        public void AddWantedVal(int val)
        {
            this.CurrentWantedVal += val * WantedValScale;
            LastWantedTime = LogicTime.time;
        }

        // 按 Luban 通缉行为表叠加（考虑 max_add_once 上限）
        public void AddWantedForBehavior(EWantedBehaveType behave)
        {
            if (behave == EWantedBehaveType.None)
            {
                return;
            }

            var row = CfgMgr.Cfgs?.TbWantedBehaveInfo?.GetOrDefault(behave);
            if (row == null)
            {
                return;
            }

            int v = row.AddWanted;
            if (row.MaxAddOnce > 0)
            {
                v = System.Math.Min(v, row.MaxAddOnce);
            }

            AddWantedVal(v);
        }

        // 与 wanted_level_info.need_val 对齐：内部值为 need_val * WantedValScale
        public int GetWantedStarLevel()
        {
            var table = CfgMgr.Cfgs?.TbWantedLevelInfo;
            if (table?.DataList == null || table.DataList.Count == 0)
            {
                return 0;
            }

            int best = 0;
            foreach (var row in table.DataList.OrderBy(r => r.Level))
            {
                if (row == null)
                {
                    continue;
                }

                if (CurrentWantedVal >= row.NeedVal * WantedValScale)
                {
                    best = row.Level;
                }
            }

            return best;
        }

        /// <summary>
        /// 检查衰减
        /// </summary>
        /// <param name="dt"></param>
        public void Tick(float dt)
        {
            if(LogicTime.time - LastWantedTime < 30.0f)
            {
                return;
            }

            CurrentWantedVal -= (int)(dt * 10 * WantedValScale);
            if (CurrentWantedVal < 0)
            {
                CurrentWantedVal = 0;
            }
        }
    }
}
