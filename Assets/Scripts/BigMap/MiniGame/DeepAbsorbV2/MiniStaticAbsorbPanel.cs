using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using DG.Tweening;
using My.Config;
using My.Input;
using My.Map;
using My.Map.Entity;
using My.Map.View;
using My.UI;
using TMPro;
using UnityEngine;


namespace My.MiniGame
{
    public class MiniStaticAbsorbPanel : PanelWithInput
    {
        public const string Id = "MiniStaticAbsorbPanel";
        public static MiniStaticAbsorbPanel Show(long targetId, int difficulty, int tryCnt)
        {

            var panel = UIManager.Instance.ShowPanel(Id) as MiniStaticAbsorbPanel;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupWindow err");
                return null;
            }

            panel.InitializeGame(targetId, difficulty, tryCnt);
            return panel;
        }


        public static MiniStaticAbsorbPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel(Id);
                if (panel != null && panel is MiniStaticAbsorbPanel panel2)
                {
                    return panel2;
                }
                return null;
            }
        }


        [SerializeField] private MiniStaticAbsorbQteBar QteBar;
        [SerializeField] private TMP_Text promptText; // 可用 Text 替代

        [Header("Feedback Colors")]
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color successTextColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color perfectTextColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color failTextColor = new Color(1f, 0.3f, 0.3f);

        protected void Awake()
        {
            QteBar.SetResultCallback(OnQueBarResult);

            SetPrompt("按 Space 进行判定");
            SetPromptColor(normalTextColor);
        }

        public long TargetEntityId = 0;
        public int TotalChance = 3;
        public int Difficulty = 3;

        private int chanceLeft = 0;
        private int actId = 0;

        private int failCnt = 0;
        private int noOpCnt = 0;
        private int successCnt = 0;
        private int perfectCnt = 0;

        private bool finished = false;
        private bool isRunning = false;

        public void InitializeGame(long targetEntityId, int difficulty, int totalChance)
        {
            isRunning = true;

            this.TargetEntityId = targetEntityId;
            this.TotalChance = totalChance;
            this.Difficulty = difficulty;

            this.chanceLeft = totalChance;
            this.successCnt = 0;
            this.perfectCnt = 0;
            this.failCnt = 0;
            this.noOpCnt = 0;

            SetPrompt("按 Space 进行判定");
            SetPromptColor(normalTextColor);

            // 随机一个静态动作；默认接触「穴」，写到目标 NPC.Receive
            var glm = MainGameManager.Instance.gameLogicManager;
            actId = PlayerGamePlayRule.RandomGetOneHAct("Unsensor", glm.playerLogicEntity.DesireLevel);
            var npc = glm.GetLogicEntity(targetEntityId, false) as NpcUnitLogicEntity;
            npc?.HInteraction.Receive.Begin(
                EBodyPart.Womb, EHInteractionSource.StaticAbsorb, actId);

            QteBar.InitCursorPos();
            QteBar.ResetGame(); 
        }

        private void OnQueBarResult(MiniStaticAbsorbQteBar.ZoneType result)
        {
            if(result == MiniStaticAbsorbQteBar.ZoneType.Perfect)
            {
                SetPrompt("大成功!");
                SetPromptColor(perfectTextColor);

                perfectCnt += 1;
            }
            else if (result == MiniStaticAbsorbQteBar.ZoneType.Success)
            {
                SetPrompt("成功!");
                SetPromptColor(successTextColor);

                successCnt += 1;
            }
            else if (result == MiniStaticAbsorbQteBar.ZoneType.Fail)
            {
                SetPrompt("失败!");
                SetPromptColor(successTextColor);

                chanceLeft = 0;
            }
            else 
            {
                SetPrompt("无");
                SetPromptColor(failTextColor);

                noOpCnt += 1;
            }

            chanceLeft -= 1;
            if(chanceLeft <= 0)
            {
                OnMiniGameFinish();
            }
            else
            {
                OnMiniGameNewTurn();
            }
        }

        private void SetPrompt(string text)
        {
            if (promptText != null) promptText.text = text;
        }

        private void SetPromptColor(Color c)
        {
            if (promptText != null) promptText.color = c;
        }

        private void OnMiniGameNewTurn()
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                QteBar.ResetGame();

                SetPrompt("按 Space 进行判定");
                SetPromptColor(normalTextColor);
            });
        }

        private void OnMiniGameFinish()
        {
            //MainGameManager.Instance.OnSmallGameFinish(ZhaQuTargetId, success, null);

            var player = MainGameManager.Instance.gameLogicManager.playerLogicEntity;


            if(failCnt > 0)
            {
                long costSan = TotalChance * 5000;

                // 按照成功次数扣减理智
                player.ApplyResourceChange(AttrIdConsts.PlayerSanity, -costSan, false, Map.Fight.FightStruct.EDmgFlag.None, null);
            }
            else
            {
                double oneCost = 3000.0 / TotalChance;
                long costSan = (long)((oneCost * noOpCnt * 1.0) + (oneCost * noOpCnt * 0.8) + (oneCost * noOpCnt * 0.5));

                // 按照成功次数扣减理智
                player.ApplyResourceChange(AttrIdConsts.PlayerSanity, -costSan, false, Map.Fight.FightStruct.EDmgFlag.None, null);
            }

            // 静态榨取：只结算冲击，不派生 HP
            var npc = MainGameManager.Instance.gameLogicManager.GetLogicEntity(TargetEntityId, false) as NpcUnitLogicEntity;
            if (!HActResolver.TryResolveAndApply(actId, player, npc, intensity: 1f, applyHpDamage: false))
            {
                Debug.LogError("err ResolveHActParams");
            }

            int score = successCnt * 1 + perfectCnt * 2;
            int bundleId = PlayerGamePlayRule.GetDropBundleFromStaticZha(score);

            var drops = DropUtils.GetBundleDropRewards(bundleId);

            DOVirtual.DelayedCall(1.2f, () =>
            {
                if(UIGainRewardCoordinator.Instance != null)
                {
                    foreach(var oneDrop in drops)
                    {
                        UIGainRewardCoordinator.Instance.CreateScreenItem(oneDrop.ItemId, oneDrop.Amount, null);
                        MainGameManager.Instance.gameLogicManager.playerDataManager.GiveDropReward(oneDrop);
                    }
                }

                canvasGroup.DOFade(0, 0.2f).OnComplete(() =>
                {
                    UIManager.Instance.HidePanel(Id);
                });
            });
        }

        public override bool OnHotkey(string keyName)
        {
            if (keyName == EInputKey.Space.ToString())
            {
                QteBar.CheckHit();
                return true;
            }
            return false;
        }
    }

}



