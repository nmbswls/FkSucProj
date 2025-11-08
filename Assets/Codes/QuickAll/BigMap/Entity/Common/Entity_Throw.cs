using Map.Logic.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static MapEntityAbilityController;



namespace Map.Entity.Throw
{
    public interface IThrowTarget
    {
        long Id { get; }

        Vector2 Pos { get; }
        bool CanBeThrow();

        void OnBeingThrowStart();

        void OnBeingThrowInterrupt();
    }

    public interface IThrowLauncher
    {
        long Id { get; }

        Vector2 Pos { get; }

        void OnThrownInterrupt();

        void OnThrowStart();
    }


    public class GlobalThrowManager
    {
        public GameLogicManager logicManager;
        public int MaxTriggerDepthPerFrame = 6;

        public static long ThrowCtxInstIdCounter = 1000;

        public class ThrowContext
        {
            public long CtxId;
            public IThrowLauncher throwLauncher;
            public IThrowTarget throwTarget;
            public string srcAbilityId;
            public float throwStartTime;
            public float throwDuration;
            public int Priority;
            public EAbilityInterruptMask InterruptMask;
            public List<long> throwBuffIds;
        }

        private Dictionary<long, ThrowContext> ContextContainer = new();

        private Dictionary<long, long> target2ContextMap = new();
        private Dictionary<long, long> launcher2ContextMap = new();

        public GlobalThrowManager(GameLogicManager logicManager)
        {
            this.logicManager = logicManager;
        }

        public void Tick(float now, float dt)
        {
            TickRunningCtx(now, dt);
        }

        private float tickTimer;

        public void TickRunningCtx(float now, float dt)
        {
            tickTimer -= dt;
            if (tickTimer > 0) return;
            tickTimer = 0.3f;
            foreach (var ctxKey in ContextContainer.Keys.ToList())
            {
                var ctx = ContextContainer[ctxKey];
                logicManager.viewer.ShowFakeFxEffect("fcked", ctx.throwTarget.Pos);
                logicManager.viewer.ShowFakeFxEffect("fcking", ctx.throwLauncher.Pos);
                if (now > ctx.throwStartTime + ctx.throwDuration)
                {
                    ContextContainer.Remove(ctxKey);
                }
            }
        }

        public bool TryLaunchThrow(IThrowLauncher launcher, IThrowTarget target, string srcAbilityId, float duration, string efffectBuffId, int priority = 1)
        {

            if(launcher2ContextMap.TryGetValue(launcher.Id, out var launcherOldCtxId))
            {
                if(ContextContainer.TryGetValue(launcherOldCtxId, out var launcherOldCtx))
                {
                    Debug.LogError("cant throw when throwing " + launcher.Id);
                    return false;
                }
                else
                {
                    Debug.LogError("interrupt status error wrong state");
                    launcher2ContextMap.Remove(launcher.Id);
                }
            }

            if(target2ContextMap.TryGetValue(target.Id, out var targetOldCtxId))
            {
                if (ContextContainer.TryGetValue(targetOldCtxId, out var targetOldCtx))
                {
                    if(priority <= targetOldCtx.Priority)
                    {
                        Debug.Log("target is being throw no bigger prioty");
                        return false;
                    }

                    CleanOneThrowContext(targetOldCtx);
                }
                else
                {
                    Debug.LogError("interrupt status error wrong state");
                    launcher2ContextMap.Remove(launcher.Id);
                }
            }

            // open context
            var newCtx = new ThrowContext();
            newCtx.CtxId = ThrowCtxInstIdCounter++;
            newCtx.throwLauncher = launcher;
            newCtx.throwTarget = target;

            newCtx.srcAbilityId = srcAbilityId;
            newCtx.Priority = priority;
            newCtx.throwStartTime = Time.time;
            newCtx.throwDuration = 2f;

            // Ôö¼Ó
            launcher.OnThrowStart();
            target.OnBeingThrowStart();

            var id1 = logicManager.globalBuffManager.AddBuff(launcher.Id, "lock_move");
            var id2 = logicManager.globalBuffManager.AddBuff(target.Id, "lock_move");
            var id3 = logicManager.globalBuffManager.AddBuff(target.Id, efffectBuffId);

            newCtx.throwBuffIds.Add(id1);
            newCtx.throwBuffIds.Add(id2);
            newCtx.throwBuffIds.Add(id3);

            return true;
        }

        private void CleanOneThrowContext(ThrowContext ctx)
        {
            // clean 
            var oldlauncher = ctx.throwLauncher;
            var oldTarget = ctx.throwTarget;

            oldlauncher.OnThrownInterrupt();
            oldTarget.OnBeingThrowInterrupt();

            foreach(var buffId in ctx.throwBuffIds)
            {
                logicManager.globalBuffManager.RequestRemoveBuff(null, buffId);
            }

            target2ContextMap.Remove(oldTarget.Id);
            launcher2ContextMap.Remove(oldlauncher.Id);

            ContextContainer.Remove(ctx.CtxId);
        }
    }
}
