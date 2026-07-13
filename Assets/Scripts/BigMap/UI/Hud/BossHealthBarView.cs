using My.Map.Logic;
using My.Map;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class BossHealthBarView : MonoBehaviour
    {
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI StateText;
        public Image Fill;

        public static BossHealthBarView Create(Transform parent)
        {
            var fallbackFont = parent.GetComponentInChildren<TextMeshProUGUI>(true)?.font;
            var holder = new GameObject("BossHealthBar", typeof(RectTransform),
                typeof(CanvasGroup), typeof(BossHealthBarView));
            holder.transform.SetParent(parent, false);
            var rect = (RectTransform)holder.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -28f);
            rect.sizeDelta = new Vector2(620f, 70f);

            var bg = CreateImage(holder.transform, "Background", new Color(0.08f, 0.06f, 0.05f, 0.94f));
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            Stretch(bg.rectTransform);

            var fillBg = CreateImage(holder.transform, "FillBackground", new Color(0.18f, 0.14f, 0.11f, 1f));
            fillBg.rectTransform.anchorMin = new Vector2(0.04f, 0.15f);
            fillBg.rectTransform.anchorMax = new Vector2(0.96f, 0.48f);
            Stretch(fillBg.rectTransform);

            var fill = CreateImage(fillBg.transform, "Fill", new Color(0.58f, 0.12f, 0.08f, 1f));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;

            var nameText = CreateText(holder.transform, "BossName", 23f, TextAlignmentOptions.Center);
            nameText.font = fallbackFont;
            nameText.rectTransform.anchorMin = new Vector2(0.08f, 0.5f);
            nameText.rectTransform.anchorMax = new Vector2(0.92f, 0.94f);
            Stretch(nameText.rectTransform);

            var stateText = CreateText(holder.transform, "BossState", 14f, TextAlignmentOptions.Right);
            stateText.font = fallbackFont;
            stateText.rectTransform.anchorMin = new Vector2(0.72f, 0.5f);
            stateText.rectTransform.anchorMax = new Vector2(0.95f, 0.9f);
            Stretch(stateText.rectTransform);

            var view = holder.GetComponent<BossHealthBarView>();
            view.NameText = nameText;
            view.StateText = stateText;
            view.Fill = fill;
            holder.SetActive(false);
            return view;
        }

        static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, float size,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.color = new Color(0.96f, 0.9f, 0.76f, 1f);
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void RefreshView()
        {
            var logic = MainGameManager.Instance?.gameLogicManager;
            var encounters = logic?.AreaManager?.BossEncounters;
            if (encounters == null || !encounters.TryGetActiveBoss(
                    out var entityId, out var displayName, out var state, out var phaseIndex))
            {
                gameObject.SetActive(false);
                return;
            }

            var boss = logic.GetLogicEntity(entityId, false);
            if (boss == null || boss.MarkDestroyed)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (NameText != null)
            {
                NameText.text = string.IsNullOrWhiteSpace(displayName) ? "Boss" : displayName;
            }
            if (StateText != null)
            {
                StateText.text = state == EBossEncounterState.Returning
                    ? "脱战恢复中"
                    : $"阶段 {phaseIndex + 1}";
            }
            if (Fill != null)
            {
                var max = boss.GetResourceMax(AttrIdConsts.HP);
                Fill.fillAmount = max > 0
                    ? Mathf.Clamp01((float)boss.GetAttr(AttrIdConsts.HP) / max)
                    : 0f;
            }
        }
    }
}
