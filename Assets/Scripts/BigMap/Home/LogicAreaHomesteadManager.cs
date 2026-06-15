using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.Home
{
    // 以 logic_area map_id 为键，管理各家园区域的建筑升级运行时
    public class LogicAreaHomesteadManager
    {
        readonly GameLogicManager _logic;

        public event Action<string, string, int> EvOnBuildingLevelChanged;

        public LogicAreaHomesteadManager(GameLogicManager logic)
        {
            _logic = logic;
        }

        public bool IsAreaUnderPlayerControl(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || _logic?.worldPersistState == null)
            {
                return false;
            }

            if (_logic.worldPersistState.IsLogicAreaAnnexed(logicAreaId))
            {
                return true;
            }

            return _logic.worldPersistState.IsLogicAreaControlRequirementMet(logicAreaId);
        }

        public bool CanOpenManagementForCurrentArea()
        {
            var logicAreaId = ResolveCurrentLogicAreaId();
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return false;
            }

            return IsAreaUnderPlayerControl(logicAreaId)
                && LogicAreaHomesteadUtil.HasManageableBuildings(logicAreaId);
        }

        public string ResolveCurrentLogicAreaId()
        {
            return LogicAreaHomesteadUtil.ResolveCurrentLogicAreaId(_logic?.AreaManager);
        }

        public int GetBuildingLevel(string logicAreaId, string buildingId)
        {
            return _logic?.worldPersistState?.GetHomesteadBuildingLevel(logicAreaId, buildingId) ?? 0;
        }

        public IReadOnlyList<HomesteadBuilding> GetBuildingDefs(string logicAreaId)
        {
            return LogicAreaHomesteadUtil.GetBuildingDefsForArea(logicAreaId);
        }

        public HomesteadBuildingUpgrade GetNextUpgradeDef(string logicAreaId, string buildingId)
        {
            var current = GetBuildingLevel(logicAreaId, buildingId);
            return LogicAreaHomesteadUtil.GetBuildingUpgradeDef(logicAreaId, buildingId, current + 1);
        }

        public bool CanUpgradeBuilding(string logicAreaId, string buildingId, out string failReason)
        {
            failReason = null;
            if (!IsAreaUnderPlayerControl(logicAreaId))
            {
                failReason = "area_not_controlled";
                return false;
            }

            var buildingDef = LogicAreaHomesteadUtil.GetBuildingDef(logicAreaId, buildingId);
            if (buildingDef == null)
            {
                failReason = "no_building_cfg";
                return false;
            }

            var current = GetBuildingLevel(logicAreaId, buildingId);
            if (current >= buildingDef.MaxLevel)
            {
                failReason = "max_level";
                return false;
            }

            var upgradeDef = LogicAreaHomesteadUtil.GetBuildingUpgradeDef(logicAreaId, buildingId, current + 1);
            if (upgradeDef == null)
            {
                failReason = "no_upgrade_cfg";
                return false;
            }

            if (_logic == null)
            {
                failReason = "no_logic";
                return false;
            }

            if (upgradeDef.UnlockConds != null && upgradeDef.UnlockConds.Count > 0
                && !_logic.CheckCommonCondsAll(upgradeDef.UnlockConds))
            {
                failReason = "unlock_cond_fail";
                return false;
            }

            if (!HasUpgradeCosts(upgradeDef, out failReason))
            {
                return false;
            }

            return true;
        }

        public bool TryUpgradeBuilding(string logicAreaId, string buildingId, out string failReason)
        {
            if (!CanUpgradeBuilding(logicAreaId, buildingId, out failReason))
            {
                return false;
            }

            var current = GetBuildingLevel(logicAreaId, buildingId);
            var upgradeDef = LogicAreaHomesteadUtil.GetBuildingUpgradeDef(logicAreaId, buildingId, current + 1);
            if (upgradeDef == null)
            {
                failReason = "no_upgrade_cfg";
                return false;
            }

            if (!PayUpgradeCosts(upgradeDef, out failReason))
            {
                return false;
            }

            var nextLevel = current + 1;
            _logic.worldPersistState.SetHomesteadBuildingLevel(logicAreaId, buildingId, nextLevel);
            EvOnBuildingLevelChanged?.Invoke(logicAreaId, buildingId, nextLevel);
            return true;
        }

        bool HasUpgradeCosts(HomesteadBuildingUpgrade upgradeDef, out string failReason)
        {
            failReason = null;
            var pdm = _logic?.playerDataManager;
            if (pdm == null)
            {
                failReason = "no_player_mgr";
                return false;
            }

            if (upgradeDef?.UnlockCosts == null || upgradeDef.UnlockCosts.Count == 0)
            {
                return true;
            }

            foreach (var cost in upgradeDef.UnlockCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (!pdm.CheckHaveItem(cost.ItemId, cost.Count))
                {
                    failReason = "not_enough_item";
                    return false;
                }
            }

            return true;
        }

        bool PayUpgradeCosts(HomesteadBuildingUpgrade upgradeDef, out string failReason)
        {
            failReason = null;
            var pdm = _logic?.playerDataManager;
            if (pdm == null)
            {
                failReason = "no_player_mgr";
                return false;
            }

            if (upgradeDef?.UnlockCosts == null || upgradeDef.UnlockCosts.Count == 0)
            {
                return true;
            }

            foreach (var cost in upgradeDef.UnlockCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (pdm.CostItem(cost.ItemId, cost.Count) < cost.Count)
                {
                    failReason = "not_enough_item";
                    return false;
                }
            }

            return true;
        }
    }
}
