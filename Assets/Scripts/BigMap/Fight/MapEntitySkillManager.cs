

using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using cfg.demo;
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

    public class SkillRuntime
    {
        public string SkillName;
        public float lastUseTime;
        public float cooldown;
        public float stackCount;

        public EntitySkillData cacheConfig;

        // 施法用可变副本；表数据为 Luban 行，勿写入 AbilityExtra 列表
        public Dictionary<string, string> RuntimeAbilityExtraVariables;

        // 被动 Buff 层：当变量字典无合法整数时使用；合法值 >=1，并受 Buff MaxStackLayer 限制
        public int PassiveBuffLayer = 1;

        // 被动技能绑定 Buff：RegisterSkill 时附加，UnregisterSkill 时按实例移除
        public long PassiveBuffBoundInstanceId;
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

        


        public Dictionary<string, SkillRuntime> SkillRuntimes = new();

        public string? CurrentSkillId = null;
        public string? CurrentAbilityId = null;


        public List<SkillIntent> inputBuffer = new List<SkillIntent>();

        // 单槽脱手：可行动时优先于 inputBuffer，仅 MainAbilityId、不走连招图
        class DetachedSkillPending
        {
            public string SkillId;
            public Vector2? InputVec;
            public Vector2? CastVec;
            public long TargetEntityId;
            public float EnqueueTime;
        }

        DetachedSkillPending _detachedPending;
        int _detachedExecutionDepth;

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
                cacheConfig = skillCfg,
                RuntimeAbilityExtraVariables = SkillLibrary.CloneAbilityExtraMap(skillCfg),
                PassiveBuffLayer = 1,
            };

            SkillRuntimes[newState.SkillName] = newState;
            TryAttachPassiveBuffForRuntime(newState);
            return true;
        }

        // 合并更新本实体已注册技能的 Ability 覆盖参数（不写回 Luban）
        public bool TryMergeSkillAbilityExtraVariables(string skillId, IReadOnlyDictionary<string, string> updates)
        {
            if (string.IsNullOrEmpty(skillId) || updates == null || updates.Count == 0)
            {
                return false;
            }

            if (!SkillRuntimes.TryGetValue(skillId, out var rt))
            {
                return false;
            }

            if (rt.RuntimeAbilityExtraVariables == null)
            {
                rt.RuntimeAbilityExtraVariables = SkillLibrary.CloneAbilityExtraMap(rt.cacheConfig);
            }

            foreach (var kv in updates)
            {
                if (string.IsNullOrEmpty(kv.Key))
                {
                    continue;
                }

                rt.RuntimeAbilityExtraVariables[kv.Key] = kv.Value;
            }

            if (rt.cacheConfig is { IsPassive: true } && !string.IsNullOrEmpty(rt.cacheConfig.PassiveBuffId))
            {
                TryAttachPassiveBuffForRuntime(rt);
            }

            return true;
        }

        // 检查已注册技能中的被动项，补挂缺失的 PassiveBuff（例如读档后 Buff 被清空）
        public void SyncPassiveBuffBindings()
        {
            foreach (var rt in SkillRuntimes.Values)
            {
                TryAttachPassiveBuffForRuntime(rt);
            }
        }

        public bool UnregisterSkill(string skillId)
        {
            if (!SkillRuntimes.TryGetValue(skillId, out var rt))
            {
                return false;
            }

            DetachPassiveBuffBinding(rt);
            SkillRuntimes.Remove(skillId);
            return true;
        }

        // 与外部「已拥有技能列表」对齐：多出的技能 Unregister（会解绑被动 Buff），缺少的 Register
        public void ReconcileRegisteredSkills(IReadOnlyCollection<string> desiredSkillIds)
        {
            var want = new HashSet<string>(StringComparer.Ordinal);
            if (desiredSkillIds != null)
            {
                foreach (var id in desiredSkillIds)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        want.Add(id);
                    }
                }
            }

            foreach (var key in SkillRuntimes.Keys.ToArray())
            {
                if (!want.Contains(key))
                {
                    UnregisterSkill(key);
                }
            }

            foreach (var id in want)
            {
                if (!SkillRuntimes.ContainsKey(id))
                {
                    RegisterSkill(id);
                }
            }

            SyncPassiveBuffBindings();
        }

        // 无「已学列表」场景下直接替换运行时技能（如 NPC）；先卸旧再挂新
        public bool TryReplaceRegisteredSkill(string oldSkillId, string newSkillId)
        {
            if (string.IsNullOrEmpty(oldSkillId) || string.IsNullOrEmpty(newSkillId))
            {
                return false;
            }

            if (string.Equals(oldSkillId, newSkillId, StringComparison.Ordinal))
            {
                return SkillRuntimes.ContainsKey(oldSkillId);
            }

            if (!SkillRuntimes.ContainsKey(oldSkillId))
            {
                return false;
            }

            if (SkillRuntimes.ContainsKey(newSkillId))
            {
                UnregisterSkill(oldSkillId);
                return true;
            }

            UnregisterSkill(oldSkillId);
            return RegisterSkill(newSkillId);
        }

        const string DefaultPassiveBuffLevelKey = "PassiveLevel";

        static string GetPassiveBuffLevelVariableKey(EntitySkillData cfg)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.PassiveBuffLevelVariableKey))
            {
                return DefaultPassiveBuffLevelKey;
            }

            return cfg.PassiveBuffLevelVariableKey;
        }

        int GetResolvedPassiveBuffLayer(SkillRuntime rt)
        {
            var cfg = rt.cacheConfig;
            if (cfg == null || string.IsNullOrEmpty(cfg.PassiveBuffId))
            {
                return 1;
            }

            string key = GetPassiveBuffLevelVariableKey(cfg);
            int layer = rt.PassiveBuffLayer;
            if (rt.RuntimeAbilityExtraVariables != null &&
                rt.RuntimeAbilityExtraVariables.TryGetValue(key, out var raw) &&
                int.TryParse(raw, out var parsed))
            {
                layer = parsed;
            }

            layer = Math.Max(1, layer);
            BuffDefinition def = BuffLibrary.GetBuffDefinition(cfg.PassiveBuffId);
            if (def != null && def.MaxStackLayer > 0)
            {
                layer = Math.Min(layer, def.MaxStackLayer);
            }

            return layer;
        }

        // 设置被动等级（Buff 层数）：同步 Runtime 字典并刷新已绑定的 Buff 实例；不配 PassiveBuffLevelVariableKey 时默认键 PassiveLevel
        public bool TrySetPassiveSkillBuffLayer(string skillId, int layer)
        {
            if (string.IsNullOrEmpty(skillId) || !SkillRuntimes.TryGetValue(skillId, out var rt))
            {
                return false;
            }

            var cfg = rt.cacheConfig;
            if (cfg == null || !cfg.IsPassive || string.IsNullOrEmpty(cfg.PassiveBuffId))
            {
                return false;
            }

            int clamped = Math.Max(1, layer);
            BuffDefinition def = BuffLibrary.GetBuffDefinition(cfg.PassiveBuffId);
            if (def != null && def.MaxStackLayer > 0)
            {
                clamped = Math.Min(clamped, def.MaxStackLayer);
            }

            rt.PassiveBuffLayer = clamped;
            if (rt.RuntimeAbilityExtraVariables == null)
            {
                rt.RuntimeAbilityExtraVariables = SkillLibrary.CloneAbilityExtraMap(cfg);
            }

            string key = GetPassiveBuffLevelVariableKey(cfg);
            rt.RuntimeAbilityExtraVariables[key] = clamped.ToString();
            TryAttachPassiveBuffForRuntime(rt);
            return true;
        }

        void TryAttachPassiveBuffForRuntime(SkillRuntime rt)
        {
            var cfg = rt.cacheConfig;
            if (cfg == null || !cfg.IsPassive)
            {
                return;
            }

            if (string.IsNullOrEmpty(cfg.PassiveBuffId))
            {
                Debug.LogWarning($"[Skill] Passive skill '{cfg.SkillId}' has empty PassiveBuffId.");
                return;
            }

            if (BuffLibrary.GetBuffDefinition(cfg.PassiveBuffId) == null)
            {
                Debug.LogWarning($"[Skill] Passive skill '{cfg.SkillId}' PassiveBuffId '{cfg.PassiveBuffId}' not found in BuffLibrary.");
                return;
            }

            if (OwnerEntity == null)
            {
                return;
            }

            int wantLayer = GetResolvedPassiveBuffLayer(rt);

            if (rt.PassiveBuffBoundInstanceId != 0
                && OwnerEntity.BuffContainer.TryGetValue(rt.PassiveBuffBoundInstanceId, out var boundInst)
                && boundInst != null
                && boundInst.BuffId == cfg.PassiveBuffId)
            {
                if (boundInst.Layer != wantLayer)
                {
                    boundInst.SetBuffLayerDirect(wantLayer);
                }

                return;
            }

            if (OwnerEntity.BuffManager.CheckHasBuff(OwnerEntity.Id, cfg.PassiveBuffId))
            {
                BuffInstance chosen = null;
                foreach (var kv in OwnerEntity.BuffContainer)
                {
                    if (kv.Value == null || kv.Value.BuffId != cfg.PassiveBuffId)
                    {
                        continue;
                    }

                    if (kv.Value.CasterId == OwnerEntity.Id)
                    {
                        chosen = kv.Value;
                        break;
                    }

                    chosen ??= kv.Value;
                }

                if (chosen != null)
                {
                    rt.PassiveBuffBoundInstanceId = chosen.InstanceId;
                    if (chosen.Layer != wantLayer)
                    {
                        chosen.SetBuffLayerDirect(wantLayer);
                    }

                    return;
                }
            }

            rt.PassiveBuffBoundInstanceId = OwnerEntity.BuffManager.AddBuff(
                OwnerEntity.Id,
                cfg.PassiveBuffId,
                layer: wantLayer,
                overrideDuration: -1,
                casterId: OwnerEntity.Id);
        }

        void DetachPassiveBuffBinding(SkillRuntime rt)
        {
            if (OwnerEntity == null || rt.PassiveBuffBoundInstanceId == 0)
            {
                rt.PassiveBuffBoundInstanceId = 0;
                return;
            }

            OwnerEntity.BuffManager.RequestRemoveBuff(OwnerEntity, rt.PassiveBuffBoundInstanceId);
            rt.PassiveBuffBoundInstanceId = 0;
        }

        // 入队脱手施法（单槽覆盖）；正在执行脱手技时拒绝嵌套入队
        public bool EnqueueDetachedSkill(string skillId, Vector2? inputVec = null, Vector2? castVec = null, ILogicEntity target = null)
        {
            if (_detachedExecutionDepth > 0)
            {
                Debug.Log($"EnqueueDetachedSkill skipped (nested): {skillId}");
                return false;
            }

            if (string.IsNullOrEmpty(skillId) || Executor == null || !SkillRuntimes.ContainsKey(skillId))
            {
                return false;
            }

            _detachedPending = new DetachedSkillPending
            {
                SkillId = skillId,
                InputVec = inputVec,
                CastVec = castVec,
                TargetEntityId = target?.Id ?? 0,
                EnqueueTime = LogicTime.time,
            };
            return true;
        }

        // 丢弃未执行的脱手意图（如死亡、切场景）
        public void ClearDetachedSkillIntent()
        {
            _detachedPending = null;
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

            // 如果可以使用技能：先消费脱手队列，再处理 input buffer
            if (Executor.IsActionable())
            {
                if (_detachedPending != null)
                {
                    var pending = _detachedPending;
                    _detachedPending = null;

                    ILogicEntity targetEntity = null;
                    if (pending.TargetEntityId != 0)
                    {
                        targetEntity = OwnerEntity.LogicManager.GetLogicEntity(pending.TargetEntityId, ensureExist: false);
                    }

                    _detachedExecutionDepth++;
                    try
                    {
                        if (!UseSkillDetached(pending.SkillId, pending.InputVec, pending.CastVec, targetEntity))
                        {
                            Debug.Log($"UseSkillDetached failed for {pending.SkillId} (cooldown / CanActiveUseSkill / TryUseAbility)");
                        }
                    }
                    finally
                    {
                        _detachedExecutionDepth--;
                    }
                }

                if (inputBuffer.Count > 0)
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
        public bool UseSkill(string skillId, Vector2? inputVec = null, Vector2 ? castVec = null, ILogicEntity target = null)
        {
            SkillRuntimes.TryGetValue(skillId, out SkillRuntime skillRuntime);
            if(skillRuntime == null)
            {
                return false;
            }


            // 不能放技能 
            if (!OwnerEntity.CanActiveUseSkill())
            {
                Debug.Log("use skill check fail.");
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

            if (!Executor.TryUseAbility(realAbilityId, inputVec: inputVec, castVec: castVec, target: target, overrideParams: skillRuntime.RuntimeAbilityExtraVariables ?? SkillLibrary.CloneAbilityExtraMap(skillRuntime.cacheConfig)))
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

        // 脱手施法：不打连招，只用 MainAbilityId；仅应在 IsActionable 时调用
        bool UseSkillDetached(string skillId, Vector2? inputVec, Vector2? castVec, ILogicEntity target)
        {
            if (!SkillRuntimes.TryGetValue(skillId, out var skillRuntime))
            {
                return false;
            }

            if (!OwnerEntity.CanActiveUseSkill())
            {
                return false;
            }

            if (!Executor.IsActionable())
            {
                return false;
            }

            comboOrchestrator?.TransitCombo(0);

            string realAbilityId = skillRuntime.cacheConfig.MainAbilityId;
            if (!IsSkillReady(skillId))
            {
                return false;
            }

            if (!Executor.TryUseAbility(realAbilityId,
                    inputVec: inputVec,
                    castVec: castVec,
                    target: target,
                    overrideParams: skillRuntime.RuntimeAbilityExtraVariables ?? SkillLibrary.CloneAbilityExtraMap(skillRuntime.cacheConfig)))
            {
                return false;
            }

            CurrentSkillId = skillId;
            CurrentAbilityId = realAbilityId;

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
        /// 持续按键
        /// </summary>
        /// <param name="skillId"></param>
        public void TrySkillHoldEnd(string skillId)
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
                Executor.CurrentCtx.LastPhaseHoldTime = 0;
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
            foreach (var skillId in SkillRuntimes.Keys)
            {
                if (!IsSkillReady(skillId))
                {
                    continue;
                }

                ret.Add(SkillRuntimes[skillId]);
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
                if (OwnerEntity.IsInHMode())
                {
                    return false;
                }
            }
            else
            {
                if (!OwnerEntity.IsInHMode())
                {
                    return false;
                }
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