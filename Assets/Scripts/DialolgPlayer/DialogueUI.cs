using System;
using System.Collections.Generic;
using DG.Tweening;
using My.Dialog;
using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace My.UI
{
    public class DialogueUI : PanelBase // 假设 PanelBase 是您自己的基类
    {
        [Header("Refs")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI contentText;

        public Transform nameTextContainer;
        public Transform contentTextContainer;

        public GameObject nextIndicator;
        public Button ClickArea;

        [Header("Choice UI")]
        public GameObject choicePanel;
        public Button choiceButtonPrefab;
        public Transform choiceContainer;

        [Header("Typing")]
        public float charInterval = 0.03f;
        public string typeSE = null;
        public int seEveryChars = 3;

        [Header("Auto")]
        public float autoDelay = 0.8f;
        [HideInInspector] public float autoTimer = 0f;
        public bool IsFastMode = false;

        // 打字机状态
        public List<OneTextLine> readingData;
        public int currentLineIndex = 0;

        private string currentFullText;
        private int currentIndex;
        private float tick;
        private float typingTimer; // 用于记录打字已经进行了多久，防止误触
        private bool typing;
        private Action onTypingComplete;

        // 选择状态
        private bool showingChoices;
        private float choiceLimitTime;
        private float choiceLimitTimeLeft;
        private Action<int> onChoiceSelected;

        public PortraitManager portraits;

        public void Awake()
        {
            if (choiceButtonPrefab) choiceButtonPrefab.gameObject.SetActive(false);

            ClickArea.onClick.RemoveAllListeners();
            ClickArea.onClick.AddListener(TryDoContinue);

        }

        

        private void Update()
        {
            // 打字机推进
            if (typing)
            {
                float dt = Time.deltaTime;
                tick += dt;
                typingTimer += dt; // 累加打字总时长

                while (tick >= charInterval)
                {
                    tick -= charInterval;
                    StepTypewriter();
                    if (!typing) break;
                }
            }

            // 选项限时逻辑
            if (showingChoices && choiceLimitTime > 0)
            {
                choiceLimitTimeLeft -= Time.deltaTime; // 建议用 Time.deltaTime，或替换为您的 LogicTime.deltaTime
                if (choiceLimitTimeLeft <= 0)
                {
                    OnChoiceClick(0); // 超时默认选择第一项
                }
            }
        }

        /// <summary>
        /// 尝试进行continue
        /// </summary>
        private void TryDoContinue()
        {
            // 如果正在展示选项，屏蔽点击，防止错误关闭或跳过
            if (showingChoices) return;

            if (readingData == null || readingData.Count == 0) return;
            if (currentLineIndex >= readingData.Count) return;

            // 正在打字时点击：尝试快进全显
            if (typing)
            {
                // 防误触：打字刚开始的 0.25 秒内点击无效（1秒通常太长，影响手感）
                if (typingTimer < 0.25f)
                {
                    return;
                }

                // 立即显示全量
                contentText.text = currentFullText ?? "";
                FinishTyping();
                return;
            }

            // 打字已完成，进入下一行
            currentLineIndex++;
            if (currentLineIndex < readingData.Count)
            {
                StartTypeOneLine(readingData[currentLineIndex]);
            }
            else
            {
                // 对话结束
                var cb = onTypingComplete;
                onTypingComplete = null;
                cb?.Invoke();

                nameTextContainer.gameObject.SetActive(false);
                contentTextContainer.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 执行一批文本的显示逻辑
        /// </summary>
        public void StartTypeTextBatch(List<OneTextLine> textLines, Action onComplete)
        {
            if (textLines == null || textLines.Count == 0)
            {
                typing = false;
                onComplete?.Invoke();
                return;
            }

            this.readingData = textLines;
            this.currentLineIndex = 0;
            this.onTypingComplete = onComplete;

            StartTypeOneLine(textLines[0]);

            nameTextContainer.gameObject.SetActive(true);
            contentTextContainer.gameObject.SetActive(true);
        }

        /// <summary>
        /// 执行一行文本的打字
        /// </summary>
        private void StartTypeOneLine(OneTextLine line)
        {
            bool hasSpeaker = !string.IsNullOrEmpty(line.Speaker);
            if (nameTextContainer) nameTextContainer.gameObject.SetActive(hasSpeaker);
            if (nameText) nameText.text = hasSpeaker ? line.Speaker : "";

            ShowNextIndicator(false);

            if (IsFastMode)
            {
                if (contentText) contentText.text = line.Content;
                FinishTyping();
            }
            else
            {
                StartTypewriter(line.Content);
            }
        }

        private void StartTypewriter(string fullText)
        {
            currentFullText = fullText ?? "";
            currentIndex = 0;
            tick = 0f;
            typingTimer = 0f; // 重置防误触计时器
            typing = true;

            if (contentText) contentText.text = "";
        }

        private void StepTypewriter()
        {
            if (!typing) return;
            if (contentText == null) { FinishTyping(); return; }
            if (currentIndex >= currentFullText.Length)
            {
                contentText.text = currentFullText;
                FinishTyping();
                return;
            }

            // 保持富文本标签完整
            if (currentFullText[currentIndex] == '<')
            {
                int close = currentFullText.IndexOf('>', currentIndex);
                if (close == -1)
                {
                    // 标签不完整，直接终止为整段
                    contentText.text = currentFullText;
                    FinishTyping();
                    return;
                }
                contentText.text += currentFullText.Substring(currentIndex, close - currentIndex + 1);
                currentIndex = close + 1;
            }
            else
            {
                contentText.text += currentFullText[currentIndex];
                currentIndex++;
            }
        }

        private void FinishTyping()
        {
            typing = false;
            ShowNextIndicator(true);
        }

        public void ShowNextIndicator(bool show)
        {
            if (nextIndicator) nextIndicator.SetActive(show);
        }

        // 选择系统
        public void StartChoices(List<string> options, Action<int> onSelected, float limitTime = 0)
        {
            ShowNextIndicator(false); // 弹出选项时隐藏下一步指示器
            showingChoices = true;
            onChoiceSelected = onSelected;

            choiceLimitTime = limitTime;
            choiceLimitTimeLeft = limitTime;

            if (choicePanel) choicePanel.SetActive(true);

            // 清理旧按钮（排除预制体本身，以防预制体在同一节点下）
            if (choiceContainer)
            {
                foreach (Transform child in choiceContainer)
                {
                    if (child.gameObject != choiceButtonPrefab.gameObject)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            // 生成新选项
            for (int i = 0; i < options.Count; i++)
            {
                var btn = Instantiate(choiceButtonPrefab, choiceContainer);
                btn.gameObject.SetActive(true);
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp) tmp.text = options[i];

                int index = i; // 捕获局部变量
                btn.onClick.AddListener(() => OnChoiceClick(index));
            }

            nameTextContainer.gameObject.SetActive(true);
            contentTextContainer.gameObject.SetActive(true);
        }

        private void OnChoiceClick(int index)
        {
            if (!showingChoices) return;
            showingChoices = false;

            if (choicePanel) choicePanel.SetActive(false);

            choiceLimitTime = 0;
            choiceLimitTimeLeft = 0;

            var cb = onChoiceSelected;
            onChoiceSelected = null;
            cb?.Invoke(index);
        }

        /// <summary>
        /// 打断打字机与选项 UI，供「无缝切换对话段」命令在进入新 Step 前清理残留。
        /// </summary>
        public void PrepareForDialogSegmentSwitch()
        {
            typing = false;
            tick = 0f;
            typingTimer = 0f;
            readingData = null;
            currentLineIndex = 0;
            currentFullText = null;
            currentIndex = 0;
            onTypingComplete = null;

            if (showingChoices)
            {
                showingChoices = false;
                if (choicePanel) choicePanel.SetActive(false);
                choiceLimitTime = 0;
                choiceLimitTimeLeft = 0;
                onChoiceSelected = null;
                if (choiceContainer && choiceButtonPrefab)
                {
                    foreach (Transform child in choiceContainer)
                    {
                        if (child.gameObject != choiceButtonPrefab.gameObject)
                            Destroy(child.gameObject);
                    }
                }
            }

            if (contentText) contentText.text = "";
            ShowNextIndicator(false);
        }
    }

}

