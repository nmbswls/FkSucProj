

using System;
using System.Collections.Generic;
using Config;
using Config.Unit;
using My.UI;
using Newtonsoft.Json;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Windows;
using static My.Map.Entity.EntitySkillComboGraph;
using static My.Map.Entity.MapEntitySkillManager;
using static My.Map.MapControlAction;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.VolumeComponent;

namespace My.Map.Entity
{

    //public class InputBuffer
    //{
    //    private readonly Dictionary<ActionType, BufferedInput?> map = new();
    //    private readonly Func<ActionType, int> priorityOf;
    //    private readonly Func<ActionType, float> windowOf;
    //    private float lastInputTime = -999f;
    //    private float debounce;

    //    public InputBuffer(Func<ActionType, int> priorityOf, Func<ActionType, float> windowOf, float debounce)
    //    {
    //        this.priorityOf = priorityOf;
    //        this.windowOf = windowOf;
    //        this.debounce = debounce;
    //    }

    //    public void Write(ActionType type, Vector2 dir)
    //    {
    //        float now = Time.time;
    //        if (now - lastInputTime < debounce) return; // 去抖
    //        lastInputTime = now;
    //        map[type] = new BufferedInput
    //        {
    //            type = type,
    //            time = now,
    //            dir = dir,
    //            priority = priorityOf(type)
    //        };
    //    }

    //    public bool TryConsume(Func<ActionType, bool> isLegal, out BufferedInput consumed)
    //    {
    //        consumed = default;
    //        float now = Time.time;
    //        // 优先级排序
    //        List<BufferedInput> list = new List<BufferedInput>();
    //        foreach (var kv in map)
    //        {
    //            if (kv.Value.HasValue) list.Add(kv.Value.Value);
    //        }
    //        list.Sort((a, b) => b.priority.CompareTo(a.priority));

    //        foreach (var entry in list)
    //        {
    //            float win = windowOf(entry.type);
    //            if (now - entry.time <= win && isLegal(entry.type))
    //            {
    //                consumed = entry;
    //                map[entry.type] = null; // 单次消耗
    //                return true;
    //            }
    //        }

    //        // 清理过期
    //        foreach (var kv in new List<ActionType>(map.Keys))
    //        {
    //            var v = map[kv];
    //            if (v.HasValue && now - v.Value.time > windowOf(v.Value.type))
    //                map[kv] = null;
    //        }
    //        return false;
    //    }

    //    public void Clear(ActionType type)
    //    {
    //        if (map.ContainsKey(type)) map[type] = null;
    //    }
    //}


    /// <summary>
    /// 输出
    /// </summary>
    [Serializable]
    public struct SkillInput
    {
        public string SkillId;
        public Vector2 AppendDir;
        public float HappenTime;

        public SkillInput(string skillId, Vector2 appp, float happenTime)
        {
            SkillId = skillId;
            AppendDir = appp;
            HappenTime = happenTime;
        }

        public bool Matches(EntitySkillComboGraph.InputPattern pattern)
        {
            if (SkillId != pattern.SkillId) return false;
            return true;
        }
    }

    

    /// <summary>
    /// 运行时连招图
    /// </summary>
    [Serializable]
    public class EntitySkillComboGraph
    {

        /// <summary>
        /// 派生输入
        /// 输入类型由上层封装
        /// 可能是左键右键 也可能是入口技能名
        /// 例如技能test  可能路由出test_01 test_02等 但只需要传入test即可
        /// </summary>
        [Serializable]
        public struct InputPattern
        {
            public string SkillId;
            public bool NeedDir;

            public InputPattern(string skillId, bool needDir)
            {
                this.SkillId = skillId;
                this.NeedDir = needDir;
            }
        }


        // 时间标记：相对当前节点时间
        [Serializable]
        public struct TimeWindow
        {
            public float start; // 秒
            public float end;   // 秒

            public TimeWindow(float s, float e)
            {
                start = s; end = e;
            }

            public bool Contains(float t) => t >= start && t <= end;
        }

        // 派生窗口：支持命中确认锚点
        [Serializable]
        public class DeriveWindow
        {
            public string id;
            public TimeWindow window;       // 基于 nodeClock 的窗口
        }

        // 连招节点
        [Serializable]
        public class ComboNode
        {
            public int NodeId;
            public string AbilityId;
            public float ExpectedDuration = 0.5f; // 估计时长，仅用于窗口参考
            public List<DeriveWindow> deriveWindows = new List<DeriveWindow>();
        }

