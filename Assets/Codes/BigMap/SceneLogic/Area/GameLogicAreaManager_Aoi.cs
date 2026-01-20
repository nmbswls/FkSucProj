

using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Logic
{

    public class UniformGridIndex<TKey> where TKey : IEquatable<TKey>
    {
        private readonly float cellSize;
        private readonly Dictionary<(int x, int y), List<TKey>> cellToIds = new();
        private readonly Dictionary<TKey, (int x, int y)> idToCell = new();

        public UniformGridIndex(float cellSize) { this.cellSize = Mathf.Max(1f, cellSize); }

        public static (int x, int y) PosToCell(Vector2 p, float cellSize)
        {
            int x = Mathf.FloorToInt(p.x / cellSize);
            int y = Mathf.FloorToInt(p.y / cellSize);
            return (x, y);
        }

        public void AddOrMove(TKey id, Vector2 pos)
        {
            var cell = PosToCell(pos, cellSize);
            if (idToCell.TryGetValue(id, out var old) && old.Equals(cell)) return;

            if (idToCell.TryGetValue(id, out var oldCell))
            {
                if (cellToIds.TryGetValue(oldCell, out var lst))
                    lst.Remove(id);
            }

            idToCell[id] = cell;
            if (!cellToIds.TryGetValue(cell, out var list))
                cellToIds[cell] = list = new List<TKey>(8);
            if (!list.Contains(id)) list.Add(id);
        }

        public void Remove(TKey id)
        {
            if (idToCell.TryGetValue(id, out var cell))
            {
                if (cellToIds.TryGetValue(cell, out var lst)) lst.Remove(id);
                idToCell.Remove(id);
            }
        }

        // 简易范围查询（方形近似）
        public void Query(Vector2 center, float radius, List<TKey> result)
        {
            // 如果 result 为 null 或不安全，最好做防御性检查
            if (result == null) return;
            result.Clear();

            // 1. 计算查询范围的 AABB (Min Max)
            // 这样做能精准覆盖所有可能涉及的格子，无论 center 在格子的哪个位置
            Vector2 minPos = center - new Vector2(radius, radius);
            Vector2 maxPos = center + new Vector2(radius, radius);

            // 2. 将 AABB 转换为格子坐标范围
            var minCell = PosToCell(minPos, cellSize);
            var maxCell = PosToCell(maxPos, cellSize);

            // 3. 遍历这个矩形范围内的所有格子
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    if (cellToIds.TryGetValue((x, y), out var lst))
                    {
                        foreach (var id in lst)
                        {
                            result.Add(id);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            cellToIds.Clear();
            idToCell.Clear();
        }
    }


    // InterestPoint：兴趣点（玩家、本地AI、相机锚点等）
    public class InterestPoint
    {
        public int Id;            // 唯一ID
        public Func<Vector3> Pos; // 实时位置获取委托
        public float LogicRadius; // 逻辑活跃半径（进入即唤醒）
        public float WarmupRadius;// 预热半径（在更远处预加载，进入Active半径更近）
    }


    public partial class GameLogicAreaManager
    {
        /// <summary>
        /// 生命周期 
        /// </summary>
        public enum LogicLifeState
        {
            NotLoaded,   // 未加载，仅有Record 对于任意，
            Warmup,      // 预热加载中（Spawn中/完成但未Wake）
            Active,      // 完全活跃
            Cooldown,    // 冷却计时（离开后延迟降级）
            Sleep        // 休眠（轻量态） 维持低频
        }


        [Serializable]
        public class Settings
        {
            public float WarmupToActiveRadiusBias = -10f; // 例：在距兴趣点半径减10m时转Active
            public float ExitCooldown = 2.0f;             // 离开后保持Active的时间
            public float SleepToDespawnDelay = 8.0f;      // 休眠保持多久后可真正卸载（可选）
            public int MaxSpawnPerFrame = 8;              // 每帧最大重建数
            public int MaxDespawnPerFrame = 6;            // 每帧最大卸载数
            public int MaxWakePerFrame = 16;              // 每帧最大Wake数
            public int MaxSleepPerFrame = 16;             // 每帧最大Sleep数
        }

        // 实体运行态
        private class OneEntityRuntimeState
        {
            public long Id;
            public LogicLifeState State;
            public float Timer;            // 冷却/延迟计时
            public int InterestRefCount;   // 当前落入任一兴趣半径的引用计数
            public bool NearAnyWarmup;     // 是否落入任一Warmup半径
            public float ForceActiveUntil;

            public bool IsMarkDestroy;        // 运行时死亡标记
            public float DeathRemainTimer;    // 尸体/残留计时
        }

        private readonly Dictionary<long, OneEntityRuntimeState> runtimeStates = new();
        private readonly Dictionary<int, InterestPoint> interestPoints = new();

        // 工作队列（限流）
        private readonly Queue<long> spawnEntityQ = new();
        private readonly Queue<long> despawnEntityQ = new();
        private readonly Queue<long> wakeEntityQ = new();
        private readonly Queue<long> sleepEntityQ = new();
        //private readonly Queue<(long, int)> destroyEntityQ = new();
        private readonly Queue<(long, string)> corpseCleanupQ = new();

        // 复用容器
        private readonly List<long> queryBufInt = new(256);

        private readonly HashSet<long> _frameWarmIdSet = new();
        private readonly HashSet<long> _frameActiveIdSet = new();

        private readonly HashSet<long> _frameAffected = new();

        private void TickEntityLifeCycle(float dt)
        {
            // 重新评估AOI：计算每个实体与兴趣点关系（按区域近似）
            _frameWarmIdSet.Clear();
            _frameActiveIdSet.Clear();

            foreach (var ip in interestPoints.Values)
            {
                // 预热查询
                UnitGridIndex.Query(ip.Pos(), ip.WarmupRadius, queryBufInt);
                foreach (var id in queryBufInt) 
                {
                    _frameWarmIdSet.Add(id);
                }

                // Active半径（可用Bias或单独半径）
                float activeR = Mathf.Max(0.1f, ip.LogicRadius);
                UnitGridIndex.Query(ip.Pos(), activeR, queryBufInt);
                foreach (var id in queryBufInt)
                {
                    _frameActiveIdSet.Add(id);
                }
            }

            // 2) 根据集合更新每个实体状态
            // 为了避免遍历全库，这里仅对“受影响集合”以及“已加载/已有状态”的实体进行处理。
            // 简易实现：合并三类集合
            _frameAffected.Clear();
            foreach (var id in _frameWarmIdSet) _frameAffected.Add(id);
            foreach (var id in _frameActiveIdSet) _frameAffected.Add(id);
            foreach (var id in runtimeStates.Keys) _frameAffected.Add(id);

            foreach (var id in _frameAffected)
            {
                bool inWarm = _frameWarmIdSet.Contains(id);
                bool inActive = _frameActiveIdSet.Contains(id);

                if (!runtimeStates.TryGetValue(id, out var st))
                {
                    st = new OneEntityRuntimeState { Id = id, State = LogicLifeState.NotLoaded };
                    runtimeStates[id] = st;
                }

                // 记录兴趣关系
                st.NearAnyWarmup = inWarm;
                st.InterestRefCount = inActive ? 1 : 0;

                StepStateMachine(st, dt);
            }

            //// 处理死亡状态
            //ProcessDieQueue();

            // 处理其他加载卸载等
            ProcessQueues(dt);

            // 处理尸体回收
            ProcessCorpse(dt);
        }


        /// <summary>
        /// 更新实体位置
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="newPos"></param>
        public void UpdatePosition(long entityId, Vector2 newPos)
        {
            UnitGridIndex.AddOrMove(entityId, newPos);
        }
    }
}

