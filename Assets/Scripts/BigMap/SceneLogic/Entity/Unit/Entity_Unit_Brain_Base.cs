using System.Collections;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Unit
{
    // --- 配置 ---
    [System.Serializable]
    public class AIBrainConfig
    {
        public float PatrolRadius = 5f;
        public float AttackRange = 2.0f;

        public float ChaseRange = 15.0f;
        public float SearchDuration = 5.0f;  // 搜索持续时间
        public float IdleWaitTime = 3.0f;

        public IdleType IdleType = IdleType.StandStill; // 枚举定义闲置类型
        public List<Vector3> PatrolPoints;   // 巡逻点数据
        public float WanderInterval = 5.0f;

        public bool IsPeace; // 和平单位只会逃
        public float CombatCloseDistance = 2.0f;
        public float CombatFarDistance = 5.0f;

        public string SpecialAnimTag1;
        public string SpecialAnimTag2;
        public string SpecialAnimTag3;
        public string SpecialAnimTag4;

        public bool IsGuard;
    }

    public static class AIBrainParamsConfigLoader
    {
        public static Dictionary<string, AIBrainConfig> _configs = null;

        public static AIBrainConfig Load(string name)
        {
            if (_configs == null)
            {
                _configs = new();
                {
                    //var config = ScriptableObject.CreateInstance<AIBrainConfig>();
                    var config = new AIBrainConfig();
                    _configs["default"] = config;
                }
                {
                    //var config = ScriptableObject.CreateInstance<AIBrainConfig>();
                    var config = new AIBrainConfig();
                    _configs["basic_unit_peace"] = config;
                }
                {
                    //var config = ScriptableObject.CreateInstance<AIBrainConfig>();
                    var config = new AIBrainConfig();
                    _configs["default_guard"] = config;
                    config.IsGuard = true;
                }

                {
                    //var config = ScriptableObject.CreateInstance<AIBrainConfig>();
                    var config = new AIBrainConfig();
                    config.ChaseRange = 999;
                    config.IdleType = IdleType.Hunting;

                    _configs["h_spirit"] = config;
                }
            }

            _configs.TryGetValue(name, out var result);
            return result;
        }
    }

    public static class AIBrainFactory
    {
        public static AIBrainV2 CreateAIBrain(NpcUnitLogicEntity npcOwner)
        {
            return new AIBrainV2(npcOwner);
        }
    }

    public enum IdleType 
    { 
        StandStill, 
        Patrol, 
        Wander,
        Hunting,
        FollowGroup,
    }

    // --- 大脑 (Controller) ---
    public class AIBrainV2
    {
        // 组件引用
        public NpcUnitLogicEntity NpcEntity; // 实体逻辑
        public AIBrainConfig Config;         // 配置

        public GameLogicManager LogicManager { get {  return NpcEntity.LogicManager; } }

        // 状态机
        public AIBaseState CurrentState { get; private set; }

        public IVisionSenser2D Vision { get { return LogicManager.visionSenser; } }

        // 预加载状态 (避免GC)
        public AIStateIdle StateIdle;
        public AIStateCombat StateCombat; 
        public AIStateReturn StateReturn;
        public AIStateFlee StateFlee;
        public AIStateSearch StateSearch;
        public AIStateAttracted StateAttracted;
        public AIStateChaseWanted StateChaseWanted;
        public AIStateCharmedFollow StateCharmedFollow;


        // 黑板 (Blackboard) - 状态间共享数据
        public Vector2? HomePos;
        public Vector2? SuspiciousPos; // <--- 搜索目标点 (最后目击位置/声音来源)

        

        public UnitAggroSystem Aggro => NpcEntity.AggroSystem;

        private bool _isChangingState;

        public float ActionsFrequency = 0.2f;
        private float _lastBrainUpdate = 0;

        public AIBrainV2(NpcUnitLogicEntity npcOwner)
        {
            this.NpcEntity = npcOwner;
            this.HomePos = npcOwner.Pos; // 记录出生点

            string cfgId = "default";
            if (npcOwner.NpcConfig.IsPeace)
            {
                cfgId = "basic_unit_peace";
            }
            if(!string.IsNullOrEmpty(npcOwner.NpcConfig.AiBrainId))
            {
                cfgId = npcOwner.NpcConfig.AiBrainId;
            }
            Config = AIBrainParamsConfigLoader.Load(cfgId);

            InitializeStates();
        }

        /// <summary>
        /// 初始化状态
        /// </summary>
        protected virtual void InitializeStates()
        {
            // 初始化状态
            StateIdle = new AIStateIdle(this);
            StateCombat = new AIStateCombat(this);
            StateReturn = new AIStateReturn(this);
            StateFlee = new AIStateFlee(this);
            StateSearch = new AIStateSearch(this);
            StateAttracted = new AIStateAttracted(this);
            StateCharmedFollow = new AIStateCharmedFollow(this);

            if (Config.IsGuard)
            {
                StateChaseWanted = new AIStateChaseWanted(this);
            }

            ChangeState(StateIdle);
        }

        public void TriggerUpdateImmediately()
        {
            _lastBrainUpdate = 0;
        }

        public void ResetBrain()
        {
            // 重置
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void Tick(float dt)
        {
            if(NpcEntity.LogicManager.IsDialogPlayering)
            {
                return;
            }

            if (LogicTime.time - _lastBrainUpdate > ActionsFrequency)
            {
                CurrentState?.OnUpdate();
                _lastBrainUpdate = LogicTime.time;


                if (CurrentState == StateIdle)
                {
                    if (NpcEntity.CheckHasBuff("social_charmed"))
                    {
                        ChangeState(StateCharmedFollow);
                    }
                }
            }

            
        }

        public void ChangeState(AIBaseState newState)
        {
            if (_isChangingState) return;
            if (CurrentState == newState) return;

            _isChangingState = true;

            CurrentState?.OnExit();
            CurrentState = newState;
            CurrentState?.OnEnter();

            _isChangingState = false;
        }

        public bool CharmedTrigger;

        public bool AttractTrigger;
        public class AttrctInfo
        {
            public float HappenTime;
            public Vector2 HappenPos;
            public int AttractLevel;
            public long AttractSrcId;
        }
        public AttrctInfo? LatestAttrctInfo;
        public void AddAttractInfo(Vector2 attractPos, int attractLevel, long attractSrcId)
        {
            if(LatestAttrctInfo != null)
            {
                if(attractLevel < LatestAttrctInfo.AttractLevel)
                {
                    Debug.Log("AddAttractInfo attract level not bigger");
                    return;
                }
            }
            this.AttractTrigger = true;

            var attractInfo = new AttrctInfo();
            attractInfo.HappenTime = LogicTime.time;
            attractInfo.HappenPos = attractPos;
            attractInfo.AttractLevel = attractLevel;
            attractInfo.AttractSrcId = attractSrcId;

            LatestAttrctInfo = attractInfo;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int GetAreaWantedVal()
        {
            return LogicManager.WantedManager.CurrentWantedVal;
        }
    }

    // --- 状态基类 ---
    public abstract class AIBaseState
    {
        public abstract string StateName { get; }
        protected AIBrainV2 _brain;
        protected float startTime;
        protected float Duration => LogicTime.time - startTime;

        public virtual bool CanBeAttract { get { return false; } }

        public AIBaseState(AIBrainV2 brain) { _brain = brain; }

        public virtual void OnEnter() { startTime = Time.time; }

        public void Update()
        {
            OnUpdate();

            if(CanBeAttract)
            {
                if(_brain.AttractTrigger)
                {
                    _brain.AttractTrigger = false;

                    _brain.ChangeState(_brain.StateAttracted);
                }
            }

            // 只要能上到charm 就能程序
            if (_brain.CharmedTrigger)
            {
                _brain.CharmedTrigger = false;

                _brain.ChangeState(_brain.StateCharmedFollow);
            }

            if(CanKaiYou())
            {
                //// 条件满足时执行揩油
                //if (_brain.LatestAttrctInfo.AttractLevel >= 2 && _brain.NpcEntity.abilityController.IsActionable())
                //{
                //    if (attractSource is PlayerLogicEntity playerEntity && !playerEntity.CheckHasState(AttrIdConsts.ImmumeKaiYou))
                //    {
                //        var diff = playerEntity.Pos - _brain.NpcEntity.Pos;
                //        if (diff.magnitude < 0.8f)
                //        {
                //            _brain.NpcEntity.abilityController.TryUseAbility("close_kaiyou", target: playerEntity);
                //        }
                //    }
                //}
            }
        }


        public abstract void OnUpdate();
        public virtual void OnExit() { }
        public virtual void OnFixedUpdate() { }

        public virtual bool CanKaiYou()
        {
            return false;
        }
    }
}


