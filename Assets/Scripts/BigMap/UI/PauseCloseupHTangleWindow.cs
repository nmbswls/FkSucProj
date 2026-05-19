
using System;
using My.Input;
using My.Map.Entity;
using My.UI;
using TMPro;
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
        public GameObject CounterHint;

        public long SrcEntityId;
        public float Duration;
        public float CounterExtraShow;

        private bool isCounterPeriod = false;
        private bool isCounterSuccess = false;

        private bool canCounter = false;

        private float _timer;

        private bool triggerCounter;

        private void Update()
        {
            _timer += Time.deltaTime;
            if(_timer > Duration + CounterExtraShow)
            {
                HandleInteractFinish();
                return;
            }

            if(_timer > Duration * 0.5f && _timer < Duration * 0.8f)
            {
                isCounterPeriod = true;
            }
            else
            {
                isCounterPeriod = false;
            }

            if(isCounterPeriod && canCounter && !isCounterSuccess)
            {
                if (!CounterHint.activeSelf)
                {
                    CounterHint.SetActive(true);
                }
            }
            else
            {
                if (CounterHint.activeSelf)
                {
                    CounterHint.SetActive(false);
                }
            }
        }


        public void RefreshData(long srcEntityId, float duration)
        {
            this.SrcEntityId = srcEntityId;
            this.Duration = duration;
            RefreshUI();
        }

        public override void Show()
        {
            base.Show();

            _timer = 0;

            LogicTime.ReleasePause("PauseCloseupWindow");
            LogicTime.RequestPause("PauseCloseupWindow");

            CounterHint.SetActive(false);

            NormalPic.gameObject.SetActive(true);
            CounterPic.gameObject.SetActive(false);

            isCounterPeriod = false;
            isCounterSuccess = false;

            CounterExtraShow = 0;

            CheckPlayerCanCounter();
        }

        /// <summary>
        /// 计算是否能反击咸猪手
        /// </summary>
        private void CheckPlayerCanCounter()
        {
            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            var srcEntity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId);


            var hPowerPlayer = player.GetAttr(AttrIdConsts.HPower);
            var hPowerSrc = srcEntity.GetAttr(AttrIdConsts.HPower);

            if (hPowerPlayer < 5000) hPowerPlayer = 5000;
            if (hPowerSrc < 5000) hPowerSrc = 5000;

            var playerCharm = player.GetAttr(AttrIdConsts.PlayerCharm);
            var enemyWill = srcEntity.GetAttr(AttrIdConsts.Will);

            double charmModifier = Math.Max(0, (playerCharm - enemyWill) * 1.0 / (playerCharm + enemyWill) * 0.5);
            double baseP = (hPowerPlayer * (1 + charmModifier)) / (hPowerPlayer * (1 + charmModifier) + hPowerSrc);

            var randVal = UnityEngine.Random.Range(0, 10000);
            if (randVal < (int)(baseP * 10000))
            {
                canCounter = true;
            }
            else
            {
                canCounter = false;
            }
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


        private void HandleInteractFinish()
        {
            var p = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            var target = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId);
            if (isCounterSuccess)
            {
                p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId : SrcEntityId);

                if(p.DesireLevel >= 2)
                {
                    target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 60_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                    p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                }
                else
                {
                    target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 40_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                    p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                }
                
            }
            else
            {
                p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -8_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);

                if (p.DesireLevel >= 2)
                {
                    target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 40_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                    p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                }
                else
                {
                    target?.ApplyResourceChange(AttrIdConsts.NPCHVal, 20_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                    p.ApplyResourceChange(AttrIdConsts.PlayerPleasure, 5_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
                }
            }

            UIManager.Instance.HidePanel(ID);
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
                if (isCounterPeriod)
                {
                    isCounterSuccess = true;
                    NormalPic.gameObject.SetActive(false);
                    CounterPic.gameObject.SetActive(true);
                    CounterExtraShow = 3.0f;
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