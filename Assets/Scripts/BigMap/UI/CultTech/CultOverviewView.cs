using System.Collections.Generic;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    public sealed class CultOverviewView : MonoBehaviour
    {
        readonly List<TextMeshProUGUI> _valueTexts = new();
        DemonCultSystem _cult;
        RectTransform _cardRoot;
        TextMeshProUGUI _influenceValue;

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult;
            EnsureLayout();
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Refresh()
        {
            if (_cult == null || _valueTexts.Count < 4) return;
            _valueTexts[0].text = _cult.Faith.ToString();
            _valueTexts[1].text = _cult.GetUnlockedTechCount().ToString();
            _valueTexts[2].text = _cult.UnlockedSeatCount.ToString();
            _valueTexts[3].text = _cult.UnlockedSeatTechNodeCount.ToString();
            if (_influenceValue != null) _influenceValue.text = "待接入";
        }

        void EnsureLayout()
        {
            var background = EnsureImage("OverviewBackground", new Color(0.07f, 0.045f, 0.075f, 0.98f));
            Stretch(background.rectTransform, new Vector2(0.03f, 0.08f), new Vector2(0.97f, 0.91f));

            var title = EnsureText("OverviewTitle", "教团概览", 30f, TextAlignmentOptions.Left);
            Stretch(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.9f));

            var subtitle = EnsureText("OverviewSubtitle", "信仰、教义与古老者之座的总体状态", 15f, TextAlignmentOptions.Left);
            subtitle.color = new Color(0.72f, 0.62f, 0.68f, 1f);
            Stretch(subtitle.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.79f));

            if (_cardRoot == null)
            {
                var cardObject = new GameObject("OverviewCards", typeof(RectTransform));
                cardObject.transform.SetParent(transform, false);
                _cardRoot = (RectTransform)cardObject.transform;
            }
            Stretch(_cardRoot, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.68f));
            if (_cardRoot.childCount == 0)
            {
                CreateCard("FaithCard", "信仰", "当前积累的教团信仰", 0);
                CreateCard("DoctrineCard", "教义铭刻", "已解锁的核心教团科技", 1);
                CreateCard("SeatCard", "古老者之座", "已开启的座席入口", 2);
                CreateCard("SeatTechCard", "座中火团", "已点亮的座席节点", 3);
            }

            var influenceCard = EnsureImage("InfluenceCard", new Color(0.11f, 0.07f, 0.12f, 0.96f));
            Stretch(influenceCard.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.29f));
            var influenceLabel = EnsureText("InfluenceLabel", "影响人数", 18f, TextAlignmentOptions.Left);
            Stretch(influenceLabel.rectTransform, new Vector2(0.05f, 0.56f), new Vector2(0.4f, 0.9f), influenceCard.rectTransform);
            _influenceValue ??= EnsureText("InfluenceValue", "待接入", 28f, TextAlignmentOptions.Right);
            Stretch(_influenceValue.rectTransform, new Vector2(0.55f, 0.28f), new Vector2(0.95f, 0.82f), influenceCard.rectTransform);
            var influenceHint = EnsureText("InfluenceHint", "当前数据层尚未提供影响人数统计，先保留入口位置", 13f, TextAlignmentOptions.Left);
            influenceHint.color = new Color(0.58f, 0.5f, 0.58f, 1f);
            Stretch(influenceHint.rectTransform, new Vector2(0.05f, 0.12f), new Vector2(0.8f, 0.42f), influenceCard.rectTransform);
        }

        void CreateCard(string name, string title, string hint, int valueIndex)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_cardRoot, false);
            var rect = (RectTransform)card.transform;
            rect.anchorMin = new Vector2(valueIndex * 0.255f, 0f);
            rect.anchorMax = new Vector2(valueIndex * 0.255f + 0.235f, 1f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            card.GetComponent<Image>().color = new Color(0.11f, 0.07f, 0.12f, 0.96f);
            var titleText = CreateText(card.transform, "Title", title, 16f, TextAlignmentOptions.Left);
            Stretch(titleText.rectTransform, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.9f));
            var valueText = CreateText(card.transform, "Value", "0", 30f, TextAlignmentOptions.Left);
            Stretch(valueText.rectTransform, new Vector2(0.1f, 0.28f), new Vector2(0.9f, 0.66f));
            var hintText = CreateText(card.transform, "Hint", hint, 11f, TextAlignmentOptions.Left);
            hintText.color = new Color(0.6f, 0.52f, 0.61f, 1f);
            Stretch(hintText.rectTransform, new Vector2(0.1f, 0.08f), new Vector2(0.9f, 0.26f));
            _valueTexts.Add(valueText);
        }

        Image EnsureImage(string name, Color color)
        {
            var image = transform.Find(name)?.GetComponent<Image>();
            if (image != null) return image;
            var objectRoot = new GameObject(name, typeof(RectTransform), typeof(Image));
            objectRoot.transform.SetParent(transform, false);
            image = objectRoot.GetComponent<Image>();
            image.color = color;
            return image;
        }

        TextMeshProUGUI EnsureText(string name, string content, float size, TextAlignmentOptions alignment)
        {
            var text = transform.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (text != null) return text;
            return CreateText(transform, name, content, size, alignment);
        }

        static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, TextAlignmentOptions alignment)
        {
            var objectRoot = new GameObject(name, typeof(RectTransform));
            objectRoot.transform.SetParent(parent, false);
            var text = objectRoot.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect, Vector2 min, Vector2 max, RectTransform parent = null)
        {
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}