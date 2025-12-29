
using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Quest
{
    //public enum RouteMode { Default, FirstMatch, AllOf, AnyOf, PlayerChoice }

    public enum ConditionType { Kill, Collect, Interact, Area, TimeLimit, HasState, And, Or, Not }

    //[Serializable]
    //public class QuestOutput
    //{ }

    [Serializable]
    public class ConditionData
    {
        public ConditionType type;
        public ConditionData[] children;

        public int id;           // monsterId, itemId, areaId, objectId, stateKey
        public int count;
        public float duration;   // seconds
        public bool negate;
    }

    [Serializable]
    public class ObjectiveData
    {
        [Tooltip("UI显示的描述")]
        public string text;

        [Tooltip("达成条件")]
        public ConditionData condition;

        [Tooltip("是否初始隐藏")]
        public bool isHidden;

        [Tooltip("是否可选")]
        public bool isOption;

        [Header("标签系统")]
        [Tooltip("当此目标完成时，给任务实例打上这些标签 (Internal Tags)")]
        public string[] completionTags;
    }



    [Serializable]
    public class StepOutcomeData
    {
        public string outcomeName;    // 比如 "正面突击"
        public string description;    // UI描述 "击杀守卫"

        public int[] NeedObjectiveIds;
        public int nextStepId;     // 达成后跳转的ID列表
    }


    [Serializable]
    public class QuestStepData
    {
        public int stepId;
        public bool isRoot;           // 是否是起始步骤

        [Header("完成路径 (任意一个达成即完成)")]
        public StepOutcomeData[] outcomes;

        [Header("目标")]
        public ObjectiveData[] objectives;

        [Header("失败条件 (可选)")]
        public ConditionData failCondition;
    }

    [CreateAssetMenu(menuName = "Quest/Quest Data")]
    public class QuestData : ScriptableObject
    {
        public int questId;
        public string title;
        [TextArea] public string description;

        public QuestStepData[] steps;

        // 辅助方法：构建ID索引
        public Dictionary<int, QuestStepData> BuildStepMap()
        {
            var map = new Dictionary<int, QuestStepData>();
            foreach (var s in steps) map[s.stepId] = s;
            return map;
        }
    }

    //public class RuntimeCondition
    //{
    //    public long LocalProgress;
    //    public ConditionData cacheConfig;


    //}



    //public sealed class StepRuntime
    //{
    //    public readonly QuestStepData data;
    //    private readonly ICondition enter;
    //    private readonly ICondition complete;
    //    private readonly ICondition fail;
    //    private readonly ICondition routeConditionCached; // 可选缓存（仅示例对FirstMatch/Default使用一个条件）

    //    public StepRuntime(QuestStepData data)
    //    {
    //        this.data = data;
    //        enter = data.enterCondition != null ? ConditionFactory.Create(data.enterCondition) : null;
    //        complete = data.completeCondition != null ? ConditionFactory.Create(data.completeCondition) : null;
    //        fail = data.failCondition != null ? ConditionFactory.Create(data.failCondition) : null;

    //        // 简化示例：若第一条路由有条件，可缓存避免每次ResolveNext创建
    //        if (data.nextRoutes != null && data.nextRoutes.Length > 0 && data.nextRoutes[0].routeCondition != null)
    //        {
    //            routeConditionCached = ConditionFactory.Create(data.nextRoutes[0].routeCondition);
    //        }
    //    }

    //    public bool CanEnter(QuestContext ctx) => enter == null || enter.Evaluate(ctx);

    //    public void Enter(QuestContext ctx)
    //    {
    //        enter?.Reset();
    //        complete?.Reset();
    //        fail?.Reset();

    //        // 通知计时类条件开始（通过StateChanged事件）
    //        ctx.Bus.Publish(new GameEvent { Type = GameEventType.StateChanged, Id = StateIds.StepEnter }, ctx);

    //        SubscribeConditions(ctx);
    //    }

    //    private void SubscribeConditions(QuestContext ctx)
    //    {
    //        if (complete is IEventSubscriber es1) ctx.Bus.Subscribe(es1);
    //        if (fail is IEventSubscriber es2) ctx.Bus.Subscribe(es2);
    //        // enter一般不订阅事件
    //    }

    //    public void Exit(QuestContext ctx)
    //    {
    //        if (complete is IEventSubscriber es1) ctx.Bus.Unsubscribe(es1);
    //        if (fail is IEventSubscriber es2) ctx.Bus.Unsubscribe(es2);
    //    }

    //    public bool IsCompleted(QuestContext ctx) => complete == null || complete.Evaluate(ctx);
    //    public bool IsFailed(QuestContext ctx) => fail != null && fail.Evaluate(ctx);

    //    // 路由解析：写入外部列表，避免分配
    //    public void ResolveNext(List<StepRuntime> outList, Dictionary<int, StepRuntime> directory, QuestContext ctx)
    //    {
    //        if (data.nextRoutes == null || data.nextRoutes.Length == 0) return;

    //        for (int i = 0; i < data.nextRoutes.Length; i++)
    //        {
    //            var route = data.nextRoutes[i];
    //            bool match = route.mode == RouteMode.Default;

    //            // 优先使用缓存路由条件，未缓存则直接Evaluate（会创建实例，建议你扩展为缓存全部）
    //            if (route.routeCondition != null)
    //            {
    //                var cond = (i == 0 && routeConditionCached != null) ? routeConditionCached : ConditionFactory.Create(route.routeCondition);
    //                match = cond.Evaluate(ctx);
    //            }

    //            if (!match) continue;

    //            var ids = route.nextStepIds;
    //            if (ids != null)
    //            {
    //                for (int j = 0; j < ids.Length; j++)
    //                {
    //                    if (directory.TryGetValue(ids[j], out var next))
    //                    {
    //                        outList.Add(next);
    //                    }
    //                }
    //            }

    //            if (route.mode == RouteMode.FirstMatch) break;
    //        }
    //    }
    //}



    //public sealed class QuestInstance
    //{
    //    public readonly QuestData Data;
    //    public QuestStatus Status { get; private set; } = QuestStatus.Inactive;

    //    private readonly QuestContext ctx;
    //    private readonly EventBus bus;

    //    private readonly Dictionary<int, StepRuntime> stepDir = new Dictionary<int, StepRuntime>(64);
    //    private readonly List<StepRuntime> activeSteps = new List<StepRuntime>(16);

    //    private readonly List<StepRuntime> completedQueue = new List<StepRuntime>(8);
    //    private readonly List<StepRuntime> failedQueue = new List<StepRuntime>(4);

    //    private readonly ICondition[] abortConds;
    //    private bool abortFlag;

    //    public QuestInstance(QuestData data, QuestContext ctx)
    //    {
    //        Data = data;
    //        this.ctx = ctx;
    //        this.bus = ctx.Bus;

    //        BuildStepsAndAbort();
    //    }

    //    private void BuildStepsAndAbort()
    //    {
    //        if (Data.steps != null)
    //        {
    //            for (int i = 0; i < Data.steps.Length; i++)
    //            {
    //                var rt = new StepRuntime(Data.steps[i]);
    //                stepDir[Data.steps[i].stepId] = rt;
    //            }
    //        }

    //        if (Data.abortConditions != null && Data.abortConditions.Length > 0)
    //        {
    //            abortConds = new ICondition[Data.abortConditions.Length];
    //            for (int i = 0; i < Data.abortConditions.Length; i++)
    //            {
    //                abortConds[i] = ConditionFactory.Create(Data.abortConditions[i]);
    //            }
    //        }
    //        else
    //        {
    //            abortConds = Array.Empty<ICondition>();
    //        }
    //    }

    //    public void Start()
    //    {
    //        if (Status != QuestStatus.Inactive) return;
    //        Status = QuestStatus.Active;

    //        ActivateInitialSteps();
    //        for (int i = 0; i < activeSteps.Count; i++)
    //        {
    //            activeSteps[i].Enter(ctx);
    //        }
    //    }

    //    private void ActivateInitialSteps()
    //    {
    //        foreach (var kv in stepDir)
    //        {
    //            var s = kv.Value;
    //            if (s.data.isRoot && s.CanEnter(ctx))
    //            {
    //                ActivateStep(s);
    //            }
    //        }
    //    }

    //    private void ActivateStep(StepRuntime step)
    //    {
    //        if (!activeSteps.Contains(step))
    //        {
    //            activeSteps.Add(step);
    //        }
    //    }

    //    // 外部驱动：在MonoBehaviour的Update中发布Timer事件，在LateUpdate中调用LateTick
    //    public void LateTick()
    //    {
    //        if (Status != QuestStatus.Active) return;

    //        // 全局中断优先
    //        abortFlag = CheckAbort();
    //        if (abortFlag) { Abort(); return; }

    //        failedQueue.Clear();
    //        completedQueue.Clear();

    //        // 先判失败
    //        for (int i = 0; i < activeSteps.Count; i++)
    //        {
    //            var step = activeSteps[i];
    //            if (step.IsFailed(ctx))
    //            {
    //                failedQueue.Add(step);
    //            }
    //        }
    //        if (failedQueue.Count > 0) { Fail(); return; }

    //        // 再判完成
    //        for (int i = 0; i < activeSteps.Count; i++)
    //        {
    //            var step = activeSteps[i];
    //            if (step.IsCompleted(ctx))
    //            {
    //                completedQueue.Add(step);
    //            }
    //        }

    //        // 统一处理完成
    //        for (int i = 0; i < completedQueue.Count; i++)
    //        {
    //            var s = completedQueue[i];
    //            s.Exit(ctx);
    //            activeSteps.Remove(s);

    //            var nextBuffer = ListPool<StepRuntime>.Get();
    //            try
    //            {
    //                s.ResolveNext(nextBuffer, stepDir, ctx);
    //                for (int n = 0; n < nextBuffer.Count; n++)
    //                {
    //                    var next = nextBuffer[n];
    //                    ActivateStep(next);
    //                    next.Enter(ctx);
    //                }
    //            }
    //            finally
    //            {
    //                ListPool<StepRuntime>.Release(nextBuffer);
    //            }
    //        }

    //        // 终止判定（根据你的设计：全部完成或到达终止状态）
    //        if (activeSteps.Count == 0)
    //        {
    //            Complete();
    //        }
    //    }

    //    private bool CheckAbort()
    //    {
    //        for (int i = 0; i < abortConds.Length; i++)
    //        {
    //            if (abortConds[i].Evaluate(ctx)) return true;
    //        }
    //        return false;
    //    }

    //    private void Abort()
    //    {
    //        if (Status != QuestStatus.Active) return;
    //        Status = QuestStatus.Aborted;
    //        Cleanup();
    //    }

    //    private void Fail()
    //    {
    //        if (Status != QuestStatus.Active) return;
    //        Status = QuestStatus.Failed;
    //        Cleanup();
    //    }

    //    private void Complete()
    //    {
    //        if (Status != QuestStatus.Active) return;
    //        Status = QuestStatus.Completed;
    //        // 发放奖励：可在外部通过 Data.reward 处理
    //        Cleanup();
    //    }

    //    private void Cleanup()
    //    {
    //        for (int i = 0; i < activeSteps.Count; i++)
    //        {
    //            activeSteps[i].Exit(ctx);
    //        }
    //        activeSteps.Clear();
    //    }
    //}
}