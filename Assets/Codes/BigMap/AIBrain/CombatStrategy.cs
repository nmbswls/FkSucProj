//using Map.Entity;
//using Map.Entity.AI;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace Map.Entity.AI.Stategy
//{
//    public enum AIStrategyType { Skill, Move, Stance, Utility }
//    public enum AIStrategyStatus { Idle, Running, Success, Failure, Interrupted }


//    public interface IAICombatStrategy
//    {
//        string Name { get; }
//        AIStrategyType Type { get; }
//        bool IsExclusive { get; }
//        bool AllowParallelMove { get; }
//        int BasePriority { get; }
//        float Cooldown { get; }
//        bool IsOnCooldown { get; }

//        AIStrategyStatus Status { get; }

//        float Evaluate(AIStrategyContext ctx);               // 返回效用值（<=0 表示不考虑）
//        void Start(AIStrategyContext ctx);
//        void Tick(AIStrategyContext ctx);
//        void Stop(AIStrategyContext ctx, AIStrategyStatus endStatus);
//        bool CanInterrupt(AIStrategyContext ctx, string reason, bool hard);
//        void UpdateCooldown(float dt);
//    }

//    public class AIStrategyContext
//    {
//        public MapUnitAIBrain AIBrain;
//        public BaseUnitLogicEntity SelfEntity;
//        public PlayerLogicEntity PlayerEntity;

//        // 每帧更新的感知快照
//        public float Distance;
//        public float AngleToPlayer;
//        public bool LineOfSight;
//        public bool InBoundary;
//        public int Ammo;

//        public float DeltaTime;
//        public float Time;
//    }


//    public interface IAIStrategySelector
//    {
//        IAICombatStrategy Select(AIStrategyContext ctx, IList<IAICombatStrategy> strategies);
//    }

//    public class BasicAISelector : IAIStrategySelector
//    {
//        public float PriorityWeight = 1f;
//        public float UtilityWeight = 1f;

//        public IAICombatStrategy Select(AIStrategyContext ctx, IList<IAICombatStrategy> list)
//        {
//            IAICombatStrategy best = null;
//            float bestScore = float.NegativeInfinity;
//            foreach (var s in list)
//            {
//                if (s.IsOnCooldown) continue;
//                float u = s.Evaluate(ctx);
//                if (u <= 0) continue;
//                float score = s.BasePriority * PriorityWeight + u * UtilityWeight;
//                if (score > bestScore) { bestScore = score; best = s; }
//            }
//            return best;
//        }
//    }

//    /// <summary>
//    /// 战斗策略管理器 用于选择并追踪状态
//    /// </summary>
//    public class AICombatStrategyManager
//    {
//        private readonly List<IAICombatStrategy> _all = new();
//        private readonly List<IAICombatStrategy> _running = new();
//        private readonly IAIStrategySelector _selector;

//        public AICombatStrategyManager(IAIStrategySelector selector) { _selector = selector; }

//        public void AddStrategy(IAICombatStrategy s) => _all.Add(s);

//        public void Update(AIStrategyContext ctx)
//        {
//            // 冷却更新 + 执行中策略 Tick
//            foreach (var s in _all) s.UpdateCooldown(ctx.DeltaTime);

//            for (int i = _running.Count - 1; i >= 0; --i)
//            {
//                _running[i].Tick(ctx);
//            }

//            // 清理：移除已结束策略
//            for (int i = _running.Count - 1; i >= 0; --i)
//            {
//                if (_running[i].Status != AIStrategyStatus.Running)
//                    _running.RemoveAt(i);
//            }

//            // 有独占策略在运行则不再选择新策略
//            bool hasExclusive = _running.Exists(s => s.IsExclusive);
//            if (!hasExclusive)
//            {
//                var chosen = _selector.Select(ctx, _all);
//                if (chosen != null && !_running.Contains(chosen))
//                {
//                    if (chosen.IsExclusive)
//                    {
//                        TryInterruptAll(ctx, hard: true, reason: "Exclusive");
//                        _running.Clear();
//                    }
//                    chosen.Start(ctx);
//                    _running.Add(chosen);
//                }
//            }

//            // 清理已经结束的策略: 这里假设策略在 Stop 后自行进入冷却且不再 Tick
//            // 若需要自动判断结束，可扩展 IStrategy 暴露状态。
//        }

//        public void TryInterruptAll(AIStrategyContext ctx, bool hard, string reason)
//        {
//            for (int i = _running.Count - 1; i >= 0; --i)
//            {
//                var s = _running[i];
//                if (s.CanInterrupt(ctx, reason, hard))
//                {
//                    s.Stop(ctx, AIStrategyStatus.Interrupted);
//                    _running.RemoveAt(i);
//                }
//            }
//        }

