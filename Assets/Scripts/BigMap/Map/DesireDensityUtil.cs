using System;
using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Map.Logic;

namespace My.Map
{
    public static class DesireDensityUtil
    {
        public static long RollInitialDensity(EDesireDensityType densityType)
        {
            var info = CfgMgr.Cfgs?.TbDesireDensityInfo?.GetOrDefault(densityType);
            if (info == null || densityType == EDesireDensityType.None)
            {
                return 0;
            }

            float sample = SampleNormal(info.DistributeMean, info.DistributeStdDev);
            long v = (long)Math.Round(sample);
            if (info.MinDensity > 0)
            {
                v = Math.Max(v, info.MinDensity);
            }

            if (info.MaxDensity > 0)
            {
                v = Math.Min(v, info.MaxDensity);
            }

            return Math.Max(0, v);
        }

        public static long GetHardCap(EDesireDensityType densityType)
        {
            var info = CfgMgr.Cfgs?.TbDesireDensityInfo?.GetOrDefault(densityType);
            if (info == null)
            {
                return 0;
            }

            return info.HardCap > 0 ? info.HardCap : info.MaxDensity;
        }

        public static long GetFinalDensity(NpcUnitLogicEntity npc)
        {
            if (npc?.NpcRecord == null)
            {
                return 0;
            }

            var type = npc.NpcRecord.DesireDensityType;
            if (type == EDesireDensityType.None)
            {
                return 0;
            }

            long initial = npc.NpcRecord.DesireDensity;
            long hardCap = GetHardCap(type);
            if (hardCap <= initial)
            {
                return Math.Min(initial, hardCap > 0 ? hardCap : initial);
            }

            long amplifyBp = npc.GetAttr(AttrIdConsts.DesireDensityAmplify);
            amplifyBp = Math.Clamp(amplifyBp, 0, 10000);
            double p = amplifyBp / 10000.0;
            long final = initial + (long)((hardCap - initial) * p);
            return Math.Clamp(final, 0, hardCap);
        }

        public static int GetDensityTier(long finalDensity)
        {
            var table = CfgMgr.Cfgs?.TbDesireDensityTier;
            if (table?.DataList == null)
            {
                return 0;
            }

            foreach (var row in table.DataList)
            {
                if (row != null && finalDensity >= row.MinDensity && finalDensity <= row.MaxDensity)
                {
                    return row.Tier;
                }
            }

            return 0;
        }

        static float SampleNormal(float mean, float stdDev)
        {
            if (stdDev <= 0f)
            {
                return mean;
            }

            double u1 = 1.0 - UnityEngine.Random.value;
            double u2 = 1.0 - UnityEngine.Random.value;
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)(mean + stdDev * randStdNormal);
        }
    }
}
