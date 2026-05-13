using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 资源路径：Resources/UI/Prefabs/RumorIntelShopPanel；Inspector 绑定 listHost / closeButton / titleLabel / blockerDismissButton（可选）
    // 打开入口：卧室床行动面板 UIBedroomBedActPanel 的 btnRumorIntel（须先在地图列表中选中一张图）→ RumorIntelShopPanel.OpenForMap(mapId)
    public class RumorIntelShopPanel : PanelWithInput
    {
        public const string Pid = "RumorIntelShop";

        [SerializeField] RectTransform listHost;

        [SerializeField] Button closeButton;

        [Tooltip("点遮罩关闭；可在 Prefab 里与半透明 Blocker 上的 Button 绑定")]
        [SerializeField] Button blockerDismissButton;

        [SerializeField] TMP_Text titleLabel;

        string _mapId;

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

            WireDismissButtons();

            if (listHost == null)
            {
                Debug.LogError(
                    "[RumorIntelShop] listHost is not assigned on prefab. Assign the VerticalLayoutGroup content RectTransform (e.g. ListHost) in RumorIntelShopPanel.prefab.");
            }
        }

        void WireDismissButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HideSelf);
            }

            if (blockerDismissButton != null)
            {
                blockerDismissButton.onClick.RemoveAllListeners();
                blockerDismissButton.onClick.AddListener(HideSelf);
            }
        }

        static void HideSelf()
        {
            UIManager.Instance?.HidePanel(Pid);
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
            HideSelf();
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
                Destroy(listHost.GetChild(i).gameObject);
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
            tmp.fontSize = 17;
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