        // 转移：从一节点到另一节点
        [Serializable]
        public class Transition
        {
            public int fromNodeId;
            public int toNodeId;
            public InputPattern triggerInput;   // 必须匹配的输入
            public string windowId;             // 必须在对应窗口内
            public bool requireHitConfirm;      // 冗余保护
            public float scoreBias;             // AI评分加权（更优先的分支给更高分）
            public string label;                // 备注/策划名
        }

        public string Id;
        public List<Transition> Transitions = new();
        public List<ComboNode> ComboNodes = new();

        [JsonIgnore]
        private Dictionary<int, ComboNode> nodes = new Dictionary<int, ComboNode>();
        [JsonIgnore]
        private Dictionary<int, List<Transition>> transitionsFrom = new Dictionary<int, List<Transition>>();

        public void BuildGraph()
        {
            foreach (var n in ComboNodes)
            {
                nodes[n.NodeId] = n;
                if (!transitionsFrom.ContainsKey(n.NodeId)) transitionsFrom[n.NodeId] = new List<Transition>();
            }

            foreach (var t in Transitions)
            {
                if (!transitionsFrom.ContainsKey(t.fromNodeId))
                    transitionsFrom[t.fromNodeId] = new List<Transition>();
                transitionsFrom[t.fromNodeId].Add(t);
            }
        }
        public ComboNode GetNode(int id) => nodes.TryGetValue(id, out var n) ? n : null;
        public List<Transition> GetTransitions(int from) =>
            transitionsFrom.TryGetValue(from, out var list) ? list : new List<Transition>();
    }

    /// <summary>
    /// 专门用于编排
    /// </summary>
    public class ComboOrchestrator
    {
        public MapEntitySkillManager SkillManager;

        private readonly EntitySkillComboGraph _graph;
        private readonly ComboContext _ctx;

        // 运行态：输入缓冲与上下文
        public class ComboContext
        {
            public int currentNodeId;
            public float nodeClock;            // 当前节点运行时间
            public bool hitConfirmed;          // 是否命中确认
            public float lastHitTime;          // 命中时间（用于窗口）
            public float useSkillTime;
            public int maxBuffer = 6;

            public void ResetClock() => nodeClock = 0f;
            public void SetNode(int id)
            {
                currentNodeId = id;
                ResetClock();
                hitConfirmed = false;
                lastHitTime = -999f;
            }
        }

        public ComboOrchestrator(EntitySkillComboGraph graph)
        {
            this._graph = graph;
            _ctx = new();
        }

        public EntitySkillComboGraph Graph { get { return _graph; } }

        public ComboNode GetComboNode(int nodeId)
        {
            return _graph?.GetNode(nodeId) ?? null;
        }

        public void Tick(float dt)
        {
            if(_ctx.currentNodeId != 0)
            {
                _ctx.nodeClock += dt;
            }
        }

