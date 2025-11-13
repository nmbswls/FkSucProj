using Config;
using Map.Logic.Events;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

namespace My.Map.Entity
{
    [Flags]
    public enum EFactionId
    { 
        None = 0,
        Player,
        Ally,
        Citizen,
        Beast,
        Bandit,
        HSprite,
    }

    public enum ERelationType
    {
        Neutral,
        Enemy,
        Friend,
    }

    public class EFactionRelationInfo
    {
        public EFactionId Left;
        public EFactionId Right;
        public ERelationType RelationType;
    }

    /// <summary>
    /// 阵营关系组件 （废除）
    /// 阵营关系除了玩家之间大部分都是固定的 只有友好 中立 敌对三种矩阵
    /// 玩家
    /// 
    /// </summary>
    public class FactionRelationManager
    {

        private static List<EFactionRelationInfo> relationInfos = new()
        {
            new EFactionRelationInfo()
            {
                Left = EFactionId.Citizen,
                Right = EFactionId.Beast,
            }
        };


        protected Dictionary<EFactionId, Dictionary<EFactionId, EFactionRelationInfo>> RuntimeFactionInfo { get; set; } = new();



        public ERelationType GetFactionRelation(EFactionId selfFaction, EFactionId targetFaction)
        {
            return ERelationType.Neutral;
        }
    }

}

