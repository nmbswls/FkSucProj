
using My.Map.Fight;
using My.Map.Entity;
using My.Map;
using System.Collections.Generic;
using System.Linq;

namespace My
{
    public partial class GameLogicManager
    {

        /// <summary>
        /// ùùùpendingùùeffect
        /// </summary>
        private void TickPendingDelayedEffect()
        {
            if (_delayQueueDirty)
            {
                _delayQueueDirty = false;
                DelayedEffectQueue.Sort((itemA, itemB) => { return itemA.fixedExeTime.CompareTo(itemB.fixedExeTime); });
            }

            if (DelayedEffectQueue.Count > 0)
            {
                int handled = 0;
                while (DelayedEffectQueue.Count > 0 && handled < 10)
                {
                    if (LogicTime.time < DelayedEffectQueue[0].fixedExeTime)
                    {
                        break;
                    }

                    var wrapped = DelayedEffectQueue[0];
                    DelayedEffectQueue.RemoveAt(0);

                    switch (wrapped)
                    {
                        case DelayedFightEffectWrapper fightEffectWrapper:
                            {
                                ApplyLogicFightEffectImmediate(fightEffectWrapper.effectConf, fightEffectWrapper.ctx);
                            }
                            break;
                        case DelayedCostPeriodEffectWrapper nextPeriodWrapper:
                            {
                                TryCostPeriod(nextPeriodWrapper.CostPeriod);
                            }
                            break;
                    }

                    handled += 1;
                }
            }
        }

        public abstract class DelayedEffectWrapper
        {
            public float fixedExeTime;
        }
        public class DelayedFightEffectWrapper : DelayedEffectWrapper
        {
            public MapFightEffectCfg effectConf;
            public LogicFightEffectContext ctx;
        }
        public class DelayedBlockEffectWrapper : DelayedEffectWrapper
        {
        }

        public class DelayedCostPeriodEffectWrapper : DelayedEffectWrapper
        {
            public int CostPeriod;
        }

        public List<DelayedEffectWrapper> DelayedEffectQueue = new();

        private bool _delayQueueDirty = false;

        public void HandleLogicFightEffect(MapFightEffectCfg effectConf, LogicFightEffectContext effectCtx)
        {
            if (effectConf.PendingTime > 0)
            {
                DelayedEffectQueue.Add(new DelayedFightEffectWrapper()
                {
                    effectConf = effectConf,
                    ctx = effectCtx,
                    fixedExeTime = LogicTime.time + effectConf.PendingTime,
                });
                _delayQueueDirty = true;
                return;
            }

            ApplyLogicFightEffectImmediate(effectConf, effectCtx);
        }

        void ApplyLogicFightEffectImmediate(MapFightEffectCfg effectConf, LogicFightEffectContext effectCtx)
        {
            if (FightEffectInterceptors.ShouldBlockEffect(
                    this,
                    effectCtx.SourceInfo.SrcEntityId,
                    effectCtx.TargetId))
            {
                return;
            }

            var executor = GetLogicFightEffectExecutor(effectConf);
            executor?.Apply(effectConf, effectCtx);
        }

        public void PushPendingBlock(float blockTime = 1.0f)
        {
            DelayedEffectQueue.Add(new DelayedBlockEffectWrapper()
            {
                fixedExeTime = LogicTime.time + blockTime,
            });
            _delayQueueDirty = true;
        }

        public void PendingCostDayPeriod(int period = 1)
        {
            float preExecTime = LogicTime.time + 0.1f;
            if (DelayedEffectQueue.Count != 0)
            {
                preExecTime = DelayedEffectQueue.Last().fixedExeTime + 0.1f;
            }

            DelayedEffectQueue.Add(new DelayedCostPeriodEffectWrapper()
            {
                CostPeriod = period,
            });
            _delayQueueDirty = true;
        }
    }
}