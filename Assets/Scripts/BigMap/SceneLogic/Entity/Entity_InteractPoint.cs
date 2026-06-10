using Config;
using Config.Map;
using Map.Logic.Events;
using My;
using My.Config;
using My.Map;
using My.Map.Logic;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;

namespace My.Map.Entity
{


    public class LogicEntityInteractPoint : LogicEntityBase, IWithInteract, IEntityInteractable
    {
        // ??
        //public bool Appear = false;
        public int CurrStatusId = 0;
        private bool IsSwitching = false;
        public Dictionary<string, string> DynamicVariables = new();

        float _poisonBaitEndTime;
        float _poisonCdEndTime;

        float _revealUntilTime;

        public bool IsDormantHidden { get; private set; }

        public bool IsLogicInteractAvailable =>
            !IsDormantHidden || LogicTime.time < _revealUntilTime;

        public event Action EventOnDormantRevealChanged;

        public MapInteractPointConfig cacheCfg;

        public EntityInteractComp InteractComp;

        public event Action<StateChangeView> EventOnStatusChange;
        public event Action<string, float> EventOnSelfAnim;

        public void NotifySelfAnim(string animName, float durationSec)
        {
            EventOnSelfAnim?.Invoke(animName, durationSec);
        }


        public LogicEntityInteractPoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            var realRecord = bindingRecord as LogicEntityRecord4InteractPoint;
            CurrStatusId = realRecord.Status;
            DynamicVariables.AddRange(realRecord.DynamicVariables);
            _poisonBaitEndTime = realRecord.PoisonBaitEndTime;
            _poisonCdEndTime = realRecord.PoisonCdEndTime;
        }

        protected virtual void LoadCfg()
        {
            cacheCfg = MapInteractPointLoader.Get(CfgId);
        }

        public override EEntityType Type => EEntityType.InteractPoint;

        public override WorldMapLandmarkKind WorldMapLandmark
        {
            get
            {
                if (cacheCfg == null) return base.WorldMapLandmark;
                if (cacheCfg.ShowOnWorldMap) return WorldMapLandmarkKind.MajorInteract;
                if (WorldMapRuntime.IsGlobalInteractLandmark(CfgId))
                    return WorldMapLandmarkKind.MajorInteract;
                return base.WorldMapLandmark;
            }
        }

