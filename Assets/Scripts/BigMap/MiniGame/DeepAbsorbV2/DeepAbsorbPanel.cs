using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using My.Map.View;
using My.UI;
using TMPro;
using UnityEngine;
using static My.MiniGame.DeepAbsorbQteBar;


namespace My.MiniGame
{
    public class DeepAbsorbPanel : PanelWithInput
    {
        public static DeepAbsorbPanel Show(long targetId, int difficulty, int tryCnt)
        {

            var panel = UIManager.Instance.ShowPanel("DeepAbsorbPanel") as DeepAbsorbPanel;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupWindow err");
                return null;
            }

            panel.InitializeGame(targetId, difficulty, tryCnt);
            return panel;
        }


        public static DeepAbsorbPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("DeepAbsorbPanel");
                if (panel != null && panel is DeepAbsorbPanel panel2)
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

            SetPrompt("按 Space 进行判定");
            SetPromptColor(normalTextColor);

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
            

            DOVirtual.DelayedCall(1.2f, () =>
            {
                if(UIGainRewardCoordinator.Instance != null)
                {
                    UIGainRewardCoordinator.Instance.CreateScreenItem("1", 1, null);
                }

                canvasGroup.DOFade(0, 0.2f).OnComplete(() =>
                {
                    UIManager.Instance.HidePanel("DeepAbsorbPanel");
                });
            });
        }
    }

}



