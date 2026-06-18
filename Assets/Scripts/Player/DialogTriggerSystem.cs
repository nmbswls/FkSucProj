
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Saving;

namespace My.Player
{
    public class DialogTriggerSystem : IPlayerSystem
    {
        protected GameLogicManager LogicManager { get; private set; }


        private readonly HashSet<string> _triggeredDialogIds = new();
        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

            _triggeredDialogIds.Clear();
            if (savingData?.PlayerData?.TriggeredDialogIds != null)
            {
                foreach (var dialogId in savingData.PlayerData.TriggeredDialogIds)
                {
                    if (string.IsNullOrEmpty(dialogId))
                    {
                        continue;
                    }

                    _triggeredDialogIds.Add(dialogId);
                }
            }

            InitAutoTriggerDialogs();
        }

        public void PostInit(PlayerSystemManager owner)
        {
        }

        private Dictionary<string, DialogMetaInfo> _autoDialogs = new();

        private void InitAutoTriggerDialogs()
        {
            _autoDialogs.Clear();

            foreach (var d in CfgMgr.Cfgs.TbDialogMetaInfo.DataList)
            {
                if (!d.IsAutoTrigger)
                {
                    continue;
                }

                if (d.OnlyOnce)
                {
                    if (IsDialogTriggered(d.DialogId))
                    {
                        continue;
                    }
                }


                _autoDialogs.Add(d.DialogId, d);
            }
        }

        public void AddTriggerCount(string dialogId)
        {
            if (string.IsNullOrEmpty(dialogId))
            {
                return;
            }

            _triggeredDialogIds.Add(dialogId);
        }

        public bool IsDialogTriggered(string dialogId)
        {
            return !string.IsNullOrEmpty(dialogId) && _triggeredDialogIds.Contains(dialogId);
        }

        public void SaveTo(PlayerData playerData)
        {
            if (playerData == null)
            {
                return;
            }

            playerData.TriggeredDialogIds ??= new List<string>();
            playerData.TriggeredDialogIds.Clear();

            foreach (var dialogId in _triggeredDialogIds)
            {
                playerData.TriggeredDialogIds.Add(dialogId);
            }
        }

        private bool IsDialogAvailable(DialogMetaInfo dialog)
        {
            if (dialog == null)
            {
                return false;
            }

            if (dialog.OnlyOnce && IsDialogTriggered(dialog.DialogId))
            {
                return false;
            }

            foreach (var cond in dialog.TriggerCond)
            {
                if (!LogicManager.CheckCommonCond(cond))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryPlayDialogByTriggerZone(string dialogId)
        {
            if (string.IsNullOrEmpty(dialogId) || LogicManager == null)
            {
                return false;
            }

            if (LogicManager.IsDialogPlayering)
            {
                return false;
            }

            var dialog = CfgMgr.Cfgs.TbDialogMetaInfo.GetOrDefault(dialogId);
            if (!IsDialogAvailable(dialog))
            {
                return false;
            }

            return LogicManager.viewer.PlayDialog(dialog.DialogId, null, dialog.LockGlobalTime);
        }

        private float _autoDialogTimer = 0;
        public void Tick(float dt)
        {
            if(LogicTime.time - _autoDialogTimer < 1.0f)
            {
                return;
            }

            _autoDialogTimer = LogicTime.time;

            if (LogicManager.IsDialogPlayering)
            {
                return;
            }

            string mapName = LogicManager.AreaManager.AreaOverlayId;
            if(string.IsNullOrEmpty(mapName))
            {
                return;
            }

            DialogMetaInfo bestDialog = null;
            var bestPriority = int.MinValue;

            foreach (var dialog in _autoDialogs.Values)
            {
                if(dialog.NeedMap != mapName)
                {
                    continue;
                }

                if (!IsDialogAvailable(dialog))
                {
                    continue;
                }

                if (bestDialog == null || dialog.ShowPriority > bestPriority)
                {
                    bestDialog = dialog;
                    bestPriority = dialog.ShowPriority;
                }
            }

            if (bestDialog != null)
            {
                LogicManager.viewer.PlayDialog(bestDialog.DialogId, null, bestDialog.LockGlobalTime);
            }
        }

    }


}