        public override string WorldMapLandmarkLabel
        {
            get
            {
                if (cacheCfg == null) return CfgId;
                if (!string.IsNullOrEmpty(cacheCfg.WorldMapMarkerLabel)) return cacheCfg.WorldMapMarkerLabel;
                if (!string.IsNullOrEmpty(cacheCfg.ShowName)) return cacheCfg.ShowName;
                return CfgId;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            LoadCfg();

            InteractComp = new(this);

            var curState = GetCurrentStatusInfo();
            if (curState != null)
            {
                InteractComp.RefreshInteractInfo(curState.InteractInfos);
            }

            CheckStatusCondition();
            ApplyInitialDormantState();
        }

        protected override void OnLocalSwitchesMutated()
        {
            base.OnLocalSwitchesMutated();
            SyncLocalSwitchesToPersistRegistryIfNeeded();
        }

        void SyncLocalSwitchesToPersistRegistryIfNeeded()
        {
            if (string.IsNullOrEmpty(SrcUniqName))
            {
                return;
            }

            if (!MapInteractPointPersistUtil.ShouldPersistEntity(Type, CfgId))
            {
                return;
            }

            LogicManager?.worldPersistState?.MapInteractPoints?.ReplaceRuntimeLocalSwitches(
                SrcUniqName, EntityLocalSwitches);
        }

        bool DormantRevealEnabled() =>
            cacheCfg != null && cacheCfg.DormantRevealSettings != null && cacheCfg.DormantRevealSettings.Enable;

        InteractPointDormantRevealSettings DormantSettings => cacheCfg?.DormantRevealSettings;

        void ApplyInitialDormantState()
        {
            if (DormantRevealEnabled())
            {
                IsDormantHidden = true;
                _revealUntilTime = 0f;
                return;
            }

            IsDormantHidden = false;
            _revealUntilTime = 0f;
        }

        void TryRevealByGcLiquid()
        {
            if (!DormantRevealEnabled() || !IsDormantHidden)
            {
                return;
            }

            var settings = DormantSettings;
            float duration = settings != null ? settings.RevealDurationSeconds : 8f;
            if (duration <= 0f)
            {
                duration = 8f;
            }

            _revealUntilTime = LogicTime.time + duration;
            EventOnDormantRevealChanged?.Invoke();
        }

        void TickDormantReveal()
        {
            if (!DormantRevealEnabled() || !IsDormantHidden)
            {
                return;
            }

            if (_revealUntilTime <= 0f)
            {
                return;
            }

            if (LogicTime.time >= _revealUntilTime)
            {
                _revealUntilTime = 0f;
                EventOnDormantRevealChanged?.Invoke();
            }
        }

        public string GetRuntimeVariable(string paramName)
        {
            DynamicVariables.TryGetValue(paramName, out var value);
            return value;
        }

        public virtual StatusInfo GetCurrentStatusInfo()
        {
            if(CurrStatusId == 0)
            {
                return cacheCfg.MainStatusInfo;
            }

            var findIt = cacheCfg.ExtraStatusInfos.Find((item)=>item.StatusId == CurrStatusId);
            return findIt;

        }

        /// <summary>
        /// ??????
        /// </summary>
        public virtual void CheckStatusCondition()
        {
            foreach(var rule in cacheCfg.StateChangeRules)
            {
                if(rule.FromStatus != CurrStatusId)
                {
                    continue;
                }

                var poassed = true;
                foreach(var cond in rule.CommonConds)
                {
                    if (!LogicManager.CheckCommonCond(cond))
                    {
                        poassed = false;
                        break;
                    }
                }

                foreach (var needFlag in rule.NeedSelfFlag)
                {
                    if (!this.CheckLocalSwitch(needFlag))
                    {
                        poassed = false;
                        break;
                    }
                }

                if (poassed)
                {
                    ChangeSelfStatus(rule.ToStatus, rule.ChangeView);
                    break;
                }
            }
        }

        /// <summary>
        /// ??????
        /// </summary>
        public bool IsInteracting { get { return InteractComp.IsInteracting; } }

        public List<MapInteractInfo> InteractInfos { get { return InteractComp.InteractInfos; } }

        public bool TryTriggerInteract(int interactId, int playerId)
        {
            return InteractComp.TryTriggerInteract(interactId, playerId);
        }

        public bool CheckTriggerInteract(int interactId, int playerId)
        {
            return InteractComp.CheckTriggerInteract(interactId, playerId);
        }


        public override void OnMapLogicEvent(IMapLogicEvent evt)
        {
            base.OnMapLogicEvent(evt);

            CheckStatusCondition(); 
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            

            // ????????
            LowFreqCheckStatusChange();

            InteractComp?.TickInteract(dt);

            TickPoisonBait(dt);
            TickDormantReveal();
        }

        protected override bool CanTickGroundOverlay()
        {
            return DormantRevealEnabled();
        }

        private float _lowFreqCheckStatusTimer = 0;
        /// <summary>
        /// ????
        /// </summary>
        protected void LowFreqCheckStatusChange()
        {
            if(LogicTime.time < _lowFreqCheckStatusTimer)
            {
                return;
            }

            _lowFreqCheckStatusTimer = LogicTime.time + 2f;

            CheckStatusCondition();
        }

        public virtual void ChangeSelfStatus(int newStatus, StateChangeView changeView = null)
        {
            int oldStat = CurrStatusId;
            CurrStatusId = newStatus;

            var curState = GetCurrentStatusInfo();
            if (curState != null)
            {
                InteractComp.RefreshInteractInfo(curState.InteractInfos);
            }
            else
            {
                InteractComp.RefreshInteractInfo(new());
            }

            OnStatusChange(oldStat);
            EventOnStatusChange?.Invoke(changeView);
        }

        

        protected virtual void OnStatusChange(int preStage)
        {

        }

        public bool PoisonFeatureEnabled()
        {
            return cacheCfg != null && cacheCfg.PoisonSettings != null && cacheCfg.PoisonSettings.Enable;
        }

        public bool IsPoisonBaitWindowActive()
        {
            return PoisonFeatureEnabled() && LogicTime.time < _poisonBaitEndTime;
        }

        public bool CanPlayerOfferPoisonInteract()
        {
            if (!PoisonFeatureEnabled())
            {
                return false;
            }

            if (IsInteracting)
            {
                return false;
            }

            if (IsPoisonBaitWindowActive())
            {
                return false;
            }

            if (_poisonCdEndTime > LogicTime.time)
            {
                return false;
            }

            var ps = cacheCfg.PoisonSettings;
            return LogicManager.CheckCommonCondsAll(ps.ApplyPoisonConds);
        }

        public bool TryPlayerApplyPoison()
        {
            if (!CanPlayerOfferPoisonInteract())
            {
                return false;
            }

            var ps = cacheCfg.PoisonSettings;
            float dur = Mathf.Max(0.1f, ps.BaitDurationSeconds);
            _poisonBaitEndTime = LogicTime.time + dur;
            LogicManager.RegisterPoisonLacedInteract(Id);
            return true;
        }

        public bool TryNpcConsumePoisonBait(long _)
        {
            if (!IsPoisonBaitWindowActive())
            {
                return false;
            }

            var ps = cacheCfg.PoisonSettings;
            _poisonBaitEndTime = 0f;
            _poisonCdEndTime = LogicTime.time + Mathf.Max(0f, ps.ReapplyCooldownSeconds);
            LogicManager.UnregisterPoisonLacedInteract(Id);
            return true;
        }

        void TickPoisonBait(float dt)
        {
            if (!PoisonFeatureEnabled())
            {
                return;
            }

            if (_poisonBaitEndTime <= 0f)
            {
                return;
            }

            if (LogicTime.time < _poisonBaitEndTime)
            {
                return;
            }

            var ps = cacheCfg.PoisonSettings;
            _poisonBaitEndTime = 0f;
            _poisonCdEndTime = LogicTime.time + Mathf.Max(0f, ps.ReapplyCooldownSeconds);
            LogicManager.UnregisterPoisonLacedInteract(Id);
        }

        public override void OnDespawn(ref LogicEntityRecord snapshot)
        {
            LogicManager.UnregisterPoisonLacedInteract(Id);
            base.OnDespawn(ref snapshot);
        }
    

        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);

            var realRecord = input as LogicEntityRecord4InteractPoint;
            if (MapInteractPointPersistUtil.ShouldPersistEntity(Type, CfgId))
            {
                realRecord.Status = 0;
            }
            else
            {
                realRecord.Status = CurrStatusId;
            }

            realRecord.DynamicVariables.Clear();
            realRecord.DynamicVariables.AddRange(DynamicVariables);
            realRecord.PoisonBaitEndTime = _poisonBaitEndTime;
            realRecord.PoisonCdEndTime = _poisonCdEndTime;
        }


        protected override void OnLiquidRemove(EGroundLiquidType liquidType)
        {
            if (liquidType == EGroundLiquidType.GcLiquid)
            {
                TryRevealByGcLiquid();
            }
        }
    }



}