//        public bool HasExclusiveRunning() => _running.Exists(s => s.IsExclusive);
//    }

//    public class DistanceControlStrategy : IAICombatStrategy
//    {
//        public string Name => "DistanceControl";
//        public AIStrategyType Type => AIStrategyType.Move;
//        public bool IsExclusive => false;
//        public bool AllowParallelMove => true;
//        public int BasePriority { get; private set; } = 1;
//        public float Cooldown { get; private set; } = 0;
//        public bool IsOnCooldown => false;

//        // 参数列表
//        public float goodDistance;
//        public float goodDiff;

//        private float _timer;
//        public AIStrategyStatus Status { get; set; }
//        public DistanceControlStrategy(float goodDistance, float goodDiff = 0.1f, float maxDuration = 5f)
//        {
//            this.goodDistance = goodDistance;
//            this.goodDiff = goodDiff;
//            _timer = maxDuration;
//        }

//        public float Evaluate(AIStrategyContext ctx)
//        {
//            if(ctx.Distance < goodDistance + 1f && ctx.Distance < goodDistance  - 1f)
//            {
//                return 0;
//            }
//            return 1;
//        }

//        public void Start(AIStrategyContext ctx)
//        {
//            Status = AIStrategyStatus.Running;
//            var targetPos = ctx.AIBrain.Vision.ChoosePointAwayFromTarget(ctx.SelfEntity.Pos, ctx.PlayerEntity.Pos, goodDistance);
//            ctx.SelfEntity.StartTargettedMove(targetPos, 0.1f);
//        }

//        public void Tick(AIStrategyContext ctx)
//        {
//            if (ctx.Distance < goodDistance + 1f && ctx.Distance < goodDistance - 1f)
//            {
//                Stop(ctx, AIStrategyStatus.Success);
//                return;
//            }

//            _timer -= ctx.DeltaTime;
//            if (_timer <= 0)
//            {
//                Stop(ctx, AIStrategyStatus.Success);
//            }
//        }

//        public void Stop(AIStrategyContext ctx, AIStrategyStatus endStatus)
//        {

//        }

//        public bool CanInterrupt(AIStrategyContext ctx, string reason, bool hard) => true;

//        public void UpdateCooldown(float dt) {  }
//    }


//    public class PrimaryUseSkillStrategy : IAICombatStrategy
//    {
//        public string Name => "PrimaryUseSkill";
//        public AIStrategyType Type => AIStrategyType.Skill;
//        public bool IsExclusive => true;
//        public bool AllowParallelMove => false;
//        public int BasePriority { get; private set; }
//        public float Cooldown { get; private set; }
//        public bool IsOnCooldown => false;

//        public AIStrategyStatus Status { get; set; }

//        public float DistanceRequirement;

//        private float durationTimer;
//        private float lastEndTimer;

//        private float relaxDuringAttack = 1f;

//        private MapAbilitySpecConfig? currentWantUseAbility = null;
//        private bool isCastAbility = false;

//        public PrimaryUseSkillStrategy(float relaxDuringAttack)
//        {
//            this.relaxDuringAttack = relaxDuringAttack;
//        }

//        public float Evaluate(AIStrategyContext ctx)
//        {
//            bool findUse = false;
//            foreach(var state in ctx.SelfEntity.abilityController.AbilityStateInfos)
//            {
//                if(state.Value.cacheConfig.IsPassive)
//                {
//                    continue;
//                }
//                if(state.Value.cacheConfig.TypeTag != Map.Entity.AbilityTypeTag.Combat)
//                {
//                    continue;
//                }
//                if(ctx.Time > state.Value.lastUseTime + state.Value.lastUseCd)
//                {
//                    findUse = true;
//                    break;
//                }
//            }

//            if (!findUse) return 0;

//            if(ctx.Time - lastEndTimer < relaxDuringAttack)
//            {
//                return 0;
//            }

//            return 11;
//        }

//        public void Start(AIStrategyContext ctx)
//        {
//            Status = AIStrategyStatus.Running;

//            foreach (var state in ctx.SelfEntity.abilityController.AbilityStateInfos.Values)
//            {
//                if (state.cacheConfig.IsPassive)
//                {
//                    continue;
//                }
//                if (state.cacheConfig.TypeTag != Map.Entity.AbilityTypeTag.Combat)
//                {
//                    continue;
//                }
//                if (ctx.Time > state.lastUseTime + state.lastUseCd)
//                {
//                    currentWantUseAbility = state.cacheConfig;
//                    break;
//                }
//            }

