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