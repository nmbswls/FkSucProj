using Config;
using Config.Map;
using cfg.demo;
using My;
using My.Config;
using My.Map.Entity;
using My.Map.Logic;
using System.Collections.Generic;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;
using static My.Config.LogicInteractOutput;

namespace My.Map.Entity
{
    public class LogicEntityRemovableObstacle : LogicEntityInteractPoint
    {
        public const int StatusBlocked = 0;
        public const int StatusRemoved = 1;
        public const int RemoveInteractId = 1;
        public const string DefaultRemovedLocalSwitch = "removed";

        public MapRemovableObstacleConfig RemovableCfg { get; private set; }

        StatusInfo _statusBlocked;
        StatusInfo _statusRemoved;

        public LogicEntityRemovableObstacle(
            GameLogicManager logicManager,
            long instId,
            string cfgId,
            Vector2 orgPos,
            LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        protected override void LoadCfg()
        {
            RemovableCfg = MapRemovableObstacleLoader.Get(CfgId);
            RebuildSynthesizedStatuses();
        }

        public override EEntityType Type => EEntityType.RemovableObstacle;

        public override void Initialize()
        {
            base.Initialize();
            ApplyPersistedRemovedStateFromLocalSwitches();
        }

        void ApplyPersistedRemovedStateFromLocalSwitches()
        {
            if (RemovableCfg == null || !RemovableCfg.PersistByUniqName || string.IsNullOrEmpty(SrcUniqName))
            {
                return;
            }

            var switchName = MapInteractPointPersistUtil.GetRemovableObstacleRemovedSwitchName(CfgId);
            if (!CheckLocalSwitch(switchName) || CurrStatusId == StatusRemoved)
            {
                return;
            }

            CurrStatusId = StatusRemoved;
            var curState = GetCurrentStatusInfo();
            if (curState != null)
            {
                InteractComp?.RefreshInteractInfo(curState.InteractInfos);
            }
        }

        void RebuildSynthesizedStatuses()
        {
            _statusRemoved = new StatusInfo
            {
                StatusId = StatusRemoved,
                HasBlock = false,
                InteractInfos = new List<MapInteractInfo>(),
            };

            var outputs = new List<LogicInteractOutput>();
            if (RemovableCfg != null && RemovableCfg.ExtraOutputsOnRemove != null)
            {
                outputs.AddRange(RemovableCfg.ExtraOutputsOnRemove);
            }

            outputs.Add(new LogicInteractOutput
            {
                OutputType = EOutputType.ChangeSelfStatus,
                Param1 = StatusRemoved,
            });

            var label = RemovableCfg != null && !string.IsNullOrEmpty(RemovableCfg.RemoveInteractLabel)
                ? RemovableCfg.RemoveInteractLabel
                : "移除";

            _statusBlocked = new StatusInfo
            {
                StatusId = StatusBlocked,
                HasBlock = true,
                InteractInfos = new List<MapInteractInfo>
                {
                    new MapInteractInfo
                    {
                        InteractId = RemoveInteractId,
                        Label = label,
                        CheckCommonCond = RemovableCfg?.RemoveConds ?? new List<CommonCheckCond>(),
                        CheckInteractCond = RemovableCfg?.RemoveInteractConds ?? new List<InteractCheckCond>(),
                        Outputs = outputs,
                    },
                },
            };
        }

        public override StatusInfo GetCurrentStatusInfo()
        {
            if (CurrStatusId == StatusBlocked)
            {
                return _statusBlocked;
            }

            if (CurrStatusId == StatusRemoved)
            {
                return _statusRemoved;
            }

            Debug.LogWarning($"LogicEntityRemovableObstacle unknown status {CurrStatusId}, cfg={CfgId}");
            return _statusRemoved;
        }

        public override void CheckStatusCondition()
        {
            if (RemovableCfg?.UnlockRules == null)
            {
                return;
            }

            foreach (var rule in RemovableCfg.UnlockRules)
            {
                if (rule.FromStatus != CurrStatusId)
                {
                    continue;
                }

                var passed = true;
                foreach (var cond in rule.CommonConds)
                {
                    if (!LogicManager.CheckCommonCond(cond))
                    {
                        passed = false;
                        break;
                    }
                }

                foreach (var needFlag in rule.NeedSelfFlag)
                {
                    if (!CheckLocalSwitch(needFlag))
                    {
                        passed = false;
                        break;
                    }
                }

                if (passed)
                {
                    ChangeSelfStatus(rule.ToStatus, rule.ChangeView);
                    break;
                }
            }
        }

        public override void ChangeSelfStatus(int newStatus, StateChangeView changeView = null)
        {
            if (newStatus == StatusRemoved
                && changeView == null
                && RemovableCfg?.RemoveChangeView != null
                && RemovableCfg.RemoveChangeView.ChangingDuration > 0f)
            {
                changeView = RemovableCfg.RemoveChangeView;
            }

            base.ChangeSelfStatus(newStatus, changeView);

            if (newStatus == StatusRemoved)
            {
                PersistRemovedLocalSwitch();
            }
        }

        void PersistRemovedLocalSwitch()
        {
            if (RemovableCfg == null || !RemovableCfg.PersistByUniqName || string.IsNullOrEmpty(SrcUniqName))
            {
                return;
            }

            var switchName = MapInteractPointPersistUtil.GetRemovableObstacleRemovedSwitchName(CfgId);
            SetLocalSwitch(switchName, true);
        }
    }
}
