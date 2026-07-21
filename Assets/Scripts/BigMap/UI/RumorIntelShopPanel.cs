using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 资源路径：Resources/UI/Prefabs/RumorIntelShopPanel；根节点挂 RumorIntelShopListPopulation + 行模板 RowTemplates
    // 打开入口：WorldMapPanel 地图列表选中后 → RumorIntelShopPanel.OpenForMap(mapId)
    public class RumorIntelShopPanel : PanelWithInput
    {
        public const string Pid = "RumorIntelShop";

        [SerializeField] Button closeButton;

        [Tooltip("点遮罩关闭；可在 Prefab 里与半透明 Blocker 上的 Button 绑定")]
        [SerializeField] Button blockerDismissButton;

        [SerializeField] TMP_Text titleLabel;

        RumorIntelShopListPopulation _listPopulation;

        string _mapId;
        bool _allMaps;
        string _feedback;

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
            _allMaps = data == null;
            _mapId = data as string;
            _feedback = null;
            if (!_allMaps && string.IsNullOrEmpty(_mapId))
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

        public static void OpenForArea(string logicAreaId)
        {
            if (string.IsNullOrEmpty(logicAreaId) || CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.DataList == null)
            {
                OpenForMap(logicAreaId);
                return;
            }

            foreach (var overlay in CfgMgr.Cfgs.TbAreaOverlayStateInfo.DataList)
            {
                if (overlay != null && overlay.VarId == logicAreaId && !string.IsNullOrEmpty(overlay.Id))
                {
                    OpenForMap(overlay.Id);
                    return;
                }
            }

            OpenForMap(logicAreaId);
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
                var map = CfgMgr.Cfgs.TbAreaOverlayStateInfo?.GetOrDefault(_mapId);
                var mapName = map != null && !string.IsNullOrEmpty(map.Desc) ? map.Desc : _mapId;
                titleLabel.text = string.IsNullOrEmpty(mapName) ? "秘闻" : $"秘闻 - {mapName}";
            }

            if (_allMaps)
                _listPopulation?.ClearAndPopulateAll(TryBuyOnMap, _feedback);
            else
                _listPopulation?.ClearAndPopulate(_mapId, TryBuy, _feedback);
        }

        public static void Open()
        {
            UIManager.Instance?.ShowPanel(Pid, null, UILayer.Popup);
        }

        void TryBuy(string rumorId)
            => TryBuyOnMap(_mapId, rumorId);

        void TryBuyOnMap(string mapId, string rumorId)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.playerDataManager == null || string.IsNullOrEmpty(mapId))
            {
                return;
            }

            var ok = glm.playerDataManager.RumorIntel.TryPurchase(mapId, rumorId, out var err);
            if (!ok)
            {
                Debug.LogWarning("[RumorIntelShop] Purchase failed: " + err);
                _feedback = GetPurchaseErrorText(err);
            }
            else
            {
                _feedback = "秘闻已购入，将在下次潜入该区域时生效。";
            }

            Refresh();
        }

        static string GetPurchaseErrorText(string error)
        {
            return error switch
            {
                "rumor_cost" => "秘闻点数不足。",
                "rumor_already_active" => "该秘闻已经处于待生效状态。",
                "rumor_random_slot_occupied" => "该区域已有一条随机秘闻等待生效。",
                "rumor_not_in_offer" => "该秘闻已不在当前候选中。",
                "rumor_cond_fail" => "当前条件不满足，无法购买该秘闻。",
                "rumor_no_map" => "尚未选择潜入区域。",
                _ => "购买失败，请重新打开秘闻界面。",
            };
        }
    }
}
