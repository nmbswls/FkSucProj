using System;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Entity.AI
{
    // 读档恢复用；None 表示默认 Idle/Sentry 初始化
    public enum EAIBrainPersistState
    {
        None = 0,
        Sentry = 1,
        Combat = 2,
        Return = 3,
        Flee = 4,
        Search = 5,
        ChaseWanted = 6,
        Attracted = 7,
        CharmedFollow = 8,
        PoisonBait = 9,
        HKnockdownFollowup = 10,
    }

    [Serializable]
    public class NpcBrainThreatPersist
    {
        public long EntityId;
        public float TotalDamage;
    }
}
