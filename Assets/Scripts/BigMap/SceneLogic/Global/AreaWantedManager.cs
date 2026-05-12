
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using My.Map;
using My.Saving;

namespace My
{
    public class AreaWantedManager
    {
        public const int WantedValScale = 1000;

        private readonly Dictionary<EWantedBehaveType, int> _channelScaled = new();

        public float LastWantedTime;

        // 各罪类通道的最大值（缩放后），与 TbWantedLevelInfo.need_val * WantedValScale 比较
        public int CurrentWantedVal => GetEffectiveWantedScaled();

        public int GetEffectiveWantedScaled()
        {
            if (_channelScaled.Count == 0)
            {
                return 0;
            }

            return _channelScaled.Values.Max();
        }

        public void ClearAllWanted()
        {
            _channelScaled.Clear();
            LastWantedTime = 0f;
        }

        // GM 等：归入 StealSmall 通道并尊重配置 channel_cap
        public void AddWantedVal(int val)
        {
            ApplyLogicalIncrementToChannel(EWantedBehaveType.StealSmall, val);
            LastWantedTime = LogicTime.time;
        }

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

            ApplyLogicalIncrementToChannel(behave, v);
            LastWantedTime = LogicTime.time;
        }

        void ApplyLogicalIncrementToChannel(EWantedBehaveType behave, int logicalIncrement)
        {
            if (logicalIncrement <= 0)
            {
                return;
            }

            var row = CfgMgr.Cfgs?.TbWantedBehaveInfo?.GetOrDefault(behave);
            int scaledInc = logicalIncrement * WantedValScale;
            int cur = GetChannelScaled(behave);
            int next = cur + scaledInc;
            if (row != null && row.ChannelCap > 0)
            {
                int capScaled = row.ChannelCap * WantedValScale;
                next = System.Math.Min(next, capScaled);
            }

            _channelScaled[behave] = next;
        }

        int GetChannelScaled(EWantedBehaveType b)
        {
            return _channelScaled.TryGetValue(b, out var v) ? v : 0;
        }

        public void ImportFromPersist(List<WantedChannelPersist> list)
        {
            _channelScaled.Clear();
            if (list == null)
            {
                return;
            }

            foreach (var e in list)
            {
                if (e == null || e.ScaledVal <= 0)
                {
                    continue;
                }

                var bt = (EWantedBehaveType)e.BehaveType;
                if (bt == EWantedBehaveType.None)
                {
                    continue;
                }

                _channelScaled[bt] = e.ScaledVal;
            }
        }

        public List<WantedChannelPersist> ExportToPersist()
        {
            var r = new List<WantedChannelPersist>();
            foreach (var kv in _channelScaled)
            {
                if (kv.Value <= 0)
                {
                    continue;
                }

                r.Add(new WantedChannelPersist { BehaveType = (int)kv.Key, ScaledVal = kv.Value });
            }

            return r;
        }

        public void MigrateLegacySingleScalar(int legacyScaledTotal)
        {
            _channelScaled.Clear();
            if (legacyScaledTotal <= 0)
            {
                return;
            }

            var row = CfgMgr.Cfgs?.TbWantedBehaveInfo?.GetOrDefault(EWantedBehaveType.StealSmall);
            int capped = legacyScaledTotal;
            if (row != null && row.ChannelCap > 0)
            {
                capped = System.Math.Min(capped, row.ChannelCap * WantedValScale);
            }

            _channelScaled[EWantedBehaveType.StealSmall] = capped;
        }

        public int GetWantedStarLevel()
        {
            var table = CfgMgr.Cfgs?.TbWantedLevelInfo;
            if (table?.DataList == null || table.DataList.Count == 0)
            {
                return 0;
            }

            int effective = CurrentWantedVal;
            int best = 0;
            foreach (var row in table.DataList.OrderBy(r => r.Level))
            {
                if (row == null)
                {
                    continue;
                }

                if (effective >= row.NeedVal * WantedValScale)
                {
                    best = row.Level;
                }
            }

            return best;
        }

        public void Tick(float dt)
        {
            if (LogicTime.time - LastWantedTime < 30.0f)
            {
                return;
            }

            int dec = (int)(dt * 10 * WantedValScale);
            if (dec <= 0)
            {
                return;
            }

            var keys = _channelScaled.Keys.ToList();
            foreach (var k in keys)
            {
                int v = _channelScaled[k] - dec;
                if (v <= 0)
                {
                    _channelScaled.Remove(k);
                }
                else
                {
                    _channelScaled[k] = v;
                }
            }
        }
    }
}
