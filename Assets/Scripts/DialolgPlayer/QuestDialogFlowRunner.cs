using My.Cfg_Ex;
using My.Map.Logic;
using My.Player;
using UnityEngine;

namespace My.Dialog
{
    public static class QuestDialogFlowRunner
    {

        public static void Start(DialogueSessionContext session, long? srcEntityId)
        {
            if (session == null)
            {
                return;
            }

            if (QuestDialogSession.TryGetAction(session, out var action) && action == EDialogueQuestAction.Remind)
            {
                PlayRemind(session, srcEntityId);
                return;
            }

            var entryDialogId = QuestDialogSession.GetEntryDialogId(session);
            if (!string.IsNullOrEmpty(entryDialogId))
            {
                MainGameManager.Instance.PlayDialog(
                    entryDialogId,
                    srcEntityId,
                    pause: false,
                    onDialogEnd: () => DispatchQuestAction(session, srcEntityId),
                    sessionContext: session);
                return;
            }

            DispatchQuestAction(session, srcEntityId);
        }

        private static void PlayRemind(DialogueSessionContext session, long? srcEntityId)
        {
            var entryDialogId = QuestDialogSession.GetEntryDialogId(session);
            if (string.IsNullOrEmpty(entryDialogId))
            {
                return;
            }

            MainGameManager.Instance.PlayDialog(
                entryDialogId,
                srcEntityId,
                pause: false,
                onDialogEnd: () => ReturnToNpcHub(srcEntityId),
                sessionContext: session);
        }

        public static void DispatchQuestAction(DialogueSessionContext session, long? srcEntityId)
        {
            if (session == null || !QuestDialogSession.TryGetAction(session, out var action))
            {
                ReturnToNpcHub(srcEntityId);
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var questSystem = glm?.playerDataManager?.QuestSystem;
            if (questSystem == null)
            {
                ReturnToNpcHub(srcEntityId);
                return;
            }

            switch (action)
            {
                case EDialogueQuestAction.Accept:
                {
                    if (!QuestDialogSession.TryResolveAccept(session, out var questId, out var characterKey, out var acceptDialogId))
                    {
                        Debug.LogWarning("[Dialog] Quest session missing accept vars.");
                        ReturnToNpcHub(srcEntityId);
                        break;
                    }

                    var ok = questSystem.TryAcceptQuestFromNpc(characterKey, questId, out _);
                    if (ok)
                    {
                        glm.AreaManager?.ForceCheckAllRefreshInfos();
                    }

                    var dialogId = QuestDialogResolver.ResolveAcceptResult(acceptDialogId, questId, characterKey, ok);
                    PlayResultDialog(dialogId, srcEntityId);
                    break;
                }
                case EDialogueQuestAction.Objective:
                {
                    if (!QuestDialogSession.TryResolveObjective(
                            session, out var questId, out var objId, out var characterKey, out var objectiveDialogId))
                    {
                        Debug.LogWarning("[Dialog] Quest session missing objective vars.");
                        ReturnToNpcHub(srcEntityId);
                        break;
                    }

                    // 先兑现（交物品 / 校验 Talk NPC），成功后再 Mark 触发态（once / Talk 进度 / Remind 门闩）
                    var ok = questSystem.TryFulfillObjective(characterKey, questId, objId, out _);
                    if (ok)
                    {
                        var triggerId = objectiveDialogId;
                        if (triggerId <= 0)
                        {
                            triggerId = QuestDialogResolver.FindObjective(questId, objId, characterKey)?.Id ?? 0;
                        }

                        if (triggerId > 0)
                        {
                            questSystem.MarkObjectiveDialogTriggered(questId, triggerId, objId);
                        }

                        // Talk 进度在 Mark 后才增加，此处再尝试 AutoNext
                        questSystem.TryAdvanceAfterObjectiveDialog(questId);
                        glm.AreaManager?.ForceCheckAllRefreshInfos();
                    }

                    var dialogId = QuestDialogResolver.ResolveObjectiveResult(
                        objectiveDialogId, questId, objId, characterKey, ok);
                    PlayResultDialog(dialogId, srcEntityId);
                    break;
                }
            }
        }

        private static void PlayResultDialog(string dialogId, long? srcEntityId)
        {
            if (string.IsNullOrEmpty(dialogId))
            {
                ReturnToNpcHub(srcEntityId);
                return;
            }

            MainGameManager.Instance.PlayDialog(
                dialogId,
                srcEntityId,
                pause: false,
                onDialogEnd: () => ReturnToNpcHub(srcEntityId));
        }

        private static void ReturnToNpcHub(long? srcEntityId)
        {
            if (srcEntityId.HasValue && srcEntityId.Value != 0)
            {
                My.UI.NpcInteractHubPanel.Open(srcEntityId.Value);
                return;
            }

            MainGameManager.Instance?.PlayDialog(DialoguePlayer.NpcDialogHubId, srcEntityId);
        }
    }
}