        public List<EntitySkillComboGraph.Transition> GetPossibleTransition()
        {
            if (_graph == null) return null;
            var nodeId = _ctx.currentNodeId;
            var node = _graph.GetNode(nodeId);
            if (node == null) return null;

            // 激活窗口
            var activeWindows = new Dictionary<string, EntitySkillComboGraph.DeriveWindow>();
            foreach (var w in node.deriveWindows)
            {
                if (w.window.Contains(_ctx.nodeClock))
                {
                    activeWindows[w.id] = w;
                }
            }
            if (activeWindows.Count == 0) return null;
            // 评分挑选
            var transitions = _graph.GetTransitions(nodeId);
            List<EntitySkillComboGraph.Transition> ret = new();

            foreach (var t in transitions)
            {
                if (!activeWindows.ContainsKey(t.windowId)) continue;
                if (t.requireHitConfirm && !_ctx.hitConfirmed) continue;
                ret.Add(t);
            }

            return ret;
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="input"></param>
        /// <param name="chosen"></param>
        /// <returns></returns>
        public bool TryTriggerCurrentCombo(SkillInput input, out EntitySkillComboGraph.Transition chosen)
        {
            chosen = null;
            if (_graph == null) return false;
            var nodeId = _ctx.currentNodeId;
            var node = _graph.GetNode(nodeId);

            // 尝试进行entry transition
            if (node == null)
            {
                return false;
            }

            // 激活窗口
            var activeWindows = new Dictionary<string, EntitySkillComboGraph.DeriveWindow>();
            foreach (var w in node.deriveWindows)
            {
                if (w.window.Contains(_ctx.nodeClock))
                {
                    activeWindows[w.id] = w;
                }
            }
            if (activeWindows.Count == 0) return false;

            // 评分挑选
            var transitions = _graph.GetTransitions(nodeId);
            float best = float.NegativeInfinity;
            foreach (var t in transitions)
            {
                if (!activeWindows.ContainsKey(t.windowId)) continue;
                if (t.requireHitConfirm && !_ctx.hitConfirmed) continue;
                if (!input.Matches(t.triggerInput)) continue;

                float score = t.scoreBias;
                if (score > best)
                {
                    best = score;
                    chosen = t;
                }
            }
            return chosen != null;
        }


        /// <summary>
        ///  
        /// </summary>
        /// <param name="input"></param>
        /// <param name="chosen"></param>
        /// <returns></returns>
        public bool TryTriggerEntryCombo(SkillInput input, out EntitySkillComboGraph.Transition chosen)
        {
            chosen = null;
            if (_graph == null) return false;
            var transitions = _graph.GetTransitions(0);
            float best = float.NegativeInfinity;
            foreach (var t in transitions)
            {
                if (t.requireHitConfirm && !_ctx.hitConfirmed) continue;
                if (!input.Matches(t.triggerInput)) continue;

                float score = t.scoreBias;
                if (score > best)
                {
                    best = score;
                    chosen = t;
                }
            }
            return chosen != null;
        }


        /// <summary>
        /// 获取入口真实combo节点
        /// </summary>
        /// <param name="input"></param>
        /// <param name="chosen"></param>
        /// <returns></returns>
        public EntitySkillComboGraph.ComboNode GetEntryComboNode(SkillInput input)
        {
            if (_graph == null) return null;
            var transitions = _graph.GetTransitions(0);
            float best = float.NegativeInfinity;
            EntitySkillComboGraph.Transition chosenTransition = null;
            foreach (var t in transitions)
            {
                if (t.requireHitConfirm && !_ctx.hitConfirmed) continue;
                if (!input.Matches(t.triggerInput)) continue;

                float score = t.scoreBias;
                if (score > best)
                {
                    best = score;
                    chosenTransition = t;
                }
            }

            var toNode = _graph.GetNode(chosenTransition.toNodeId);
            return toNode;
        }


        public void OnHitConfirm()
        {
            _ctx.hitConfirmed = true;
            _ctx.lastHitTime = Time.time; // 或者使用外部权威时钟
        }


        public void TransitCombo(int nextNodeId)
        {
            if (_graph == null) return;
            if (nextNodeId == 0)
            {
                _ctx.SetNode(0);

                // 重置
                _ctx.hitConfirmed = false;
                _ctx.lastHitTime = -999f;
                _ctx.useSkillTime = 0;
            }
            else
            {
                var toNode = _graph.GetNode(nextNodeId);
                if (toNode == null) return;

                //_callbacks.OnNodeExit(_ctx.currentNodeId);
                _ctx.SetNode(nextNodeId);
                //_callbacks.OnNodeEnter(t.toNodeId);

                // 重置
                _ctx.hitConfirmed = false;
                _ctx.lastHitTime = -999f;
                _ctx.useSkillTime = LogicTime.time;
            }
        }

    }


    public class MapEntitySkillManager
    {

        public class SkillIntent
        {
            public string skillId;
            public Vector2? castVec = null;
            public ILogicEntity target = null;

            public float happenTime;
        }

        public BaseUnitLogicEntity OwnerEntity;
        public MapEntityAbilityExecutor Executor;
        public ComboOrchestrator comboOrchestrator;

        public class SkillRuntime
        {
            public string SkillName;
            public float lastUseTime;
            public float cooldown;
            public float stackCount;

            public EntitySkillCfg cacheConfig;
        }


        public Dictionary<string, SkillRuntime> SkillRuntimes = new();

        public string? CurrentSkillId = null;
        public string? CurrentAbilityId = null;


        public List<SkillIntent> inputBuffer = new List<SkillIntent>();


        public MapEntitySkillManager(BaseUnitLogicEntity ownerEntity, EntitySkillComboGraph comboGraph = null)
        {
            this.OwnerEntity = ownerEntity;

            this.comboOrchestrator = new(comboGraph);
            // 初始化comboOrchestrator
        }