//            durationTimer = 5.0f;
//            isCastAbility = false;

//            if (currentWantUseAbility == null)
//            {
//                Debug.LogError("currentWantUseAbility not found");
//                return;
//            }
//            var targetPos = ctx.AIBrain.Vision.ChoosePointAwayFromTarget(ctx.SelfEntity.Pos, ctx.PlayerEntity.Pos, currentWantUseAbility.DesiredUseDistance);
//            ctx.SelfEntity.StartTargettedMove(targetPos, 0.1f);
//        }

//        public void Tick(AIStrategyContext ctx)
//        {
//            if (Status != AIStrategyStatus.Running) return;

//            // 保底中断 避免卡在那里
//            durationTimer -= ctx.DeltaTime;
//            if (durationTimer <= 0)
//            {
//                Stop(ctx, AIStrategyStatus.Success);
//                return;
//            }

//            if(currentWantUseAbility == null)
//            {
//                return;
//            }
//            if(!isCastAbility && currentWantUseAbility.DesiredUseDistance > 0 && ctx.Distance < currentWantUseAbility.DesiredUseDistance)
//            {
//                var dir = ctx.PlayerEntity.Pos - ctx.SelfEntity.Pos;
//                ctx.SelfEntity.abilityController.TryUseAbility(currentWantUseAbility.name, dir);
//                isCastAbility = true;
//            }
//            else if(isCastAbility)
//            {
//                if (!ctx.SelfEntity.abilityController.IsRunning)
//                {
//                    Stop(ctx, AIStrategyStatus.Success);
//                }
//            }
//        }

//        public void Stop(AIStrategyContext ctx, AIStrategyStatus endStatus)
//        {
//            if (Status == AIStrategyStatus.Idle) return;
//            lastEndTimer = ctx.Time;
//            Status = endStatus;
//            currentWantUseAbility = null;
//        }

//        public bool CanInterrupt(AIStrategyContext ctx, string reason, bool hard) => false;

//        public void UpdateCooldown(float dt) { ; }
//    }

//    public class PrimaryShotStrategy : IAICombatStrategy
//    {
//        public string Name => "PrimaryShot";
//        public AIStrategyType Type => AIStrategyType.Skill;
//        public bool IsExclusive => false;
//        public bool AllowParallelMove => true;
//        public int BasePriority { get; private set; }
//        public float Cooldown { get; private set; }
//        public bool IsOnCooldown => _cd > 0;

//        public AIStrategyStatus Status { get; set; }

//        private float _windup; // 前摇时间
//        private float _timer;
//        private float _cd;

//        public PrimaryShotStrategy(int priority, float cd, float windup = 0.05f)
//        {
//            BasePriority = priority; Cooldown = cd; _windup = windup;
//        }

//        public float Evaluate(AIStrategyContext ctx)
//        {
//            //if (ctx.Ammo <= 0) return 0f;
//            if (!ctx.LineOfSight) return 0f;
//            if (ctx.Distance > 2f) return 0f;
//            // 中距离更高
//            float d = ctx.Distance;
//            //float score = Mathf.Clamp01((d - 1f) / 4f) * 5f; // 0..5
//            return 1;
//        }

//        public void Start(AIStrategyContext ctx)
//        {
//            Status = AIStrategyStatus.Running;
//            _timer = _windup;
//        }

//        public void Tick(AIStrategyContext ctx)
//        {
//            if (Status != AIStrategyStatus.Running) return;

//            _timer -= ctx.DeltaTime;
//            if (_timer <= 0)
//            {
//                Vector2 dir = (ctx.PlayerEntity.Pos - ctx.SelfEntity.Pos).normalized;
//                //ctx.Combat.FireProjectile(ctx.Self.position, dir, 2f);
//                //ctx.Combat.CurrentAmmo = Mathf.Max(0, ctx.Combat.CurrentAmmo - 1);
//                if(ctx.SelfEntity is NpcUnitLogicEntity npcEntity)
//                {
//                    Debug.Log("NpcUnitLogicEntity slash once");
//                    npcEntity.abilityController.TryUseAbility("slash", dir);
//                }
//                Stop(ctx, AIStrategyStatus.Success);
//            }
//        }

//        public void Stop(AIStrategyContext ctx, AIStrategyStatus endStatus)
//        {
//            if (Status == AIStrategyStatus.Idle) return;
//            _cd = Cooldown;
//            Status = endStatus;
//        }

//        public bool CanInterrupt(AIStrategyContext ctx, string reason, bool hard) => true;

//        public void UpdateCooldown(float dt) { if (_cd > 0) _cd -= dt; }
//    }
//}