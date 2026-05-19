using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.View;
using My.UI;
using TMPro;
using UnityEngine;
using static My.MiniGame.DeepAbsorbQteBar;


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


        [SerializeField] private DeepAbsorbQteBar QteBar;
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

            // 随机一个静态动作
            actId = RandomGetStaticHAct();

            QteBar.InitCursorPos();
            QteBar.ResetGame(); 
        }

        private void OnQueBarResult(ZoneType result)
        {
            if(result == ZoneType.Perfect)
            {
                SetPrompt("大成功!");
                SetPromptColor(perfectTextColor);

                perfectCnt += 1;
            }
            else if (result == ZoneType.Success)
            {
                SetPrompt("成功!");
                SetPromptColor(successTextColor);

                successCnt += 1;
            }
            else if (result == ZoneType.Fail)
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


        private Dictionary<string, long> CalcAbsorbGainResult(int successCnt, int perfectCnt)
        {
            return new();
        }

        /// <summary>
        /// 随机获取一个h动作
        /// </summary>
        /// <returns></returns>
        private int RandomGetStaticHAct()
        {
            int playerDesire = MainGameManager.Instance.gameLogicManager.playerLogicEntity.DesireLevel;
            var ll = CfgMgr.Cfgs.TbHActInfo.DataList.Where(item => item.FilterType.Contains("Static") && item.PlayerMinDesire <= playerDesire).ToList();
            if(ll.Count == 0)
            {
                return 0;
            }
            return ll[ll.Count - 1].Id;
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

            // 对于静态敌人 玩家碾压 
            if(!PlayerGamePlayRule.ResolveHActParams(actId, player.GetAttr(AttrIdConsts.HPower), 10, 1, out _, out var hImpulsePlayer))
            {
                Debug.LogError("err ResolveHActParams");
            }

            // 对玩家施加冲击力
            player.ApplyHImpulseDirectly(hImpulsePlayer, null);


            var reward = CalcAbsorbGainResult(successCnt, perfectCnt);

            int score = successCnt * 1 + perfectCnt * 2;
            int bundleId = PlayerGamePlayRule.GetDropBundleFromStaticZha(score);

            var drops = DropUtils.GetBundleDropItems(bundleId);

            DOVirtual.DelayedCall(1.2f, () =>
            {
                if(UIGainRewardCoordinator.Instance != null)
                {
                    foreach(var oneDrop in drops)
                    {
                        UIGainRewardCoordinator.Instance.CreateScreenItem(oneDrop.Item1, oneDrop.Item2, null);
                        MainGameManager.Instance.gameLogicManager.playerDataManager.GiveItemToPlayer(oneDrop.Item1, oneDrop.Item2);
                    }
                }

                canvasGroup.DOFade(0, 0.2f).OnComplete(() =>
                {
                    UIManager.Instance.HidePanel(Id);
                });
            });
        }
    }

}



