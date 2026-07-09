using cfg.demo;
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
                    if (!QuestDialogSession.TryResolveAccept(session, out var questId, out var characterKey))
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

                    var role = ok ? EQuestDialogRole.AcceptSuccess : EQuestDialogRole.AcceptFail;
                    var dialogId = QuestDialogResolver.Resolve(questId, "", characterKey, role);
                    PlayResultDialog(dialogId, srcEntityId);
                    break;
                }
                case EDialogueQuestAction.Fulfill:
                {
                    if (!QuestDialogSession.TryResolveFulfill(session, out var questId, out var objId, out var characterKey))
                    {
                        Debug.LogWarning("[Dialog] Quest session missing fulfill vars.");
                        ReturnToNpcHub(srcEntityId);
                        break;
                    }

                    var ok = questSystem.TryFulfillObjective(characterKey, questId, objId, out _);
                    if (ok)
                    {
                        glm.AreaManager?.ForceCheckAllRefreshInfos();
                    }

                    var role = ok ? EQuestDialogRole.FulfillSuccess : EQuestDialogRole.FulfillFail;
                    var dialogId = QuestDialogResolver.Resolve(questId, objId, characterKey, role);
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
            MainGameManager.Instance?.PlayDialog(DialoguePlayer.NpcDialogHubId, srcEntityId);
        }
    }
}
