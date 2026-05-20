
using System;
using System.Linq;
using My.Config;
using My.Input;
using My.Map.Entity;
using My.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.View
{

    /// <summary>
    /// 跳脸暂停的H缠绵窗口
    /// 目前仅在主动与发情/魅惑敌人交互时触发
    /// </summary>
    public class PauseCloseupHTangleWindow : PanelBase, IInputConsumer
    {
        public const string ID = "PauseCloseupHTangleWindow";
        public static PauseCloseupHTangleWindow Show(long srcEntityId)
        {
            var panel = UIManager.Instance.ShowPanel(ID) as PauseCloseupHTangleWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupHTangleWindow err");
                return null;
            }

            panel.RefreshData(srcEntityId);
            return panel;
        }

        public RectTransform Mask;
        public Image ShowPic;

        public Image ProgressBar;

        public long SrcEntityId;


        private float _timer;
        private float _lastBalanceTimer;

        public int ActId = 0;
        public long Socre;
        public float CurrentVal;

        // 当前进度
        // 该小游戏
        public int PgreossVal = 0; // 检查突破了几格

        public int BreakTimes = 0;

        const int MaxProgress = 4;
        const int NeedBreakTimes = 5;
        const float CheckInteval = 0.5f;


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private float GetProgressLockVal()
        {
            if(PgreossVal == 0)
            {
                return 0.2f;
            }
            else if(PgreossVal == 1)
            {
                return 0.4f;
            }
            else if (PgreossVal == 2)
            {
                return 0.6f;
            }
            else if (PgreossVal == 3)
            {
                return 0.8f;
            }
            else
            {
                return 1.0f;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            CurrentVal += Time.deltaTime * 1;
            var lockVal = GetProgressLockVal();
            if (CurrentVal >= lockVal)
            {
                CurrentVal = lockVal;
            }

            if(_timer - _lastBalanceTimer > CheckInteval)
            {
                _lastBalanceTimer += CheckInteval;

                ApplyOneActEffect();
            }

            if (_timer > 5)
            {
                HandleInteractFinish();
            }

            if(ProgressBar != null)
            {
                ProgressBar.fillAmount = CurrentVal;
            }
        }

        /// <summary>
        /// 执行一次结算
        /// </summary>
        private void ApplyOneActEffect()
        {
            // 
            if (!PlayerGamePlayRule.ResolveHActParams(ActId, 10, 10, 1, out var hImpulseEnemy, out var hImpulsePlayer))
            {
                Debug.LogError("err ResolveHActParams");
            }

            MainGameManager.Instance.gameLogicManager.playerLogicEntity.ApplyHImpulseDirectly((long)(hImpulsePlayer * 0.2), null);

            var npc = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId) as NpcUnitLogicEntity;
            if (npc != null)
            {
                // 对npc冲击
                npc.ApplyNpcHImpulse((long)(hImpulsePlayer * 0.2));
            }
        }


        public void RefreshData(long srcEntityId)
        {
            this.SrcEntityId = srcEntityId;

            int orgActId = RandomGetTangleHAct();
            ActId = orgActId;

            PgreossVal = 0;
            BreakTimes = 0;

            _timer = 0;
            _lastBalanceTimer = 0;

            RefreshUI();
        }

        public override void Show()
        {
            base.Show();

            _timer = 0;

            LogicTime.ReleasePause("PauseCloseupWindow");
            LogicTime.RequestPause("PauseCloseupWindow");

            MainGameManager.Instance.gameLogicManager.globalBuffManager.AddBuff(SrcEntityId, "fcked_marked", 1, overrideDuration: 0.5f);
            MainGameManager.Instance.gameLogicManager.globalBuffManager.AddBuff(MainGameManager.Instance.gameLogicManager.playerLogicEntity.Id, "charm_fck_bonus", 1, overrideDuration: 0.5f);
        }


        protected void RefreshUI()
        {

        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }

        /// <summary>
        /// 随机获取一个h动作
        /// </summary>
        /// <returns></returns>
        private int RandomGetTangleHAct()
        {
            int checkDesire = PgreossVal;
            var ll = CfgMgr.Cfgs.TbHActInfo.DataList.Where(item => item.FilterType.Contains("Charmed") && item.PlayerMinDesire <= PgreossVal).ToList();
            if (ll.Count == 0)
            {
                return 0;
            }
            return ll[ll.Count - 1].Id;
        }

        /// <summary>
        /// 
        /// </summary>

        private void HandleInteractFinish()
        {
            //ApplyNpcHImpulse

            //var p = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            //var target = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId);
            //if (isCounterSuccess)
            //{
            //    p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId : SrcEntityId);

            //    if(p.DesireLevel >= 2)
            //    {
            //        target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 60_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //        p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //    }
            //    else
            //    {
            //        target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 40_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //        p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //    }

            //}
            //else
            //{
            //    p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -8_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);

            //    if (p.DesireLevel >= 2)
            //    {
            //        target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 40_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //        p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //    }
            //    else
            //    {
            //        target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 20_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //        p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            //    }
            //}

            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            var npc = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId) as NpcUnitLogicEntity;

            if(npc == null)
            {
                Debug.LogError("err ResolveHActParams");
                Debug.LogError("err ResolveHActParams");
                return;
            }
            // 对于静态敌人 玩家碾压 
            if (!PlayerGamePlayRule.ResolveHActParams(ActId, player.GetAttr(AttrIdConsts.HPower), 10, 1, out var hImpulseEnemy, out var hImpulsePlayer))
            {
                Debug.LogError("err ResolveHActParams");
            }

            // 对玩家施加冲击力
            player.ApplyHImpulseDirectly(hImpulsePlayer, null);

            // 对npc冲击
            npc.ApplyNpcHImpulse(hImpulseEnemy);
        }
        
        public override void Hide()
        {
            base.Hide();
            LogicTime.ReleasePause("PauseCloseupWindow");
        }

        public bool OnConfirm()
        {
            return true;
        }

        public bool OnCancel()
        {
            return true;
        }

        public bool OnNavigate(Vector2 dir)
        {
            return true;
        }

        public bool OnHotkey(string keyName)
        {
            if(keyName == EInputKey.Space.ToString())
            {
                // 
                if(PgreossVal < MaxProgress && CurrentVal >= GetProgressLockVal())
                {
                    BreakTimes += 1;
                }

                if(BreakTimes >= NeedBreakTimes)
                {
                    PgreossVal += 1;
                    BreakTimes = 0;

                    // show effect
                    int orgActId = RandomGetTangleHAct();
                    ActId = orgActId;
                }
            }
            return true;
        }


        public bool OnScroll(float deltaY)
        {
            return true;
        }

        public bool OnClick(int button, Vector2 mousePos)
        {
            return true;
        }

        public bool OnHoldStart(string holdKey)
        {
            return true;
        }
        public bool OnHoldUpdate(string holdKey)
        {
            return true;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            return true;
        }
    }
}