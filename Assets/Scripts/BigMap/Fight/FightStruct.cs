
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
            /// <summary>施法者周围半径内最近敌对单位（用于抛物弹等索敌）</summary>
            NearestEnemyInRadius,
            /// <summary>抛物弹制导到 castVec 落点（Point 施法）</summary>
            CastPoint,
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

            Nonlethal = 1 << 5,

            Loss = 1 << 6,

            // H 命中接触部位（供 H 类伤害读取玩家部位 HStrength / Endurance）
            HitPart_Mouth = 1 << 8,
            HitPart_Breast = 1 << 9,
            HitPart_Womb = 1 << 10,
            HitPart_Tail = 1 << 11,
            HitPart_Wing = 1 << 12,
            HitPart_Skin = 1 << 13,
        }

        public const EDmgFlag HitPartMask =
            EDmgFlag.HitPart_Mouth | EDmgFlag.HitPart_Breast | EDmgFlag.HitPart_Womb |
            EDmgFlag.HitPart_Tail | EDmgFlag.HitPart_Wing | EDmgFlag.HitPart_Skin;

        public static EDmgFlag ToHitPartFlag(cfg.demo.EBodyPart part)
        {
            return part switch
            {
                cfg.demo.EBodyPart.Mouth => EDmgFlag.HitPart_Mouth,
                cfg.demo.EBodyPart.Breast => EDmgFlag.HitPart_Breast,
                cfg.demo.EBodyPart.Womb => EDmgFlag.HitPart_Womb,
                cfg.demo.EBodyPart.Tail => EDmgFlag.HitPart_Tail,
                cfg.demo.EBodyPart.Wing => EDmgFlag.HitPart_Wing,
                cfg.demo.EBodyPart.Skin => EDmgFlag.HitPart_Skin,
                _ => EDmgFlag.None,
            };
        }

        public static bool TryGetHitPart(EDmgFlag flags, out cfg.demo.EBodyPart part)
        {
            part = cfg.demo.EBodyPart.None;
            var masked = flags & HitPartMask;
            if (masked == EDmgFlag.None)
            {
                return false;
            }

            // 多 flag 时按部位枚举优先级取最低位
            if ((masked & EDmgFlag.HitPart_Mouth) != 0) { part = cfg.demo.EBodyPart.Mouth; return true; }
            if ((masked & EDmgFlag.HitPart_Breast) != 0) { part = cfg.demo.EBodyPart.Breast; return true; }
            if ((masked & EDmgFlag.HitPart_Womb) != 0) { part = cfg.demo.EBodyPart.Womb; return true; }
            if ((masked & EDmgFlag.HitPart_Tail) != 0) { part = cfg.demo.EBodyPart.Tail; return true; }
            if ((masked & EDmgFlag.HitPart_Wing) != 0) { part = cfg.demo.EBodyPart.Wing; return true; }
            if ((masked & EDmgFlag.HitPart_Skin) != 0) { part = cfg.demo.EBodyPart.Skin; return true; }
            return false;
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
            Die = 7,
            System = 8,
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