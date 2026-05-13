using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 资源路径：Resources/UI/Prefabs/RumorIntelShopPanel；根节点挂 RumorIntelShopListPopulation + 行模板 RowTemplates
    // 打开入口：卧室床行动面板 UIBedroomBedActPanel 的 btnRumorIntel（须先在地图列表中选中一张图）→ RumorIntelShopPanel.OpenForMap(mapId)
    public class RumorIntelShopPanel : PanelWithInput
    {
        public const string Pid = "RumorIntelShop";

        [SerializeField] Button closeButton;

        [Tooltip("点遮罩关闭；可在 Prefab 里与半透明 Blocker 上的 Button 绑定")]
        [SerializeField] Button blockerDismissButton;

        [SerializeField] TMP_Text titleLabel;

        RumorIntelShopListPopulation _listPopulation;

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

            if (_listPopulation == null)
            {
                _listPopulation = GetComponent<RumorIntelShopListPopulation>();
            }

            WireDismissButtons();

            if (_listPopulation == null)
            {
                Debug.LogError(
                    "[RumorIntelShop] RumorIntelShopListPopulation missing on prefab root.");
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
            if (CfgMgr.Cfgs == null)
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

            _listPopulation?.ClearAndPopulate(_mapId, TryBuy);
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
    }
}
