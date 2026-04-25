using System.Collections.Generic;
using My;
using My.Map.Entity;
using My.UI;
using UnityEngine;

namespace My.Map.Scene
{
    // 搬运入口与表现层衔接：权威状态与 Buff/落点在 PlayerLogicEntity（逻辑层）
    public static class PlayerNpcCarryService
    {
        public static bool IsCarrying =>
            MainGameManager.Instance?.gameLogicManager?.playerLogicEntity?.IsCarryingNpcBody ?? false;

        public static long CarriedNpcEntityId =>
            MainGameManager.Instance?.gameLogicManager?.playerLogicEntity?.CarriedNpcEntityId ?? 0;

        // 由 SceneNpcPresenter.TriggerInteract 调用：先播放交互动画，再由逻辑层进入搬运
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

            if (player.IsCarryingNpcBody)
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
                    if (!complete)
                    {
                        return;
                    }

                    var glm = MainGameManager.Instance?.gameLogicManager;
                    var pl = glm?.playerLogicEntity;
                    if (pl == null)
                    {
                        return;
                    }

                    var npcAfter = glm.AreaManager.GetLogicEntiy(npcId) as NpcUnitLogicEntity;
                    if (npcAfter == null || npcAfter.MarkDestroyed)
                    {
                        return;
                    }

                    if (!pl.TryBeginCarryNpcBody(npcAfter))
                    {
                        return;
                    }

                    OverworldHUDPanel.Instance?.SetCarryBodyHintVisible(true);
                    MainGameManager.Instance?.playerScenePresenter?.RefreshLocomotionAnimIfNoStack();
                    Debug.Log($"[CarryService] Start carrying npcId={npcId} (logic state on player)");
                });

            if (!started)
            {
                MainGameManager.Instance.ShowFakeFxEffect("Cannot carry now", player.Pos);
            }
        }

        // 由 OverworldHUDPanel.Update 在玩家按 X 时调用
        public static void TryPutDownCarriedBody()
        {
            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                return;
            }

            if (!player.TryPutDownCarriedNpcBody(out var noSpace))
            {
                if (noSpace)
                {
                    MainGameManager.Instance.ShowFakeFxEffect("No space to put down", player.Pos);
                }

                return;
            }

            OverworldHUDPanel.Instance?.SetCarryBodyHintVisible(false);
            MainGameManager.Instance?.playerScenePresenter?.RefreshLocomotionAnimIfNoStack();
            Debug.Log("[CarryService] Put down (logic cleared on player)");
        }
    }
}
