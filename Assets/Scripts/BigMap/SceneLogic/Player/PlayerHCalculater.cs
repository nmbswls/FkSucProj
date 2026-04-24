
using System;
using Map.Logic.Events;
using My.Map.Entity;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{

    public static class PlayerHCalculater
    {
        public static bool CheckHForceDownSuccess(GameLogicManager logicManager, NpcUnitLogicEntity npcEntity)
        {
            var playerPhysicalForm = logicManager.playerLogicEntity.GetAttr(AttrIdConsts.PhysicalForm);
            var targetPhysicalForm = npcEntity.GetAttr(AttrIdConsts.PhysicalForm);

            if (playerPhysicalForm < 5) playerPhysicalForm = 5;
            if (targetPhysicalForm < 5) targetPhysicalForm = 5;

            var playerCharm = logicManager.playerLogicEntity.GetAttr(AttrIdConsts.PlayerCharm);
            var enemyWill = npcEntity.GetAttr(AttrIdConsts.Will);

            double charmModifier = Math.Max(0, (playerCharm - enemyWill) * 1.0 / (playerCharm + enemyWill) * 0.3);

            double baseP = (playerPhysicalForm * (1 + charmModifier))/ (playerPhysicalForm * (1 + charmModifier) + targetPhysicalForm);
            if (npcEntity.CheckHasState(AttrIdConsts.Charmed))
            {
                baseP += 0.2f;
            }

            bool backHit = false;
            do
            {
                Vector2 to = npcEntity.Pos - logicManager.playerLogicEntity.Pos;
                if (to.magnitude > 1.0f) break;
                float angle = Vector2.SignedAngle(logicManager.playerLogicEntity.FinalLook, to);
                float fov = 240;
                if (Mathf.Abs(angle) > fov * 0.5f) break;
                backHit = true;
            }
            while (false);

            if (backHit)
            {
                baseP += 0.2f;
            }

            var randVal = UnityEngine.Random.Range(0, 10000);
            if(randVal < (int)(baseP * 10000))
            {
                // 显示层事件
                logicManager.playerLogicEntity.viewer.ShowFakeFxEffect("成功", logicManager.playerLogicEntity.Pos);
                logicManager.playerLogicEntity.abilityController.TryUseAbility("zhaqu", target: npcEntity);
            }
            else
            {
                // 显示层事件
                MainGameManager.Instance.ShowFakeFxEffect("失败", logicManager.playerLogicEntity.Pos);
                logicManager.playerLogicEntity.abilityController.TryUseAbility("zhaqu", target: npcEntity);
            }

            return true;
        }
    }

}