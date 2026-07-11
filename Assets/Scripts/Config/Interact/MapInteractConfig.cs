
using System;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;
using UnityEngine.UIElements;
using static My.Config.LogicInteractOutput;

namespace My.Config
{

    

    [Serializable]
    public class LogicInteractOutput
    {
        public enum EOutputType
        {
            Invalid = 0,
            ChangeSelfStatus,
            FinishTask,
            GiveItems,
            CostItems,
            Teleport,
            OpenPanel,


            ActivateEventGroup,
            // Param3=目标命名点；GhostOrb 过渡，时长见 PlayerRelocateTimings
            RelocateGhostOrb = 8,
            // Param3=目标命名点；WaitOnly 默认（无过渡表现），时长见 PlayerRelocateTimings
            RelocateWaitOnly = 9,

            StartStealth,

            SetLocalSwitch,
            UnsetLocalSwitch,
            SetGlobalSwitch,

            OpenDialog,

            SelfAnim,
            Wait,

            StartRetreat,
            TriggerSpawner,

            NextDayPeriod,
            MarkCharacterValue, // 标记人物变量

            // Param1 = 持续秒数（0=直到 LMB 成功施放一次）；Param3 = skillId；一次性消失请配合 ChangeSelfStatus
            GrantTempSkill,

            // Param3 = rune_id
            GrantRune,

            AddBuff,

            // Param1=(int)EFactionId；Param2=1 保持战斗状态；TargetType=StaticName，StaticName=刷新点 uniq 名
            ChangeUnitFaction,

            // Param3=藤顶命名点 Param4=落点命名点；各段时长见 PlayerRelocateTimings
            RelocateVineClimb = 50,

            // Param3=起跳命名点 Param4=落点命名点；各段时长见 PlayerRelocateTimings
            RelocateFakeJump2D = 51,

            // Param1=持续ms Param2=可视半径(0=默认28) Param3=命名点(空则用 Target 位置)
            // TargetType=StaticName 时 Pin 远端交互点 Presenter
            ShowCameraOverride = 60,

            EGMemberChangeState = 100,
            EGMemberActivate = 101,
        }

        public EOutputType OutputType;
        public long Param1;
        public long Param2;
        public string Param3;
        public string Param4;
        public long Param5;
        public long Param6;

        public float DelayTime;

        public enum ETargetType
        {
            Default,
            GroupMember,
            StaticName,
            DynamicEntity,
        }
        public ETargetType TargetType;
        public int MemberId;
        public string StaticName;
        public string DynamicEntityVariable;
    }

    [Serializable]
    public class InteractCheckCond
    {
        public enum ECheckType
        {
            None = 0,
            NotHide,
            HasLocalSwitch,
            NoLocalSwitch,

            PlayerNotRetreating,

            // Param3 = skill_id；玩家当前已持有该临时技能时不可交互
            PlayerNotHoldTempSkill,

            // Param1 = count, Param2 = 0 by attach_id / 1 by attach_type, Param3 = attach id or type
            PlayerAttachCountAtLeast,

            PlayerHasBuff,

            PlayerNoBuff,
        }

        public ECheckType CheckType;
        public long Param1;
        public long Param2;
        public string Param3;
        public string Param4;
    }


    [Serializable]
    public class MapInteractInfo
    {
        // 所有交互放一起
        public int InteractId;
        public string Label; // 选项
        public string UnLabel; // 灰色选项
        public bool HideWhenFail = true;
        public float NeedDist = 0.4f;
        public bool Passive = false;

        public List<CommonCheckCond> CheckCommonCond = new();
        public List<InteractCheckCond> CheckInteractCond = new();
        public List<LogicInteractOutput> Outputs = new();
    }
}


