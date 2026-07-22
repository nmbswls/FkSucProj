
using System;
using cfg.demo;
using DG.Tweening;
using My.Config;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.View
{


    public class PauseCloseupKaiYouWindow : PanelBase, IInputConsumer
    {
        public const string ID = "PauseCloseupKaiYouWindow";
        public static PauseCloseupKaiYouWindow Show(long srcEntityId, string showName, float duration)
        {
            var panel = UIManager.Instance.ShowPanel(ID) as PauseCloseupKaiYouWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupWindow err");
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
        public int HActId = 0;
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

            var glm = MainGameManager.Instance.gameLogicManager;
            HActId = PlayerGamePlayRule.RandomGetOneHAct("KaiYou", glm.playerLogicEntity.DesireLevel);

            if (HActId == 0)
            {
                Debug.LogError("RefreshData");
            }

            // NPC 主动：开胸类默认接触胸
            var npc = glm.GetLogicEntity(srcEntityId, false) as NpcUnitLogicEntity;
            npc?.HInteraction.Active.Begin(
                EBodyPart.Breast, EHInteractionSource.CloseupKaiYou, HActId);

            CheckPlayerCanCounter();

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
        }

        /// <summary>
        /// �����Ƿ��ܷ���������
        /// </summary>
        private void CheckPlayerCanCounter()
        {
            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;
            var srcEntity = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId);
            if(srcEntity == null)
            {
                Debug.LogError($"ERRRRRRR {SrcEntityId} null");
                return;
            }

            var hPowerPlayer = player.GetAttr(AttrIdConsts.HTechnique);
            var hPowerSrc = srcEntity.GetAttr(AttrIdConsts.HTechnique);

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
            var target = MainGameManager.Instance.gameLogicManager.GetLogicEntity(SrcEntityId) as NpcUnitLogicEntity;

            if(p == null || target == null)
            {
                Debug.LogError("err ResolveHActParams 11");
                return;
            }
            if (!HActResolver.TryResolveAndApply(HActId, p, target, intensity: 1f, applyHpDamage: false))
            {
                Debug.LogError("err ResolveHActParams");
            }


            if (isCounterSuccess)
            {
                p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -3_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
            }
            else
            {
                p.ApplyResourceChange(AttrIdConsts.PlayerSanity, -6_000, false, Fight.FightStruct.EDmgFlag.None, srcEntityId: SrcEntityId);
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
                    CounterExtraShow = 2.0f;

                    HActId = PlayerGamePlayRule.RandomGetOneHAct("KaiYou", MainGameManager.Instance.gameLogicManager.playerLogicEntity.DesireLevel);
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