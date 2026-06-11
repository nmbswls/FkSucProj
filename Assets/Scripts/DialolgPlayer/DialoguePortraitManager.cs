using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PortraitManager : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public string name;              // "Left","Right","Center"
        public RectTransform root;
        public Image image;
        public CanvasGroup group;
        [HideInInspector] public string characterId;
        [HideInInspector] public string expressionId;
    }

    public List<Slot> slots = new List<Slot>();
    public DIalogueCharacterDatabase db;

    private void Awake()
    {
        EnsureSlotsFromHierarchy();
    }

    // DialoguePanel 预制体未配置 slots 时，按子节点横向位置自动绑定 Left/Center/Right
    public void EnsureSlotsFromHierarchy()
    {
        if (slots != null && slots.Count > 0)
            return;

        slots = new List<Slot>();
        var children = new List<(RectTransform rt, float x)>();
        foreach (Transform child in transform)
        {
            if (child is RectTransform rt)
                children.Add((rt, rt.anchoredPosition.x));
        }

        if (children.Count == 0)
            return;

        children.Sort((a, b) => a.x.CompareTo(b.x));
        string[] slotNames = { "Left", "Center", "Right" };
        for (int i = 0; i < children.Count && i < slotNames.Length; i++)
            slots.Add(BuildSlot(children[i].rt, slotNames[i]));
    }

    private static Slot BuildSlot(RectTransform root, string slotName)
    {
        var image = root.GetComponent<Image>();
        if (image == null)
            image = root.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        var group = root.GetComponent<CanvasGroup>();
        if (group == null)
            group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        root.sizeDelta = new Vector2(300f, 400f);
        root.gameObject.name = slotName;

        return new Slot
        {
            name = slotName,
            root = root,
            image = image,
            group = group
        };
    }

    public void Show(string slotName, string charId, string expr, float fade, DialogueTimeDriver driver, Action onComplete)
    {
        var s = FindSlot(slotName);
        if (s == null) { onComplete?.Invoke(); return; }
        s.characterId = charId;
        s.expressionId = expr;
        var sprite = db != null ? db.LoadSprite(charId, expr) : null;
        s.image.sprite = sprite;

        float start = s.group ? s.group.alpha : 1f;
        float target = 1f;
        if (s.group && fade > 0f)
        {
            driver.Run(fade, p => {
                s.group.alpha = Mathf.Lerp(start, target, p);
            }, onComplete);
        }
        else
        {
            if (s.group) s.group.alpha = target;
            onComplete?.Invoke();
        }
    }

    public void ChangeExpression(string slotName, string expr, float fade, DialogueTimeDriver driver, Action onComplete)
    {
        var s = FindSlot(slotName);
        if (s == null) { onComplete?.Invoke(); return; }
        s.expressionId = expr;
        var sprite = db != null ? db.LoadSprite(s.characterId, expr) : null;

        if (fade > 0.01f)
        {
            // 创建覆盖图进行交叉淡入
            var overlayGO = new GameObject("ExprOverlay");
            overlayGO.transform.SetParent(s.image.transform.parent, false);
            var overlay = overlayGO.AddComponent<Image>();
            CopyRect(s.image.rectTransform, overlay.rectTransform);
            overlay.sprite = sprite;
            overlay.color = new Color(1, 1, 1, 0);

            driver.Run(fade, p => {
                overlay.color = new Color(1, 1, 1, p);
            }, () => {
                s.image.sprite = sprite;
                Destroy(overlayGO);
                onComplete?.Invoke();
            });
        }
        else
        {
            s.image.sprite = sprite;
            onComplete?.Invoke();
        }
    }

    // 根据 Speaker 名字高亮对应立绘槽（slot.characterId 需与 Speaker 一致）
    public void FocusSpeaker(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
            return;

        foreach (var slot in slots)
        {
            if (slot?.group == null || string.IsNullOrEmpty(slot.characterId))
                continue;
            bool active = slot.characterId == speaker;
            slot.group.alpha = active ? 1f : 0.45f;
        }
    }

    public void ShowSlotSprite(string slotName, Sprite sprite, string speakerId = null)
    {
        EnsureSlotsFromHierarchy();

        var slot = FindSlot(slotName);
        if (slot == null)
            return;

        if (!string.IsNullOrEmpty(speakerId))
            slot.characterId = speakerId;

        if (sprite != null && slot.image != null)
            slot.image.sprite = sprite;

        if (slot.group != null)
            slot.group.alpha = 1f;
    }

    public void Hide(string slotName, float fade, DialogueTimeDriver driver, Action onComplete)
    {
        var s = FindSlot(slotName);
        if (s == null) { onComplete?.Invoke(); return; }
        float start = s.group ? s.group.alpha : 1f;
        float target = 0f;
        if (s.group && fade > 0f)
        {
            driver.Run(fade, p => {
                s.group.alpha = Mathf.Lerp(start, target, p);
            }, onComplete);
        }
        else
        {
            if (s.group) s.group.alpha = target;
            onComplete?.Invoke();
        }
    }

    private Slot FindSlot(string name)
    {
        return slots.FirstOrDefault(x => x.name == name);
    }

    private void CopyRect(RectTransform src, RectTransform dst)
    {
        dst.anchorMin = src.anchorMin;
        dst.anchorMax = src.anchorMax;
        dst.pivot = src.pivot;
        dst.sizeDelta = src.sizeDelta;
        dst.anchoredPosition = src.anchoredPosition;
    }
}