        public bool RegisterSkill(string skillId)
        {
            var skillCfg = SkillLibrary.GetSkillConfig(skillId);
            if(skillCfg == null)
            {
                return false;
            }
            if (SkillRuntimes.TryGetValue(skillCfg.SkillId, out var state))
            {
                Debug.Log($"RegisterSkill duplicate {skillCfg.SkillId}");
                return false;
            }
            var newState = new SkillRuntime()
            {
                SkillName = skillId,
                cacheConfig = skillCfg
            };

            SkillRuntimes[newState.SkillName] = newState;
            return true;
        }

        public void Tick(float dt)
        {
            comboOrchestrator?.Tick(dt);

            foreach (var abState in SkillRuntimes.Values)
            {
                if (abState.cooldown > 0)
                {
                    abState.cooldown -= dt;
                }
            }

            // 清理
            if(!Executor.IsRunning)
            {
                CurrentSkillId = null;
                CurrentAbilityId = null;
            }

            // 如果可以使用技能 检查input buffer
            if (Executor.IsActionable())
            {
                if(inputBuffer.Count > 0)
                {
                    var lastInput = inputBuffer[inputBuffer.Count - 1];

                    do
                    {
                        var skillConf = SkillLibrary.GetSkillConfig(lastInput.skillId);
                        if (skillConf == null)
                        {
                            break;
                        }

                        float timeDiff = LogicTime.time - lastInput.happenTime;
                        if (timeDiff > skillConf.BufferCacheTime)
                        {
                            break;
                        }

                        UseSkill(lastInput.skillId, castVec:lastInput.castVec, target:lastInput.target);
                        break;
                    }
                    while (false);

                    inputBuffer.Clear();
                }
            }

        }
        /// <summary>
        /// 使用技能
        /// </summary>
        /// <param name="skillName"></param>
        /// <param name="castVec1"></param>
        /// <param name="castVec2"></param>
        /// <param name="targetId"></param>
        public bool UseSkill(string skillId, Vector2? castVec = null, ILogicEntity target = null)
        {
            SkillRuntimes.TryGetValue(skillId, out SkillRuntime skillRuntime);
            if(skillRuntime == null)
            {
                return false;
            }

            // 停止技能输入 
            if(OwnerEntity.CheckHasState(AttrIdConsts.ForbidSkillOp))
            {
                return false;
            }

            // 不可行动
            // 处理中断技能层级
            if (!Executor.IsActionable())
            {
                // 
                inputBuffer.Add(new SkillIntent()
                {
                    skillId = skillId,
                    castVec = castVec,
                    target = target,
                    happenTime = LogicTime.time,
                });

                return false;
            }

            string realAbilityId;
            EntitySkillComboGraph.Transition chosenTran = null;

            // 先检查是否能直接衔接combo
            if (comboOrchestrator.TryTriggerCurrentCombo(new SkillInput() { SkillId = skillId }, out chosenTran))
            {
                var nextNode = comboOrchestrator.GetComboNode(chosenTran.toNodeId);
                realAbilityId = nextNode.AbilityId;
            }
            // 再检查能否触发入口combo
            else if(comboOrchestrator.TryTriggerEntryCombo(new SkillInput() { SkillId = skillId }, out chosenTran))
            {
                var nextNode = comboOrchestrator.GetComboNode(chosenTran.toNodeId);
                realAbilityId = nextNode.AbilityId;
                if (!IsSkillReady(skillId))
                {
                    return false;
                }
            }
            // 非combo类技能 直接执行
            else
            {
                realAbilityId = skillRuntime.cacheConfig.MainAbilityId;
                if (!IsSkillReady(skillId))
                {
                    return false;
                }
            }

            if (!Executor.TryUseAbility(realAbilityId, castDir: castVec, target: target))
            {
                Debug.Log("UseSkill fail");
                comboOrchestrator.TransitCombo(0);
                return false;
            }

            // 执行combo状态更新
            // todo 如果技能打断连击 则需要在这里置空
            if(chosenTran != null)
            {
                comboOrchestrator.TransitCombo(chosenTran.toNodeId);
            }
            else
            {
                // 打断连招的技能 需要重置
                if(skillRuntime.cacheConfig.InterruptCombo)
                {
                    comboOrchestrator.TransitCombo(0);
                }
            }

            // 保存当前技能
            CurrentSkillId = skillId;
            CurrentAbilityId = realAbilityId;

            // 冷却等情况
            if (skillRuntime.cacheConfig.CoolDown > 0)
            {
                skillRuntime.cooldown = skillRuntime.cacheConfig.CoolDown;
            }
            skillRuntime.lastUseTime = LogicTime.time;

            return true;
        }

