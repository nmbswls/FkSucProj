using My;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class DayPeriodSettlementPanel : PanelWithInput
    {
        public const string PanelId = "DayPeriodSettlementPanel";

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text summaryText;
        [SerializeField] UICommonItemAmountGridView outputGrid;
        [SerializeField] Button btnDetail;
        [SerializeField] GameObject detailOverlay;
        [SerializeField] Button btnDetailClose;
        [SerializeField] Button btnConfirm;

        public static void Show(GameLogicManager.OneDayBalanceInfo info)
        {
            var panel = UIManager.Instance.ShowPanel(PanelId, info) as DayPeriodSettlementPanel;
            if (panel == null)
            {
                Debug.LogError("DayPeriodSettlementPanel: panel not found");
            }
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelId;
            }

            ResolveRefs();
        }

        void ResolveRefs()
        {
            titleText ??= transform.Find("Title")?.GetComponent<TMP_Text>();
            summaryText ??= transform.Find("Summary")?.GetComponent<TMP_Text>();
            btnConfirm ??= transform.Find("BtnConfirm")?.GetComponent<Button>();
            btnDetail ??= transform.Find("BuildingOutputSection/HeaderRow/BtnDetail")?.GetComponent<Button>();
            detailOverlay ??= transform.Find("DetailOverlay")?.gameObject;
            btnDetailClose ??= transform.Find("DetailOverlay/DetailBox/BtnClose")?.GetComponent<Button>();
            outputGrid ??= transform.Find("BuildingOutputSection/OutputGrid")?.GetComponent<UICommonItemAmountGridView>();
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            ResolveRefs();
            HideDetailOverlay();

            if (data is GameLogicManager.OneDayBalanceInfo info)
            {
                Refresh(info);
            }
        }

        public override void Show()
        {
            base.Show();
            ResolveRefs();
            HideDetailOverlay();
        }

        void Refresh(GameLogicManager.OneDayBalanceInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = "日结算";
            }

            RefreshSummary(info);
            outputGrid?.Refresh(info.TownFacilityOutputs);
        }

        void RefreshSummary(GameLogicManager.OneDayBalanceInfo info)
        {
            if (summaryText == null)
            {
                return;
            }

            long afterFallen = info.FromFallenAmount + info.AddFallenAmount;
            summaryText.text =
                $"沉沦人数：{info.FromFallenAmount} → {afterFallen}（+{info.AddFallenAmount}）\n" +
                $"\u83b7\u5f97\u6b32\u671b\u788e\u7247?{info.DesireShardAdded}\n" +
                $"\u6559\u56e2\u4fe1\u4ef0?+{info.CultFaithAdded}?{info.CultControlledTownCount} \u4e2a\u53d7\u63a7\u57ce\u9547 ? {info.CultTownDailyFaith}?";
        }

        void OnEnable()
        {
            ResolveRefs();
            WireButtons();
            HideDetailOverlay();
        }

        void WireButtons()
        {
            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveListener(Close);
                btnConfirm.onClick.AddListener(Close);
            }

            if (btnDetail != null)
            {
                btnDetail.onClick.RemoveListener(ShowDetailOverlay);
                btnDetail.onClick.AddListener(ShowDetailOverlay);
            }

            if (btnDetailClose != null)
            {
                btnDetailClose.onClick.RemoveListener(HideDetailOverlay);
                btnDetailClose.onClick.AddListener(HideDetailOverlay);
            }
        }

        void ShowDetailOverlay()
        {
            if (detailOverlay != null)
            {
                detailOverlay.SetActive(true);
            }
        }

        void HideDetailOverlay()
        {
            if (detailOverlay != null)
            {
                detailOverlay.SetActive(false);
            }
        }

        public void Close()
        {
            HideDetailOverlay();
            UIManager.Instance.HidePanel(PanelId);
        }

        public override bool OnCancel()
        {
            if (detailOverlay != null && detailOverlay.activeSelf)
            {
                HideDetailOverlay();
                return true;
            }

            Close();
            return true;
        }

        public override bool OnConfirm()
        {
            if (detailOverlay != null && detailOverlay.activeSelf)
            {
                HideDetailOverlay();
                return true;
            }

            Close();
            return true;
        }
    }
}
