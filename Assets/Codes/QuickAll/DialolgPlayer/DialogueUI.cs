using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("Refs")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI contentText;
    public GameObject nextIndicator;

    [Header("Choice UI")]
    public GameObject choicePanel;
    public Button choiceButtonPrefab;
    public Transform choiceContainer;

    [Header("Typing")]
    public float charInterval = 0.03f;
    //public AudioBus audioBus;
    public string typeSE = null;
    public int seEveryChars = 3;

    [Header("Auto")]
    public float autoDelay = 0.8f;
    [HideInInspector] public float autoTimer = 0f;

    // 打字机状态
    private string currentFullText;
    private int currentIndex;
    private float tick;
    private bool typing;
    private Action onTypingComplete;

    // 选择状态
    private bool showingChoices;
    private Action<int> onChoiceSelected;

    private void Update()
    {
        // 打字机推进
        if (typing)
        {
            tick += Time.deltaTime;
            while (tick >= charInterval)
            {
                tick -= charInterval;
                StepTypewriter();
                if (!typing) break;
            }
        }
    }

    public void StartTypeText(string speaker, string content, string voice, bool fast, Action onComplete)
    {
        if (nameText) nameText.text = speaker;
        ShowNextIndicator(false);

        //if (!string.IsNullOrEmpty(voice) && audioBus != null)
        //{
        //    audioBus.PlayVoice(voice);
        //}

        onTypingComplete = onComplete;

        if (fast)
        {
            if (contentText) contentText.text = content;
            FinishTyping();
        }
        else
        {
            StartTypewriter(content);
        }
    }

    private void StartTypewriter(string fullText)
    {
        currentFullText = fullText ?? "";
        currentIndex = 0;
        tick = 0f;
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

        // 保持富文本标签完整：若遇到 '<'，直接跳到 '>' 后
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
            // 播放 SE（节流）
            //if (!string.IsNullOrEmpty(typeSE) && audioBus != null && seEveryChars > 0)
            //{
            //    if (currentIndex % seEveryChars == 0) audioBus.PlaySE(typeSE);
            //}
        }
    }

    private void FinishTyping()
    {
        typing = false;
        ShowNextIndicator(true);
        var cb = onTypingComplete;
        onTypingComplete = null;
        cb?.Invoke();
    }

    public void ShowNextIndicator(bool show)
    {
        if (nextIndicator) nextIndicator.SetActive(show);
    }

    // 选择系统（非协程）
    public void StartChoices(List<string> options, Action<int> onSelected)
    {
        showingChoices = true;
        onChoiceSelected = onSelected;
        if (choicePanel) choicePanel.SetActive(true);

        // 清空旧按钮
        if (choiceContainer)
        {
            for (int i = choiceContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(choiceContainer.GetChild(i).gameObject);
            }
        }

        for (int i = 0; i < options.Count; i++)
        {
            var btn = Instantiate(choiceButtonPrefab, choiceContainer);
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp) tmp.text = options[i];
            int index = i;
            btn.onClick.AddListener(() => OnChoiceClick(index));
        }
    }

    private void OnChoiceClick(int index)
    {
        if (!showingChoices) return;
        showingChoices = false;
        if (choicePanel) choicePanel.SetActive(false);
        var cb = onChoiceSelected;
        onChoiceSelected = null;
        cb?.Invoke(index);
    }
}