        public void CheckSkillCanceled(string skillId)
        {
            if(CurrentSkillId == null || CurrentSkillId != skillId)
            {
                return;
            }

            if(!Executor.IsRunning)
            {
                return;
            }

            // 不一致
            if (Executor.CurrentCtx.AbilityConfig.Id != CurrentAbilityId)
            {
                return;
            }

            var phase = Executor.GetCurrentPhase();
            if (phase == null) return;
            if (phase.HoldingPhase)
            {
                Executor.CurrentCtx.PhaseMarkSkip = true;
            }
        }

        /// <summary>
        /// 持续按键
        /// </summary>
        /// <param name="skillId"></param>
        public void TrySkillHold(string skillId)
        {

            if (CurrentSkillId == null || CurrentSkillId != skillId)
            {
                return;
            }

            if (!Executor.IsRunning)
            {
                return;
            }

            var phase = Executor.GetCurrentPhase();
            if (phase == null) return;
            if (phase.HoldingPhase)
            {
                Executor.CurrentCtx.LastPhaseHoldTime = LogicTime.time;
            }
        }

        /// <summary>
        /// 获取当前准备好的主动技能
        /// </summary>
        /// <returns></returns>
        public bool CheckAnyReadySkill()
        {
            foreach (var abName in SkillRuntimes.Keys)
            {
                if (!IsSkillReady(abName))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前准备好的主动技能
        /// </summary>
        /// <returns></returns>
        public List<SkillRuntime> GetAllReadySkills()
        {
            List<SkillRuntime> ret = new();
            foreach (var abName in SkillRuntimes.Keys)
            {
                if (!IsSkillReady(abName))
                {
                    continue;
                }

                ret.Add(SkillRuntimes[abName]);
            }

            return ret;
        }

        /// <summary>
        /// 获取当前准备好的主动技能
        /// </summary>
        /// <returns></returns>
        public bool IsSkillReady(string skillName)
        {
            SkillRuntimes.TryGetValue(skillName, out var skillRuntime);
            if (skillRuntime == null)
            {
                return false;
            }

            if (skillRuntime.cacheConfig.IsPassive)
            {
                return false;
            }

            if (skillRuntime.cooldown > 0)
            {
                return false;
            }

            if (!skillRuntime.cacheConfig.NeedHMode)
            {
                //if (OwnerEntity.IsHMode)
                //{
                //    return false;
                //}
            }
            else
            {
                //if (!OwnerEntity.IsHMode)
                //{
                //    return false;
                //}
            }

            return true;
        }

        

        ///// <summary>
        ///// 检查技能是否可派生
        ///// </summary>
        ///// <param name="groupAbility"></param>
        //public (bool, string) GetCanDerive(string abilityName)
        //{


        //    return (false, null);
        //}


        //private ComboNode SelectNextComboNode(string abilityName)
        //{
        //    ComboRuntimes.TryGetValue(abilityName, out var comboRuntime);
        //    var info = _comboDict[abilityName];

        //    if (comboRuntime.currentIndex < 0) 
        //        return info.NodeLinks[info.startIndex];

        //    // 命中分支与索引推进的综合选择
        //    var nextIndex = Math.Min(comboRuntime.currentIndex + 1, info.NodeLinks.Count - 1);
        //    var candidate = info.NodeLinks[nextIndex];

        //    bool canUse = true;
        //    do
        //    {
        //        if(candidate.HitRequired && !comboRuntime.lastHit)
        //        {
        //            canUse = false;
        //            Debug.Log("SelectNextNode not hit");
        //            break;
        //        }

        //        if (candidate.ComboWindow > 0)
        //        {
        //            if(LogicTime.time - comboRuntime.lastUseTime > candidate.ComboWindow)
        //            {
        //                canUse = false;
        //                Debug.Log("过期了");
        //                break;
        //            }
        //        }
        //    }
        //    while (false);
        //    if (canUse)
        //        return candidate;

        //    return info.NodeLinks[info.startIndex];
        //}

        //private void Reset(string comboName) 
        //{
        //    ComboRuntimes.TryGetValue(comboName, out var comboRuntime);
        //    if(comboRuntime != null)
        //    {
        //        comboRuntime.currentIndex = -1; /* 清状态与token */
        //    }
        //}
    }

}