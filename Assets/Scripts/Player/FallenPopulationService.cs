using System;
using cfg.demo;
using My.Saving;
using UnityEngine;

namespace My.Player
{
    // 沉沦人数：基础人数 + 扩散人数；对外展示一般用合计 Total
    public static class FallenPopulationService
    {
        public const string InitialHomeLogicAreaId = "home_01";
        public const long DefaultSpreadCapMul = 10;
        public const long DefaultSpreadGrowthPermille = 100;

        public static long GetTotal(PlayerSystemManager psm)
            => psm == null ? 0 : Math.Max(0, psm.FallenBaseAmount + psm.FallenSpreadAmount);

        public static void LoadFromSave(PlayerSystemManager psm, PlayerData pd)
        {
            if (psm == null) return;
            pd ??= new PlayerData();

            var baseAmt = Math.Max(0, pd.FallenBaseAmount);
            var spreadAmt = Math.Max(0, pd.FallenSpreadAmount);
            var legacyTotal = Math.Max(0, pd.TotalFallPeopleAmount);

            // 旧档：只有合计时，全部记入基础，扩散从 0 重新积
            if (baseAmt == 0 && spreadAmt == 0 && legacyTotal > 0)
            {
                baseAmt = legacyTotal;
                spreadAmt = 0;
                Debug.Log($"[FallenPopulation] Migrated legacy total {legacyTotal} into base.");
            }

            psm.SetFallenPopulation(baseAmt, spreadAmt);
        }

        public static void ApplyToSave(PlayerSystemManager psm, PlayerData pd)
        {
            if (psm == null || pd == null) return;
            pd.FallenBaseAmount = Math.Max(0, psm.FallenBaseAmount);
            pd.FallenSpreadAmount = Math.Max(0, psm.FallenSpreadAmount);
            pd.TotalFallPeopleAmount = GetTotal(psm);
        }

        public static void AddBaseAmount(PlayerSystemManager psm, long amount)
        {
            if (psm == null || amount == 0) return;
            if (amount < 0)
            {
                var next = Math.Max(0, psm.FallenBaseAmount + amount);
                psm.SetFallenPopulation(next, psm.FallenSpreadAmount);
                return;
            }

            psm.SetFallenPopulation(psm.FallenBaseAmount + amount, psm.FallenSpreadAmount);
        }

        public static long GetSpreadCap(long baseAmount, long capMul)
        {
            var mul = capMul > 0 ? capMul : DefaultSpreadCapMul;
            if (baseAmount <= 0) return 0;
            try
            {
                return checked(baseAmount * mul);
            }
            catch (OverflowException)
            {
                return long.MaxValue;
            }
        }

        public static int ResolveInitialHomeProsperity(GameLogicManager glm)
        {
            if (glm == null) return 0;
            var home = glm.homeDataManager;
            if (home != null
                && !string.IsNullOrEmpty(home.CurrentTownId)
                && string.Equals(home.CurrentTownId, InitialHomeLogicAreaId, StringComparison.Ordinal))
            {
                return Math.Max(0, home.TownProsperity);
            }

            var state = glm.worldPersistState?.GetLogicAreaHomesteadState(InitialHomeLogicAreaId);
            return Math.Max(0, state?.Prosperity ?? 0);
        }

        // 日结：基础慢增（默认0）+ 扩散公式增至上限
        public static void ApplyDailySettlement(GameLogicManager glm, GameLogicManager.OneDayBalanceInfo balanceInfo)
        {
            var psm = glm?.playerDataManager;
            if (psm == null || balanceInfo == null) return;

            var prog = psm.ProgressionSystem;
            long baseDaily = prog?.GetFinalAttribute((int)EYCAttribute.FallenBaseDailyAdd) ?? 0;
            long homeNeed = prog?.GetFinalAttribute((int)EYCAttribute.FallenBaseDailyHomeProsperityNeed) ?? 0;
            long homeAdd = prog?.GetFinalAttribute((int)EYCAttribute.FallenBaseDailyAddFromHome) ?? 0;
            long capMul = prog?.GetFinalAttribute((int)EYCAttribute.FallenSpreadCapMul) ?? 0;
            long growthPermille = prog?.GetFinalAttribute((int)EYCAttribute.FallenSpreadGrowthPermille) ?? 0;

            if (homeNeed > 0 && homeAdd != 0)
            {
                var prosperity = ResolveInitialHomeProsperity(glm);
                if (prosperity >= homeNeed)
                {
                    baseDaily += homeAdd;
                }
            }

            if (baseDaily < 0) baseDaily = 0;

            var baseBefore = Math.Max(0, psm.FallenBaseAmount);
            var spreadBefore = Math.Max(0, psm.FallenSpreadAmount);
            balanceInfo.FromFallenAmount = baseBefore + spreadBefore;
            balanceInfo.FromFallenBaseAmount = baseBefore;
            balanceInfo.FromFallenSpreadAmount = spreadBefore;

            var baseAfter = baseBefore + baseDaily;
            var spreadCap = GetSpreadCap(baseAfter, capMul);
            var growth = growthPermille > 0 ? growthPermille : DefaultSpreadGrowthPermille;
            var totalForFormula = Math.Max(1, baseAfter + spreadBefore);
            // 与旧式接近：total * (permille/1000) * sqrt(total)
            var rawSpreadAdd = (long)(totalForFormula * (growth / 1000.0) * Math.Sqrt(totalForFormula));
            if (rawSpreadAdd < 0) rawSpreadAdd = 0;

            var room = Math.Max(0, spreadCap - spreadBefore);
            var spreadAdd = Math.Min(rawSpreadAdd, room);

            var spreadAfter = spreadBefore + spreadAdd;
            psm.SetFallenPopulation(baseAfter, spreadAfter);

            balanceInfo.AddFallenBaseAmount = baseDaily;
            balanceInfo.AddFallenSpreadAmount = spreadAdd;
            balanceInfo.AddFallenAmount = baseDaily + spreadAdd;

            prog?.BaseStats?.OnFallenAmountUpdate(GetTotal(psm));
        }
    }
}
