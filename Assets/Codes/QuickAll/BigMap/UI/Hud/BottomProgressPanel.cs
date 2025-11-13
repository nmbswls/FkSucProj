using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class BottomProgressPanel : MonoBehaviour
    {
        public static long ShowInstIdCounter = 10;

        /// <summary>
        /// 组件
        /// </summary>

        public TextMeshProUGUI hintTextComp;
        public Image ProgressBar;

        #region 参数

        public long playingShowId;
        public float TargetProgress;
        public string HintText;

        #endregion


        private float valCounter = 0;
        private bool isPlaying = false;

        private bool isFading = false;
        private float fadingTimer = 0;


        public void Update()
        {


            if (isFading)
            {
                fadingTimer -= Time.deltaTime;
                if (fadingTimer < 0)
                {
                    fadingTimer = -1;
                    isFading = false;

                    if (!isPlaying)
                    {
                        HideProgress(playingShowId);
                    }
                }
            }


            
            if(isPlaying)
            {
                if (TargetProgress < 0)
                {
                    return;
                }
                valCounter += Time.deltaTime;
                ProgressBar.fillAmount = valCounter / TargetProgress;
                if (valCounter >= TargetProgress)
                {
                    OnProgressComplete();
                }
            }
        }


        public void Setup(long showId, string hintText, float targetProgress)
        {
            this.playingShowId = showId;
            this.TargetProgress = targetProgress;
            this.HintText = hintText;

            valCounter = 0;
            isPlaying = false;
            isFading = false;
            fadingTimer = -1;

            hintTextComp.text = HintText;
            ProgressBar.fillAmount = 0;

            gameObject.SetActive(true);
        }

        public void HideProgress(long showId)
        {
            if (playingShowId != showId)
            {
                return;
            }

            playingShowId = 0;
            isPlaying = false;

            gameObject.SetActive(false);
        }

        public void TryCancelProgressComplete(long showId, string cancelHint = "Cancel")
        {
            if(!isPlaying)
            {
                return;
            }

            if (playingShowId != showId)
            {
                return;
            }

            hintTextComp.text = cancelHint;
            fadingTimer = 0.3f;
            isFading = true;
        }

        public void OnProgressComplete()
        {
            HideProgress(playingShowId);
        }
    }

}


