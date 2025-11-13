using System.Collections;
using System.Collections.Generic;
using Map.Entity;
using Map.Logic;
using My.UI;
using My.UI.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace Map.SmallGame.Zha
{
    public class DeepZhaQuMiniGamePanel : PanelBase, IInputConsumer
    {
        public static DeepZhaQuMiniGamePanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("DeepZhaQuMiniGame");
                if (panel != null && panel is DeepZhaQuMiniGamePanel panel2)
                {
                    return panel2;
                }
                return null;
            }
        }

        public Transform ViewRoot;
        public Image Slider;
        public Image GoodArea;

        public Transform UpperBound;
        public Transform BottomBound;

        public float PlayerAreaSize = 0.4f;
        public float GoodAreaChangeInterval = 0.3f;
        public float GoodAreaSize = 0.2f;
        public float GoodAreaMoveSpeed = 0.5f;
        public int NeedScore;
        public long ZhaQuTargetId;

        public enum EGoodAreaDir
        {
            None,
            Up,
            Down,
        }
        private EGoodAreaDir currAreaDir = EGoodAreaDir.None;
        private float currentPos = 0;
        private float goodAreaPos = 0; // 0-1
        private float currScore = 50;
        private float goodAreaTimer = 0;

        private bool finished = false;
        private bool isRunning = false;

        private float SelfMoveDir = 0;


        public void Update()
        {
            if(!isRunning)
            {
                return;
            }

            if (!finished)
            {
                AdjustGoodArea();

                AdjustCurrPos();

                CalculateScore();

                if(currScore <= 0)
                {
                    OnSmallGameFinish(false);
                }
                else if (currScore > NeedScore)
                {
                    OnSmallGameFinish(true);
                }

                {
                    float yDiff = UpperBound.localPosition.y - BottomBound.localPosition.y;
                    float yCurr = (currentPos * yDiff) + BottomBound.localPosition.y;
                    Slider.transform.localPosition = new Vector3(Slider.transform.localPosition.x, yCurr, 0);
                }

                {
                    float yDiff = UpperBound.localPosition.y - BottomBound.localPosition.y;
                    float yCurr = (goodAreaPos * yDiff) + BottomBound.localPosition.y;
                    GoodArea.transform.localPosition = new Vector3(GoodArea.transform.localPosition.x, yCurr, 0);
                }

            }
        }

        public void InitializeGame(long targetEntityId, float goodAreaSize, float goodAreaMoveSpeed)
        {
            isRunning = true;
            this.ZhaQuTargetId = targetEntityId;
            this.GoodAreaSize = goodAreaSize;
            this.GoodAreaMoveSpeed = goodAreaMoveSpeed;
            this.goodAreaPos = UnityEngine.Random.Range(2500, 7500) * 0.0001f;

            ViewRoot.gameObject.SetActive(true);
            float yDiff = UpperBound.localPosition.y - BottomBound.localPosition.y;

            float selfAreaY = yDiff * PlayerAreaSize;
            Slider.rectTransform.sizeDelta = new Vector2(Slider.rectTransform.sizeDelta.x, selfAreaY);

            float enemyAreaY = yDiff * GoodAreaSize;
            GoodArea.rectTransform.sizeDelta = new Vector2(GoodArea.rectTransform.sizeDelta.x, enemyAreaY);
        }

        private void AdjustCurrPos()
        {
            if (SelfMoveDir > 0)
            {
                currentPos += Time.deltaTime * 2f;
            }
            else if (SelfMoveDir < 0)
            {
                currentPos -= Time.deltaTime * 2f;
            }
            else
            {
                currentPos -= Time.deltaTime * 1f;
            }

            if (currentPos < PlayerAreaSize * 0.5f)
            {
                currentPos = PlayerAreaSize * 0.5f;
            }
            else if (currentPos > 1 - PlayerAreaSize * 0.5f)
            {
                currentPos = 1 - PlayerAreaSize * 0.5f;
            }
        }

        private void AdjustGoodArea()
        {
            goodAreaTimer -= Time.deltaTime;
            if (goodAreaTimer > 0)
            {
                return;
            }
            goodAreaTimer = GoodAreaChangeInterval;

            var a = UnityEngine.Random.Range(0, 10000);
            if (a < 4000)
            {
                currAreaDir = EGoodAreaDir.Up;
            }
            else if(a < 8000)
            {
                currAreaDir = EGoodAreaDir.Down;
            }
            else
            {
                currAreaDir = EGoodAreaDir.None;
            }

            switch (currAreaDir)
            {
                case EGoodAreaDir.Up:
                    {
                        var diffProgress = GoodAreaMoveSpeed * Time.deltaTime;
                        goodAreaPos += diffProgress;
                    }
                    break;
                case EGoodAreaDir.Down:
                    {
                        var diffProgress = -GoodAreaMoveSpeed * Time.deltaTime;
                        goodAreaPos += diffProgress;
                    }
                    break;
            }

            if(goodAreaPos < GoodAreaSize * 0.5f)
            {
                goodAreaPos = GoodAreaSize * 0.5f;
            }
            else if(goodAreaPos > 1 - GoodAreaSize * 0.5f)
            {
                goodAreaPos = 1 - GoodAreaSize * 0.5f;
            }
        }

        private void CalculateScore()
        {
            float overlap = 0;
            if(goodAreaPos - GoodAreaSize * 0.5f > currentPos + PlayerAreaSize * 0.5f)
            {
                overlap = 0;
            }
            else if(currentPos - PlayerAreaSize * 0.5f > goodAreaPos + GoodAreaSize * 0.5f)
            {
                overlap = 0;
            }
            else
            {
                float p1 = goodAreaPos - GoodAreaSize * 0.5f;
                float p2 = goodAreaPos + GoodAreaSize * 0.5f;
                float p3 = currentPos - PlayerAreaSize * 0.5f;
                float p4 = currentPos - PlayerAreaSize * 0.5f;
                List<float> ps = new() { p1, p2, p3, p4};
                ps.Sort();

                overlap = Mathf.Abs(ps[1] - ps[2]);
            }

            if(overlap / GoodAreaSize > 0.9f)
            {
                currScore += Time.deltaTime * 10f;
            }
            else if(overlap / GoodAreaSize < 0.5f)
            {
                currScore -= Time.deltaTime * 5f;
            }
        }

        public void OnSmallGameFinish(bool success)
        { 
            MainGameManager.Instance.OnSmallGameFinish(ZhaQuTargetId, success, null);
        }

        public bool OnConfirm()
        {
            OnSmallGameFinish(true);
            return true;
        }

        public bool OnCancel()
        {
            return true;
        }

        public bool OnNavigate(Vector2 dir)
        {
            if (dir.y > 0)
            {
                SelfMoveDir = 1;
            }
            else if (dir.y < 0)
            {
                SelfMoveDir = -1;
            }

            return true;
        }

        public bool OnHotkey(int index)
        {
            return true;
        }

        public bool OnScroll(float deltaY)
        {
            return true;
        }

        public bool OnSpace()
        {
            return false;
        }
    }
}

