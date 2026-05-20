using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.Hunting
{
    /// <summary>
    /// 运行时/编辑器共用：在 HUD 下搭建 HuntingNpcDetail 节点树。
    /// </summary>
    public static class HuntingNpcDetailUiBuilder
    {
        const string DetailRootName = "HuntingNpcDetail";
        const string DetailPanelName = "DetailPanel";
        const string ExecuteHintName = "ExecuteHint";

        public static HuntingNpcDetailView BuildUnder(Transform hudRoot)
        {
            if (hudRoot == null)
            {
                return null;
            }

            var existing = hudRoot.Find(DetailRootName);
            if (existing != null)
            {
                var existView = existing.GetComponent<HuntingNpcDetailView>();
                if (existView != null)
                {
                    return existView;
                }
            }

            var detailRt = CreateDetailRoot(hudRoot);
            var panelRt = CreateDetailPanel(detailRt);
            var nameText = CreateTmp(panelRt, "NameText", "NPC", 16, new Vector2(0, 22), new Vector2(200, 26));
            var willText = CreateTmp(panelRt, "NpcWillText", "", 14, new Vector2(72, -6), new Vector2(40, 22));
            var sjBar = CreateFillBar(panelRt, "SJProgressBar", new Vector2(-55, -6), new Vector2(96, 10));
            var executeRt = CreateExecuteHint(detailRt);

            var view = detailRt.gameObject.AddComponent<HuntingNpcDetailView>();
            view.DetailRoot = detailRt;
            view.NameText = nameText;
            view.NpcWillText = willText;
            view.SJProgressBar = sjBar;
            view.ExecuteHintRoot = executeRt;
            view.ExecuteHintText = executeRt.GetComponentInChildren<TextMeshProUGUI>(true);

            detailRt.gameObject.SetActive(false);
            return view;
        }

        static RectTransform CreateDetailRoot(Transform parent)
        {
            var go = new GameObject(DetailRootName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(220f, 100f);
            return rt;
        }

        static RectTransform CreateDetailPanel(RectTransform parent)
        {
            var go = new GameObject(DetailPanelName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            Stretch(rt);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
            bg.raycastTarget = false;
            return rt;
        }

        static RectTransform CreateExecuteHint(RectTransform parent)
        {
            var go = new GameObject(ExecuteHintName, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 52f);
            rt.sizeDelta = new Vector2(100f, 28f);
            CreateTmp(rt, "ExecuteHintText", "点击处决", 13, Vector2.zero, new Vector2(100f, 28f));
            go.SetActive(false);
            return rt;
        }

        static TextMeshProUGUI CreateTmp(RectTransform parent, string name, string text, int size, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Image CreateFillBar(RectTransform parent, string name, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;
            var img = go.AddComponent<Image>();
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 0f;
            img.color = new Color(0.92f, 0.35f, 0.2f, 0.95f);
            img.raycastTarget = false;
            return img;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
