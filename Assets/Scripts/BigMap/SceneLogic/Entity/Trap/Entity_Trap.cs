using System.Collections.Generic;
using My.Map;
using My.Map.Logic;
using UnityEngine;

namespace My.Map.Entity
{
    public class TrapLogicEntity : LogicEntityBase
    {
        TrapSpecConfig _spec;
        bool _armed = true;
        float _sleepWakeAtLogicTime;

        public TrapLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.Trap;

        public TrapSpecConfig Spec => _spec;

        public bool IsArmedForScan => _armed && !MarkDestroyed;

        public float SleepWakeAtLogicTime => _sleepWakeAtLogicTime;

        protected override void LoadCfg()
        {
            _spec = TrapSpecRuntimeMap.Get(CfgId);
            if (_spec == null)
            {
                Debug.LogError($"TrapLogicEntity: TrapSpec not found in TbTrapSpec for CfgId '{CfgId}'.");
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (BindingRecord is LogicEntityRecord4Trap tr)
            {
                _armed = tr.Armed;
                _sleepWakeAtLogicTime = tr.SleepWakeAtLogicTime;
                if (_sleepWakeAtLogicTime > 0f && LogicTime.time < _sleepWakeAtLogicTime)
                {
                    _armed = false;
                }
                else if (_sleepWakeAtLogicTime > 0f && LogicTime.time >= _sleepWakeAtLogicTime)
                {
                    _armed = true;
                    _sleepWakeAtLogicTime = 0f;
                }
            }

            MarkSleep = !_armed;
        }

        protected override void RefreshEntityRecordInfo(LogicEntityRecord input)
        {
            base.RefreshEntityRecordInfo(input);
            if (input is LogicEntityRecord4Trap tr)
            {
                tr.Armed = _armed;
                tr.SleepWakeAtLogicTime = _sleepWakeAtLogicTime;
            }
        }

        public void TryWakeFromSleep()
        {
            if (MarkDestroyed)
            {
                return;
            }

            if (_sleepWakeAtLogicTime <= 0f || LogicTime.time < _sleepWakeAtLogicTime)
            {
                return;
            }

            _armed = true;
            _sleepWakeAtLogicTime = 0f;
            MarkSleep = false;
            SyncRecordForPersistence();
        }

        public bool TryTrigger(BaseUnitLogicEntity victim)
        {
            if (_spec == null || victim == null || MarkDestroyed || !_armed)
            {
                return false;
            }

            if (_spec.OnlyPlayer && victim is not PlayerLogicEntity)
            {
                return false;
            }

            var srcInfo = new GameLogicManager.EffectSourceInfo
            {
                SrcType = GameLogicManager.ESourceType.Mechanism,
                SrcEntityId = Id,
                SrcFactionId = FactionId,
                SrcCfgId = CfgId,
            };

            var dir = victim.Pos - Pos;
            if (dir.sqrMagnitude < 1e-8f)
            {
                dir = BindingRecord != null ? BindingRecord.FaceDir : Vector2.right;
            }
            else
            {
                dir.Normalize();
            }

            var ctx = new GameLogicManager.LogicFightEffectContext(LogicManager, GameLogicManager.EFightCtxType.Trap, srcInfo)
            {
                TargetId = victim.Id,
                TriggerPos = Pos,
                CastVec1 = dir,
            };

            if (_spec.TriggerEffects != null)
            {
                foreach (var eff in _spec.TriggerEffects)
                {
                    if (eff == null)
                    {
                        continue;
                    }

                    LogicManager.HandleLogicFightEffect(eff, ctx);
                }
            }

            switch (_spec.PostTrigger)
            {
                case ETrapPostTrigger.Destroy:
                    DoEntityDestroyed("trap_trigger");
                    break;
                case ETrapPostTrigger.SleepRecover:
                    _armed = false;
                    _sleepWakeAtLogicTime = LogicTime.time + Mathf.Max(0.01f, _spec.SleepDuration);
                    MarkSleep = true;
                    SyncRecordForPersistence();
                    break;
            }

            return true;
        }
    }
}
