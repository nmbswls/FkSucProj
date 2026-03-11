
using System;
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Fight
{
    public class FightStruct
    {
        public enum EShapeType
        {
            None,
            Square,
            Circle,
            Sector,
        }

        [Serializable]
        public class Shape
        {
            public EShapeType Type = EShapeType.None;
            public float Length;
            public float Width;
            public float Radius;
            public float Angle;
        }

        /// <summary>
        /// 目标选择策略
        /// </summary>
        public enum ETargetSelectPolicy
        {
            None,
            PrimaryTarget,
            Self,
            LowHpAlly,
            LowHpEnmity,
            Random,
        }

        /// <summary>
        /// 
        /// </summary>
        [Flags]
        public enum EDmgFlag
        {
            None,
            ZiWei = 0x08,
            Xixue = 1 << 4,
        }

        public enum EInterruptSource
        {
            None = 0,
            Hit = 1,
            Stun = 2,
            Move = 3,
            Cast = 4,
            Dodge = 5,
            InputCancel = 6,
            System = 7,
        }

        public struct InterruptRequest
        {
            public EInterruptSource source;
            public int priority;     // 来源优先级（例如：Stun=100, Hit=50, InputCancel=30）
            public object payload;   // 可选：时长、方向、效果ID等
        }

        [Serializable]
        public class HitResult
        {
            public bool IgnoreHit;
            public float KnockForce;

            [SerializeReference]
            public List<MapFightEffectCfg> OnHitEffects = new();
        }

    }

}