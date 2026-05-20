
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
        public static PauseCloseupHTangleWindow Show(long srcEntityId, string showName, float duration)
        {
            var panel = UIManager.Instance.ShowPanel(ID) as PauseCloseupHTangleWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupHTangleWindow err");
                return null;
            }

            panel.RefreshData(srcEntityId, duration);
            return panel;
        }

        public RectTransform Mask;
        public Image NormalPic;
        public Image CounterPic;

        public int ActId = 0;
        public long SrcEntityId;

        private bool canCounter = false;

        private float _timer;

        public long Socre;

        private void Update()
        {
            _timer += Time.deltaTime;
         
            
            if(_timer > 5)
            {
                HandleInteractFinish();
            }
        }


        public void RefreshData(long srcEntityId, float duration)
        {
            this.SrcEntityId = srcEntityId;

            int orgActId = RandomGetTangleHAct();
            ActId = orgActId;

            RefreshUI();
        }

        public override void Show()
        {
            base.Show();

            _timer = 0;

            LogicTime.ReleasePause("PauseCloseupWindow");
            LogicTime.RequestPause("PauseCloseupWindow");


            NormalPic.gameObject.SetActive(true);
            CounterPic.gameObject.SetActive(false);
        }

        

        private void SwitchToCounterMode()
        {

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
            int playerDesire = MainGameManager.Instance.gameLogicManager.playerLogicEntity.DesireLevel;
            var ll = CfgMgr.Cfgs.TbHActInfo.DataList.Where(item => item.FilterType.Contains("Charmed") && item.PlayerMinDesire <= playerDesire).ToList();
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