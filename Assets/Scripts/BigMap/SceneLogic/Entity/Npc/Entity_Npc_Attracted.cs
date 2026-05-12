
using My.Map.Entity;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace My.Map
{
    /// <summary>
    /// 吸引源类型
    /// </summary>
    public enum ENpcAttractSrcType
    {
        Inlivad,
        PointOnce, // 坐标点单次吸引
        Player, // 某个单位 可能是玩家或召唤物
        PlayerMist, // 玩家留下的迷雾
        SrcEntity,
    }

    public class NpcAttrctedInfo
    {
        public ENpcAttractSrcType SrcType;

        public float HappenTime;
        public Vector2 HappenPos;
        public int AttractPower;

        public long AttractSrcId;
    }

    public partial class NpcUnitLogicEntity
    {
        public NpcAttrctedInfo? LatestAttrctInfo;
        public void ApplyAttracted(ENpcAttractSrcType srcType, int attractPower, Vector2 attractPos, long attractSrcId)
        {
            if (LatestAttrctInfo != null)
            {
                if (attractPower < LatestAttrctInfo.AttractPower)
                {
                    Debug.Log("AddAttractInfo attract level not bigger");
                    return;
                }
            }

            var attractInfo = new NpcAttrctedInfo();
            attractInfo.SrcType = srcType;
            attractInfo.HappenTime = LogicTime.time;
            attractInfo.HappenPos = attractPos;
            attractInfo.AttractPower = attractPower;
            attractInfo.AttractSrcId = attractSrcId;

            LatestAttrctInfo = attractInfo;

            if (AIBrain != null && AIBrain.CurrentState.CanBeAttract)
            {
                AIBrain.AttractTrigger = true;
            }
        }

        /// <summary>
        /// 检查应用被mist吸引
        /// </summary>
        private void TickPlayerMist()
        {
            // 检查是否由
            if(CheckHasState(AttrIdConsts.DesireMistAffected))
            {
                long attractPower = 1;
                if(LatestAttrctInfo != null && LatestAttrctInfo.AttractPower >= attractPower)
                {
                    return;
                }

                ApplyAttracted(ENpcAttractSrcType.PlayerMist, 1, this.Pos,  0);
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