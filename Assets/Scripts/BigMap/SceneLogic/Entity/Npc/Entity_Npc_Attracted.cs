
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{

    // ============================================================
    // 扰动类型枚举
    // ============================================================
    public enum EStimulusType
    {
        Audio_Normal,     // 脚步声
        //Visual_Enemy,       // 直接看到敌人
        //Visual_Body,        // 看到尸体
        //Visual_Item,        // 看到可疑物品 (炸弹/血迹)
        Evil_Ability,

        Src_Entity,
        Player_Mist,
        Player_Attract,
    }

    // ============================================================
    // 环境发出的扰动事件 (由道具/玩家广播出来)
    // ============================================================
    public struct StimulusEvent
    {
        public Vector3 Position;        // 发生位置
        public float Priority;          // 注意力争夺力 (决定AI去哪)
        public float Agitation;         // 警戒累积量  (决定AI状态升级)
        public EStimulusType Type;       // 扰动类型    (决定合并与反应逻辑)
        public long SourceID;            // 来源物体ID  (防止跨物体错误聚类)

        public StimulusEvent(
            Vector3 position,
            float priority,
            float agitation,
            EStimulusType type,
            long sourceID = -1)
        {
            Position = position;
            Priority = priority;
            Agitation = agitation;
            Type = type;
            SourceID = sourceID;
        }
    }

    public class StimulusMemoryRecord
    {
        public Vector3 Position;
        public float BasePriority;
        public float Timestamp;
        public EStimulusType Type;
        public long SourceID;
        public bool HasBeenChecked;     // AI 已到达并确认安全

        public StimulusMemoryRecord(StimulusEvent evt)
        {
            Position = evt.Position;
            BasePriority = evt.Priority;
            Timestamp = LogicTime.time;
            Type = evt.Type;
            SourceID = evt.SourceID;
            HasBeenChecked = false;
        }

        StimulusMemoryRecord()
        {
        }

        public static StimulusMemoryRecord FromPersist(
            Vector3 position,
            float basePriority,
            float timestamp,
            EStimulusType type,
            long sourceId)
        {
            return new StimulusMemoryRecord
            {
                Position = position,
                BasePriority = basePriority,
                Timestamp = timestamp,
                Type = type,
                SourceID = sourceId,
                HasBeenChecked = false,
            };
        }

        // 随时间衰减后的实际优先级
        public float GetCurrentPriority(float currentTime, float decayRate)
        {
            float age = currentTime - Timestamp;
            return Mathf.Max(0f, BasePriority - age * decayRate);
        }
    }


    ///// <summary>
    ///// 吸引源类型
    ///// </summary>
    //public enum ENpcAttractSrcType
    //{
    //    Inlivad,
    //    PointOnce, // 坐标点单次吸引
    //    Player, // 某个单位 可能是玩家或召唤物
    //    PlayerMist, // 玩家留下的迷雾
    //    SrcEntity,
    //}

    //public class NpcAttrctedInfo
    //{
    //    public ENpcAttractSrcType SrcType;

    //    public float HappenTime;
    //    public Vector2 HappenPos;
    //    public int AttractPower;

    //    public long AttractSrcId;
    //}


    public partial class NpcUnitLogicEntity
    {
        //public NpcAttrctedInfo? LatestAttrctInfo;

        public float suspicionGauge = 0f;           // 当前警戒值
        public float suspicionDecayRate = 3f;       // 警戒值每秒自动回落速度

        public int maxMemories = 5;          // 记忆容量上限
        public float mergeRadius = 2.0f;       // 空间聚类半径
        public float priorityDecay = 2.0f;       // 注意力每秒衰减量

        private List<StimulusMemoryRecord> _activeMemories = new List<StimulusMemoryRecord>();

        public StimulusMemoryRecord CurrentFocus { get; private set; }

        public void RestorePersistedFocus(Vector3 position, float basePriority, float timestamp, EStimulusType type, long sourceId)
        {
            CurrentFocus = StimulusMemoryRecord.FromPersist(position, basePriority, timestamp, type, sourceId);
        }

        private void TickStimulusAttract()
        {
            TickStimulusMemory();
            SuspicionDecayTick();
        }


        private void TickStimulusMemory()
        {
            float currentTime = LogicTime.time;
            StimulusMemoryRecord bestMemory = null;
            float maxPriority = 0f;

            // 倒序遍历，方便安全地在循环中删除元素
            for (int i = _activeMemories.Count - 1; i >= 0; i--)
            {
                StimulusMemoryRecord memory = _activeMemories[i];

                // --- 垃圾回收条件 ---
                float currentPriority = memory.GetCurrentPriority(currentTime, priorityDecay);
                bool decayedOut = currentPriority <= 0f;
                bool alreadyChecked = memory.HasBeenChecked;

                if (decayedOut || alreadyChecked)
                {
                    _activeMemories.RemoveAt(i);
                    continue;
                }

                // --- 竞争注意力焦点 ---
                if (currentPriority > maxPriority)
                {
                    maxPriority = currentPriority;
                    bestMemory = memory;
                }
            }

            if(CurrentFocus != bestMemory)
            {
                CurrentFocus = bestMemory;

                if (AIBrain != null && AIBrain.CurrentState.CanBeAttract)
                {
                    AIBrain.AttractTrigger = true;
                }
            }
        }

        // list?


        public void OnReceiveStimulus(StimulusEvent evt)
        {
            // --- 数值管线：无条件更新警戒槽 ---
            suspicionGauge = Mathf.Clamp(suspicionGauge + evt.Agitation, 0f, 100f);

            // --- 记忆管线：带聚类的记忆写入 ---
            if (TryMergeWithExistingMemory(evt)) return;   // 聚类成功，不新增
            EnsureMemoryCapacity();                         // 容量检查，踢出最弱记忆
            _activeMemories.Add(new StimulusMemoryRecord(evt));     // 写入新记忆
        }

        private bool TryMergeWithExistingMemory(StimulusEvent evt)
        {
            foreach (var memory in _activeMemories)
            {
                bool isSameSource = (evt.SourceID != -1) && (evt.SourceID == memory.SourceID);
                bool isSameType = evt.Type == memory.Type;
                bool isCloseEnough = Vector3.Distance(evt.Position, memory.Position) < mergeRadius;

                // 合并条件：(同一物体 OR 同一类型) AND 距离相近
                if ((isSameSource || isSameType) && isCloseEnough)
                {
                    memory.Timestamp = LogicTime.time;
                    if (evt.Priority > memory.BasePriority)
                    {
                        memory.BasePriority = evt.Priority;
                        memory.Position = evt.Position;
                    }
                    return true;
                }
            }
            return false;
        }

        private void EnsureMemoryCapacity()
        {
            if (_activeMemories.Count < maxMemories) return;

            float currentTime = LogicTime.time;
            int weakestIndex = 0;
            float weakestPriority = float.MaxValue;

            for (int i = 0; i < _activeMemories.Count; i++)
            {
                float p = _activeMemories[i].GetCurrentPriority(currentTime, priorityDecay);
                if (p < weakestPriority)
                {
                    weakestPriority = p;
                    weakestIndex = i;
                }
            }

            _activeMemories.RemoveAt(weakestIndex);
        }

        /// <summary>
        /// Tick 二：警戒槽自动回落
        /// </summary>
        private void SuspicionDecayTick()
        {
            // 只有在没有活跃记忆时，警戒槽才自动回落
            if (_activeMemories.Count == 0)
            {
                suspicionGauge = Mathf.Max(0f, suspicionGauge - suspicionDecayRate * Time.deltaTime);
            }
        }


        //public void ApplyAttracted(ENpcAttractSrcType srcType, int attractPower, Vector2 attractPos, long attractSrcId)
        //{
        //    if (LatestAttrctInfo != null)
        //    {
        //        if (attractPower < LatestAttrctInfo.AttractPower)
        //        {
        //            Debug.Log("AddAttractInfo attract level not bigger");
        //            return;
        //        }
        //    }

        //    var attractInfo = new NpcAttrctedInfo();
        //    attractInfo.SrcType = srcType;
        //    attractInfo.HappenTime = LogicTime.time;
        //    attractInfo.HappenPos = attractPos;
        //    attractInfo.AttractPower = attractPower;
        //    attractInfo.AttractSrcId = attractSrcId;

        //    LatestAttrctInfo = attractInfo;

        //    if (AIBrain != null && AIBrain.CurrentState.CanBeAttract)
        //    {
        //        AIBrain.AttractTrigger = true;
        //    }
        //}

        /// <summary>
        /// 检查应用被mist吸引
        /// </summary>
        private void TickPlayerMist()
        {
            // 检查是否由
            if (CheckHasState(AttrIdConsts.DesireMistAffected))
            {
                long attractPower = 1;
                OnReceiveStimulus(new StimulusEvent()
                {
                    Position = this.Pos,
                    Priority = 10,
                    Agitation = 10,

                    Type = EStimulusType.Player_Mist,
                });

                //if (LatestAttrctInfo != null && LatestAttrctInfo.AttractPower >= attractPower)
                //{
                //    return;
                //}

                //ApplyAttracted(ENpcAttractSrcType.PlayerMist, 1, this.Pos,  0);
            }
        }

        /// <summary>
        /// 尝试进行社交魅惑
        /// </summary>
        /// <param name="srcPlayer"></param>
        public void ApplySocialCharmed(PlayerLogicEntity srcPlayer)
        {
            long rate10000 = PlayerGamePlayRule.GetCharmWillCompare(srcPlayer.GetAttr(AttrIdConsts.PlayerCharm), this.GetAttr(AttrIdConsts.Will));

            if(UnityEngine.Random.Range(0, 10000) < rate10000)
            {
                AIBrain.CharmedTrigger = true;
                LogicManager.globalBuffManager.AddBuff(this.Id, "social_charmed", overrideDuration: 15.0f, casterId: srcPlayer.Id);
            }
            else
            {
                LogicManager.viewer.ShowFakeFxEffect("失败", this.Pos);
            }
        }
    }
}