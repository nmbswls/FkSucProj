using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using My.Map.Fight;
using UnityEngine;



namespace My.Map.Entity
{
    // ===== 基础标签与枚举 =====
    public enum AbilityTypeTag 
    { 
        Combat, 
        Interaction, 
        ItemUse, 
        Crafting,
        HMode,
        Utility,
    }

    //[Flags]
    //public enum ConcurrencyPolicy { Reject = 01, Replace, Stack }


    public enum EOneVariatyType
    {
        Invalid,
        Int,
        Long,
        Float,
        String,
    }

    public enum PhaseEventKind { OnEnter, OnExit, Timed } // Timed 为相对阶段时间的触发点


    [Serializable]
    public struct OneVariaty
    {
        public EOneVariatyType ValType;
        public string RawVal;
        public string ReferName;
    }

    public enum EPhaseEffectFlag
    {
        None,
        PlayerHunterMode,
        PlayerNonHunterMode,
    }

    /// <summary>
    /// 阶段吸附垫步：继承技能根配置，或使用本 phase 独立配置
    /// </summary>
    public enum EPhaseStepSnapSource
    {
        InheritFromAbility,
        PhaseCustom,
    }

    [Serializable]
    public class PhaseEffectEvent
    {
        public PhaseEventKind Kind = PhaseEventKind.Timed;
        public float TimeOffset = 0f;         // 仅当 Kind==Timed 生效（相对阶段开始）
        [SerializeReference]
        public MapFightEffectCfg Effect;                // 具体效果
        public int Repeat = 0;                // 可选：重复次数
        public float RepeatInterval = 0f;     // 可选：重复间隔（适合持续伤害/持续采样）

        public string CheckNeedBuff;
        public string CheckNoBuff;
    }

    

    [Serializable]
    public class MapPreviewIntent
    {
        public Vector2 FaceOffset = Vector2.zero;
        public FightStruct.Shape ShapeInfo;
    }

    public enum EAbilityInterruptMask
    {
        None = 0,
        Hit = 1 << 0, // 普通受击
        Move = 1 << 1, // 普通受击
        Cancel = 1 << 2, // 普通取消
        Cast = 1 << 3,
    }


    [Serializable]
    public class MapAbilityPhase
    {
        public string PhaseName;

        public OneVariaty DurationValue;
        public bool HoldingPhase; // 持续施法的phase不会自然结束
        
        public string AnimTag; // 可用于驱动动画状态


        public EAbilityInterruptMask InterruptMask; // 自定义Flags
        public bool ForbidDodge = false;

        public bool WithProgress = false;
        public bool LockMovement = false;
        public bool LockRotation = false;
        public bool ImmuneKnock = false;


        public List<PhaseEffectEvent> Events = new();  // 该阶段内的所有效果与时序

        public string EnterDebugString = string.Empty;
        public List<string> PhaseBuff = new();
        public bool EnableVariablePhaseBuff;

        public bool ShowRangePreview = false;
        public MapPreviewIntent PreviewIntent = new();

        public string UsePhaseHitAsTarget; // 特殊属性 如果非空 则进入当前phase时 使用特定phase的击中目标当作新技能target

        [Header("阶段开始吸附（垫步/拉向目标）")]
        public EPhaseStepSnapSource StepSnapSource = EPhaseStepSnapSource.InheritFromAbility;
        public float DefaultStepDistance = 0f;
        public bool AddTargetCorrection = false;
        public float MaxCorrectionValue = 0.5f;
        public float GoodCorrectionnDist = 0.8f;
    }


    [CreateAssetMenu(menuName = "GP/Ability/Action")]
    [Serializable]
    public class MapAbilitySpecConfig : ScriptableObject
    {
        public string Id;

        /// <summary>
        /// 主tag
        /// </summary>
        public AbilityTypeTag TypeTag = AbilityTypeTag.Combat;


        // 阶段定义
        public List<MapAbilityPhase> Phases = new List<MapAbilityPhase>();

        // 效果
        [SerializeReference]
        public List<MapFightEffectCfg> OnStartEffects = new();
        [SerializeReference]
        public List<MapFightEffectCfg> OnCompleteEffects = new();
        [SerializeReference]
        public List<MapFightEffectCfg> OnCancelEffects = new();

        // 变量集合
        public Dictionary<string, string> Variables = new();

       
        public string AbilityTag;

        public bool IsDodge;
        public bool AdjustFaceDir;

        // ai 相关
        

        public bool CauseAttract = false;
        public float AttractPower = 0;
        public float AttractRange = 2.0f;

        public enum ECastType
        {
            NoTarget,
            Point,
            Circle,
            Directional,
            LockTarget,
            ToFace, // 会根据面向伪造一个施法参数
        }
        public ECastType CastType;
        public float Range1;
        public float Range2;


        /// <summary>
        /// 施法吸附（技能级默认；各 phase 在 StepSnapSource==InheritFromAbility 时沿用此处）
        /// </summary>
        public float DefaultStepDistance = 0.0f;
        public bool AddTargetCorrection = false; // 是否增加吸附
        public float MaxCorrectionValue = 0.5f;
        public float GoodCorrectionnDist = 0.8f;

        /// <summary>
        /// 选择策略
        /// </summary>
        public FightStruct.ETargetSelectPolicy TargetSelectPolicy = FightStruct.ETargetSelectPolicy.None;


        public float DesiredUseAngle = 5;
        public float DesiredUseDistance = 1.0f;
    }
}
