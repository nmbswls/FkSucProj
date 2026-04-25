using System.Collections.Generic;
using My;
using My.Map.Entity;
using My.UI;
using UnityEngine;

namespace My.Map.Scene
{
    // 玩家搬运 NPC 尸体 / 昏迷单位的状态管理（静态单例逻辑，无需 MonoBehaviour）
    public static class PlayerNpcCarryService
    {
        // 放下时搜索参数
        public const float PutDownSearchRadius  = 2.2f;
        public const float PutDownClearanceRadius = 0.28f;

        // 当前被搬运单位的实体 Id；0 表示未搬运
        public static long CarriedNpcEntityId { get; private set; }

        public static bool IsCarrying => CarriedNpcEntityId != 0;

        // 由 SceneNpcPresenter.TriggerInteract 调用：先播放交互动画，再真正进入搬运状态
        public static void TryStartCarryInteract(NpcUnitLogicEntity npc)
        {
            if (npc == null || npc.MarkDestroyed)
            {
                return;
            }

            if (!npc.IsDead && !npc.MarkUnsensored)
            {
                return;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            if (IsCarrying)
            {
                MainGameManager.Instance.ShowFakeFxEffect("Already carrying", player.Pos);
                return;
            }

            var ctrl = MainGameManager.Instance?.playerScenePresenter?.PlayerEntity?.abilityController;
            if (ctrl == null)
            {
                return;
            }

            long npcId = npc.Id;
            bool started = ctrl.TryUseAbility(
                "player_common_interact",
                target: npc,
                overrideParams: new Dictionary<string, string>
                {
                    ["InteractTime"] = "0.45",
                },
                phaseOverrideAnims: new Dictionary<string, string>
                {
                    ["Interacting"] = string.Empty,
                },
                onAbilityEnd: complete =>
                {
                    if (complete)
                    {
                        ApplyCarryState(npcId);
                    }
                });

            if (!started)
            {
                MainGameManager.Instance.ShowFakeFxEffect("Cannot carry now", player.Pos);
            }
        }

        // 交互动画结束后，真正挂 buff、隐藏 NPC、显示提示
        private static void ApplyCarryState(long npcId)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            var npc = glm.AreaManager.GetLogicEntiy(npcId) as NpcUnitLogicEntity;
            if (npc == null || npc.MarkDestroyed)
            {
                return;
            }

            if (!npc.IsDead && !npc.MarkUnsensored)
            {
                return;
            }

            if (IsCarrying)
            {
                return;
            }

            var player = glm.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            player.SetSpecialCrouchStance(false);

            var gbm = glm.globalBuffManager;
            gbm.AddBuff(npcId,    "give_hide");
            gbm.AddBuff(player.Id, "player_carry_slow");
            gbm.AddBuff(player.Id, "player_carry_ov_idle");
            gbm.AddBuff(player.Id, "player_carry_ov_move");
            gbm.AddBuff(player.Id, "player_carry_ov_walk");

            CarriedNpcEntityId = npcId;

            OverworldHUDPanel.Instance?.SetCarryBodyHintVisible(true);

            // 立即刷新玩家动画表现（Buff 已经挂上，AnimOverride 可以生效）
            MainGameManager.Instance?.playerScenePresenter?.RefreshLocomotionAnimIfNoStack();

            Debug.Log($"[CarryService] Start carrying npcId={npcId}");
        }

        // 由 OverworldHUDPanel.Update 在玩家按 X 时调用
        public static void TryPutDownCarriedBody()
        {
            if (!IsCarrying)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var player = glm?.playerLogicEntity;
            if (player == null)
            {
                AbortAndClearPlayer();
                return;
            }

            var npc = glm.AreaManager.GetLogicEntiy(CarriedNpcEntityId) as NpcUnitLogicEntity;
            if (npc == null || npc.MarkDestroyed)
            {
                // NPC 已不存在，只清玩家侧
                AbortAndClearPlayer();
                return;
            }

            if (!MapWorldEmptySpotUtil.TryFindEmptySpotNear(
                    player.Pos,
                    PutDownSearchRadius,
                    PutDownClearanceRadius,
                    ignoreIdA: CarriedNpcEntityId,
                    ignoreIdB: player.Id,
                    out Vector2 spot))
            {
                MainGameManager.Instance.ShowFakeFxEffect("No space to put down", player.Pos);
                return;
            }

            // 移除 NPC 隐藏 buff，移动到目标点
            glm.globalBuffManager.RemoveAllBuffById(npc.Id, "give_hide");
            npc.SetPosition(spot);

            long npcId = CarriedNpcEntityId;
            CarriedNpcEntityId = 0;

            RemovePlayerCarryBuffs(player.Id);
            OverworldHUDPanel.Instance?.SetCarryBodyHintVisible(false);
            MainGameManager.Instance?.playerScenePresenter?.RefreshLocomotionAnimIfNoStack();

            Debug.Log($"[CarryService] Put down npcId={npcId} at {spot}");
        }

        private static void AbortAndClearPlayer()
        {
            long npcId = CarriedNpcEntityId;
            CarriedNpcEntityId = 0;

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player != null)
            {
                RemovePlayerCarryBuffs(player.Id);
            }

            OverworldHUDPanel.Instance?.SetCarryBodyHintVisible(false);
            MainGameManager.Instance?.playerScenePresenter?.RefreshLocomotionAnimIfNoStack();

            Debug.Log($"[CarryService] Aborted carry npcId={npcId} (npc gone)");
        }

        private static void RemovePlayerCarryBuffs(long playerId)
        {
            var gbm = MainGameManager.Instance?.gameLogicManager?.globalBuffManager;
            if (gbm == null)
            {
                return;
            }

            gbm.RemoveAllBuffById(playerId, "player_carry_slow");
            gbm.RemoveAllBuffById(playerId, "player_carry_ov_idle");
            gbm.RemoveAllBuffById(playerId, "player_carry_ov_move");
            gbm.RemoveAllBuffById(playerId, "player_carry_ov_walk");
        }
    }
}
