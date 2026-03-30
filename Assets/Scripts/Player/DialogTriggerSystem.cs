
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


        private Dictionary<string, int> _dialogTriggerCounter = new();
        public void InitSystem(GameLogicManager ctx, SaveData savingData)
        {
            this.LogicManager = ctx;

            InitAutoTriggerDialogs();
        }

        private Dictionary<string, DialogMetaInfo> _autoDialogs = new();

        private void InitAutoTriggerDialogs()
        {
            foreach (var d in CfgMgr.Cfgs.TbDialogMetaInfo.DataList)
            {
                if (!d.IsAutoTrigger)
                {
                    continue;
                }

                if(d.OnlyOnce)
                {
                    if(_dialogTriggerCounter.TryGetValue(d.DialogId, out var count) && count >= 1)
                    {
                        continue;
                    }
                }


                _autoDialogs.Add(d.DialogId, d);
            }
        }

        public void AddTriggerCount(string dialogId)
        {
            _dialogTriggerCounter[dialogId] = _dialogTriggerCounter.GetValueOrDefault(dialogId) + 1;
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

            string mapName = LogicManager.AreaManager.MapName;
            if(string.IsNullOrEmpty(mapName))
            {
                return;
            }

            foreach (var dialog in _autoDialogs.Values)
            {
                if(dialog.NeedMap != mapName)
                {
                    continue;
                }

                if (dialog.OnlyOnce)
                {
                    if (_dialogTriggerCounter.TryGetValue(dialog.DialogId, out var count) && count >= 1)
                    {
                        continue;
                    }
                }

                bool passed = true;

                foreach(var cond in dialog.TriggerCond)
                {
                    if (!LogicManager.CheckCommonCond(cond))
                    {
                        passed = false;
                        break;
                    }
                }

                if(passed)
                {
                    // 
                    LogicManager.viewer.PlayDialog(dialog.JsonDataName, null, dialog.LockGlobalTime);
                    break;
                }
            }
        }

    }


}