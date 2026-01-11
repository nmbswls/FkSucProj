
using System;
using System.Collections.Generic;
using My.Map.Entity;
using UnityEngine;
using static My.Map.BaseUnitLogicEntity;
using static My.Map.NpcCombatStateComp;

namespace My.Map
{
    public partial class BaseUnitLogicEntity
    {
        public class StealthInfo
        {
            public long stealthId;
            public long hidePointId;
            public Dictionary<long, float> SeeUnits = new();
            public Vector2 beforePos;
        }
        public StealthInfo stealthInfo = new();
        public static string StealBuffId = "player_stealth";

        public event Action<long> EventOnStartStealth;
        public event Action<long> EventOnEndStealth;

        public long IIder = 100;

        /// <summary>
        /// 检查目标是否隐藏
        /// </summary>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public bool IsTargetInvisibleFromSelf(long targetId)
        {
            var targetEntity = LogicManager.GetLogicEntity(targetId, false) as BaseUnitLogicEntity;
            if (targetEntity == null)
            {
                return false;
            }
            if (targetEntity.CheckHasState(AttrIdConsts.Invisible))
            {
                return true;
            }

            if (targetEntity.IsStealthFrom(this.Id))
            {
                return true;
            }

            return false;
        }


        public bool IsInStealth()
        {
            if (stealthInfo.stealthId == 0) return false;
            return true;
        }


        public bool IsStealthFrom(long seeEntityId)
        {
            if (stealthInfo.stealthId == 0) return false;
            if (stealthInfo.SeeUnits.ContainsKey(seeEntityId)) return false;
            return true;
        }

        /// <summary>
        /// 开始躲藏至躲藏点
        /// </summary>
        public void StartStealth(long hidePointId, Vector2 stealthPos)
        {
            if(stealthInfo.stealthId != 0)
            {
                EndStealth();
            }
            stealthInfo.stealthId = IIder++;
            stealthInfo.SeeUnits.Clear();
            stealthInfo.beforePos = Pos;
            stealthInfo.hidePointId = hidePointId;

            // get units
            var list = LogicManager.visionSenser.OverlapCircleAllEntity(Pos, 8.0f, new EntityFilterParam()
            {
                CampFilterType = ECampFilterType.NotSelf,
                SelfCampId = FactionId,

                FilterParamLists = new() {  EEntityType.Npc }
            });

            foreach (var e in list)
            {
                if (e == null || e is not BaseUnitLogicEntity witness)
                {
                    continue;
                }

                // 检查是否可见
                if (!LogicManager.visionSenser.CanUnitSee(witness.Id, Id))
                {
                    continue;
                }

                // 维护可见性
                stealthInfo.SeeUnits.Add(e.Id, LogicTime.time);
            }

            // 增加buff
            LogicManager.globalBuffManager.RequestAddBuff(Id, StealBuffId);

            // 开始躲藏
            TeleportTo(stealthPos);

            EventOnStartStealth?.Invoke(hidePointId);
        }

        /// <summary>
        /// 结束躲藏
        /// </summary>
        public void EndStealth()
        {
            long stealEntityId = stealthInfo.hidePointId;
            stealthInfo.stealthId = 0;
            stealthInfo.SeeUnits.Clear();
            stealthInfo.hidePointId = 0;

            // 增加buff
            LogicManager.globalBuffManager.RemoveAllBuffById(Id, StealBuffId);
            TeleportTo(stealthInfo.beforePos);

            EventOnEndStealth?.Invoke(stealEntityId);
        }
    }
}