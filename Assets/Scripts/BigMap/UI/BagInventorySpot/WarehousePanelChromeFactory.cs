using System.Linq;
using My.Player.Bag;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Bag
{
    // 在预制体中搭建仓库页签与类型筛选条；Editor 菜单与运行时 Awake 共用，避免两套逻辑。
    public static class WarehousePanelChromeFactory
    {
        public static void EnsureChrome(WarehouseUIPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            var content = panel.transform.Find("WarehouseContent")
                ?? panel.transform.Find("MainBag");
            if (content == null)
            {
                return;
            }
            if (content.name != "WarehouseContent")
            {
                content.name = "WarehouseContent";
            }

            var stripExisting = content.Find("WarehouseTypeFilterStrip");
            var tabsExisting = content.Find("WarehousePageTabs");
            if (stripExisting != null && stripExisting.childCount >= 6
                && tabsExisting != null && tabsExisting.childCount >= WarehouseConfig.PageCount)
            {
                SyncScrollRegion(content);
                panel.warehouseTypeFilterButtons = Enumerable.Range(0, stripExisting.childCount)
                    .Select(i => stripExisting.GetChild(i).GetComponent<Button>())
                    .ToArray();
                panel.warehousePageTabsRoot = tabsExisting;
                return;
            }

            if (content.GetComponent<VerticalLayoutGroup>() == null)
            {
                var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(6, 6, 8, 8);
                vlg.spacing = 6;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlHeight = true;
                vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
            }

            var strip = EnsureChild(content, "WarehouseTypeFilterStrip", 28f);
            strip.SetAsFirstSibling();
            if (strip.GetComponent<HorizontalLayoutGroup>() == null)
            {
                var h = strip.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 4;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.padding = new RectOffset(4, 4, 0, 0);
                h.childControlWidth = false;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
            }
            var stripLe = strip.gameObject.GetComponent<LayoutElement>() ?? strip.gameObject.AddComponent<LayoutElement>();
            stripLe.minHeight = 28f;
            stripLe.preferredHeight = 28f;

            string[] filterLabels = { "\u5168", "\u666e", "\u5e01", "\u88c5", "\u6302", "\u63d2" };
            while (strip.childCount < filterLabels.Length)
            {
                CreateFilterButton(strip, filterLabels[strip.childCount]);
            }
            while (strip.childCount > filterLabels.Length)
            {
                DestroyUiObject(strip.GetChild(strip.childCount - 1).gameObject);
            }
            for (int i = 0; i < filterLabels.Length; i++)
            {
                var txt = strip.GetChild(i).GetComponentInChildren<Text>(true);
                if (txt != null)
                {
                    txt.text = filterLabels[i];
                }
            }

            var tabsRoot = EnsureChild(content, "WarehousePageTabs", 36f);
            tabsRoot.SetSiblingIndex(1);
            if (tabsRoot.GetComponent<HorizontalLayoutGroup>() == null)
            {
                var h = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 4;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.padding = new RectOffset(4, 4, 4, 4);
                h.childControlWidth = false;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = true;
            }
            var tabsLe = tabsRoot.gameObject.GetComponent<LayoutElement>() ?? tabsRoot.gameObject.AddComponent<LayoutElement>();
            tabsLe.minHeight = 36f;
            tabsLe.preferredHeight = 36f;

            for (int p = tabsRoot.childCount; p < WarehouseConfig.PageCount; p++)
            {
                CreatePageTab(tabsRoot, p + 1);
            }
            while (tabsRoot.childCount > WarehouseConfig.PageCount)
            {
                DestroyUiObject(tabsRoot.GetChild(tabsRoot.childCount - 1).gameObject);
            }

            SyncScrollRegion(content);

            panel.warehouseTypeFilterButtons = Enumerable.Range(0, strip.childCount)
                .Select(i => strip.GetChild(i).GetComponent<Button>())
                .ToArray();
            panel.warehousePageTabsRoot = tabsRoot;
        }

        static void SyncScrollRegion(Transform content)
        {
            var scrollTf = content.Cast<Transform>().FirstOrDefault(t => t.name == "WarehouseItemScroll" || t.name == "BagGrids");
            if (scrollTf != null)
            {
                scrollTf.name = "WarehouseItemScroll";
                var le = scrollTf.gameObject.GetComponent<LayoutElement>() ?? scrollTf.gameObject.AddComponent<LayoutElement>();
                le.flexibleHeight = 1f;
                le.minHeight = 120f;
            }
        }

        static void DestroyUiObject(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(go);
                return;
            }
#endif
            Object.Destroy(go);
        }

        static Transform EnsureChild(Transform parent, string name, float preferredHeight)
        {
            var t = parent.Find(name);
            if (t != null)
            {
                return t;
            }
            var go = new GameObject(name, typeof(RectTransform));
            t = go.transform;
            t.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, preferredHeight);
            return t;
        }

        static void CreateFilterButton(Transform strip, string label)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(strip, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(72f, 24f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.18f, 0.32f, 0.95f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var tr = txtGo.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
            var text = txtGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.92f, 0.88f, 1f, 1f);
            text.fontSize = 12;
            text.resizeTextForBestFit = true;
        }

        static void CreatePageTab(Transform tabsRoot, int pageLabel)
        {
            var go = new GameObject("WarehousePageTab_" + pageLabel, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(tabsRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(56f, 28f);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.16f, 0.3f, 0.9f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var selGo = new GameObject("Select", typeof(RectTransform), typeof(Image));
            selGo.transform.SetParent(go.transform, false);
            var srt = selGo.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;
            var simg = selGo.GetComponent<Image>();
            simg.color = new Color(0.45f, 0.35f, 0.65f, 0.55f);
            simg.gameObject.SetActive(false);

            var hintGo = new GameObject("Hint", typeof(RectTransform), typeof(Text));
            hintGo.transform.SetParent(go.transform, false);
            var hrt = hintGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.offsetMin = new Vector2(2f, 2f);
            hrt.offsetMax = new Vector2(-2f, -2f);
            var ht = hintGo.GetComponent<Text>();
            ht.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ht.fontSize = 11;
            ht.alignment = TextAnchor.MiddleCenter;
            ht.color = new Color(0.9f, 0.88f, 1f, 1f);
            ht.text = "0/0";
        }
    }
}
