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


    [Serializable]
    public class PhaseEffectEvent
    {
        public PhaseEventKind Kind = PhaseEventKind.Timed;
        public float TimeOffset = 0f;         // 仅当 Kind==Timed 生效（相对阶段开始）
        [SerializeReference]
        public MapFightEffectCfg Effect;                // 具体效果
        public int Repeat = 0;                // 可选：重复次数
        public float RepeatInterval = 0f;     // 可选：重复间隔（适合持续伤害/持续采样）
    }

    [System.Flags]
    public enum EAbilityInterruptMask
    {
        None = 0,
        Hit = 1 << 0,
        Move = 1 << 3,
        NewAbility = 1 << 5,
    }

    [Serializable]
    public class MapPreviewIntent
    {
        public Vector2 FaceOffset = Vector2.zero;
        public FightStruct.Shape ShapeInfo;
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

        public bool ShowRangePreview = false;
        public MapPreviewIntent PreviewIntent = new();
    }


    [CreateAssetMenu(menuName = "GP/Ability/Action")]
    [Serializable]
    public class MapAbilitySpecConfig : ScriptableObject
    {
        public string Id;
        public AbilityTypeTag TypeTag = AbilityTypeTag.Combat;

        public float MaxStepDistance = 0.0f;

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
        

        

        // ai 相关
        

        public bool CauseAttract = false;
        public float AttractPower = 0;
        public float AttractRange = 2.0f;
    }
}
