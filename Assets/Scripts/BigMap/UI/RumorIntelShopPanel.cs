using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 资源路径：UI/Prefabs/RumorIntelShopPanel；数据：Setup(string mapId)
    public class RumorIntelShopPanel : PanelWithInput
    {
        public const string Pid = "RumorIntelShop";

        [SerializeField] RectTransform listHost;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text titleLabel;

        string _mapId;
        GameObject _rowTemplate;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (listHost == null)
            {
                BuildDefaultShell();
            }
            else if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => UIManager.Instance?.HidePanel(Pid));
            }
        }

        void BuildDefaultShell()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var blocker = new GameObject("Blocker", typeof(RectTransform), typeof(Image), typeof(Button));
            blocker.transform.SetParent(transform, false);
            var blRt = blocker.GetComponent<RectTransform>();
            blRt.anchorMin = Vector2.zero;
            blRt.anchorMax = Vector2.one;
            blRt.offsetMin = Vector2.zero;
            blRt.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            blocker.GetComponent<Button>().onClick.AddListener(() => UIManager.Instance?.HidePanel(Pid));

            var win = new GameObject("Window", typeof(RectTransform), typeof(Image));
            win.transform.SetParent(transform, false);
            var winRt = win.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.5f, 0.5f);
            winRt.anchorMax = new Vector2(0.5f, 0.5f);
            winRt.pivot = new Vector2(0.5f, 0.5f);
            winRt.sizeDelta = new Vector2(720f, 520f);
            win.GetComponent<Image>().color = new Color(0.14f, 0.15f, 0.18f, 0.98f);

            titleLabel = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            titleLabel.transform.SetParent(win.transform, false);
            var tRt = titleLabel.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0, -8f);
            tRt.sizeDelta = new Vector2(-16f, 36f);
            titleLabel.fontSize = 22;
            titleLabel.color = Color.white;
            titleLabel.alignment = TextAlignmentOptions.Center;

            closeButton = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            closeButton.transform.SetParent(win.transform, false);
            var cRt = closeButton.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(1, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(1f, 1f);
            cRt.anchoredPosition = new Vector2(-8f, -8f);
            cRt.sizeDelta = new Vector2(88f, 32f);
            var cImg = closeButton.GetComponent<Image>();
            cImg.color = new Color(0.35f, 0.38f, 0.42f, 1f);
            closeButton.targetGraphic = cImg;
            var cTxt = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            cTxt.transform.SetParent(closeButton.transform, false);
            var cTr = cTxt.GetComponent<RectTransform>();
            cTr.anchorMin = Vector2.zero;
            cTr.anchorMax = Vector2.one;
            cTr.offsetMin = Vector2.zero;
            cTr.offsetMax = Vector2.zero;
            cTxt.text = "Close";
            cTxt.fontSize = 16;
            cTxt.alignment = TextAlignmentOptions.Center;

            closeButton.onClick.AddListener(() => UIManager.Instance?.HidePanel(Pid));

            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGo.transform.SetParent(win.transform, false);
            var scRt = scrollGo.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0, 0);
            scRt.anchorMax = new Vector2(1, 1);
            scRt.offsetMin = new Vector2(12f, 48f);
            scRt.offsetMax = new Vector2(-12f, -52f);
            scrollGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 1f);
            var scroll = scrollGo.GetComponent<ScrollRect>();

            var vp = new GameObject("Viewport", typeof(RectTransform), typeof(Mask));
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRt = vp.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(4f, 4f);
            vpRt.offsetMax = new Vector2(-4f, -4f);
            vp.GetComponent<Mask>().showMaskGraphic = false;

            listHost = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            listHost.transform.SetParent(vp.transform, false);
            var lRt = listHost;
            lRt.anchorMin = new Vector2(0, 1);
            lRt.anchorMax = new Vector2(1, 1);
            lRt.pivot = new Vector2(0.5f, 1f);
            lRt.sizeDelta = new Vector2(0, 0);
            var vlg = listHost.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;
            scroll.content = listHost;
            scroll.viewport = vpRt;
            scroll.vertical = true;
            scroll.horizontal = false;

            _rowTemplate = new GameObject("RowTemplate", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _rowTemplate.SetActive(false);
            _rowTemplate.transform.SetParent(listHost, false);
            var rowRt = _rowTemplate.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 36f);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            _mapId = data as string;
            if (string.IsNullOrEmpty(_mapId))
            {
                Debug.LogWarning("[RumorIntelShop] Setup expects string mapId.");
            }

            Refresh();
        }

        public override bool OnCancel()
        {
            UIManager.Instance?.HidePanel(Pid);
            return true;
        }

        public static void OpenForMap(string mapId)
        {
            UIManager.Instance?.ShowPanel(Pid, mapId, UILayer.Popup);
        }

        void Refresh()
        {
            if (listHost == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.playerDataManager == null)
            {
                return;
            }

            if (titleLabel != null)
            {
                titleLabel.text = string.IsNullOrEmpty(_mapId) ? "Intel shop" : $"Intel — {_mapId}";
            }

            for (var i = listHost.childCount - 1; i >= 0; i--)
            {
                var ch = listHost.GetChild(i).gameObject;
                if (_rowTemplate != null && ch == _rowTemplate)
                {
                    continue;
                }

                Destroy(ch);
            }

            var rumor = glm.playerDataManager.RumorIntel;

            AddSection("=== Fixed ===");
            var fixedList = rumor.ListPurchasableFixed(_mapId);
            foreach (var def in fixedList)
            {
                AddBuyRow(def.ThumbName, def.CostItemId, def.CostCount, () => TryBuy(def.RumorId));
            }

            if (fixedList.Count == 0)
            {
                AddHint("No fixed intel available.");
            }

            AddSection("=== Rumor pool (pick one) ===");
            if (rumor.HasActiveRandomIntel(_mapId))
            {
                AddHint("Random intel slot occupied; finish infiltration or wait expire.");
            }
            else
            {
                rumor.EnsureRandomOffersForShop(_mapId);
                foreach (var rid in rumor.GetRandomOfferIds(_mapId))
                {
                    var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(rid);
                    if (def == null)
                    {
                        continue;
                    }

                    AddBuyRow(def.ThumbName, def.CostItemId, def.CostCount, () => TryBuy(def.RumorId));
                }
            }

            AddSection("=== Pending (next infiltration) ===");
            var actives = rumor.GetActiveSnapshot(_mapId);
            foreach (var a in actives)
            {
                var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(a.RumorId);
                var txt = def != null ? def.FullText : a.RumorId;
                AddHint($"- {txt}");
            }

            if (actives.Count == 0)
            {
                AddHint("(none)");
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listHost);
        }

        void TryBuy(string rumorId)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.playerDataManager == null || string.IsNullOrEmpty(_mapId))
            {
                return;
            }

            var ok = glm.playerDataManager.RumorIntel.TryPurchase(_mapId, rumorId, out var err);
            if (!ok)
            {
                Debug.LogWarning("[RumorIntelShop] Purchase failed: " + err);
            }

            Refresh();
        }

        GameObject AddSection(string title)
        {
            var go = new GameObject("Sec", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(listHost, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize =17;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.85f, 0.9f, 1f);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28f);
            return go;
        }

        void AddHint(string msg)
        {
            var go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(listHost, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = msg;
            tmp.fontSize = 15;
            tmp.color = Color.white;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 24f);
        }

        void AddBuyRow(string thumb, string costId, long cost, UnityEngine.Events.UnityAction onBuy)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(listHost, false);
            row.GetComponent<LayoutElement>().minHeight = 40f;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.padding = new RectOffset(8, 8, 4, 4);

            var labGo = new GameObject("L", typeof(RectTransform), typeof(TextMeshProUGUI));
            labGo.transform.SetParent(row.transform, false);
            var lab = labGo.GetComponent<TextMeshProUGUI>();
            lab.text = thumb;
            lab.fontSize = 16;
            lab.color = Color.white;
            var labLe = labGo.AddComponent<LayoutElement>();
            labLe.flexibleWidth = 1f;

            var priceGo = new GameObject("P", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceGo.transform.SetParent(row.transform, false);
            var price = priceGo.GetComponent<TextMeshProUGUI>();
            price.text = $"{cost}x {costId}";
            price.fontSize = 14;
            price.color = new Color(0.75f, 0.8f, 0.85f);

            var btnGo = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(row.transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(96f, 32f);
            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.35f, 0.45f, 0.55f, 1f);
            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            var bt = new GameObject("T", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            bt.transform.SetParent(btnGo.transform, false);
            var btRt = bt.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero;
            btRt.offsetMax = Vector2.zero;
            bt.text = "Buy";
            bt.fontSize = 15;
            bt.alignment = TextAlignmentOptions.Center;
            btn.onClick.AddListener(onBuy);
        }
    }
}
