using System.Collections.Generic;
using My;
using My.Config;
using My.Farm;
using My.Player;
using TMPro;
using UnityEngine;

namespace My.UI
{
    // 播种模式种子栏：使用 Resources UI Prefab，逻辑模仿人类道具栏显隐切换
    public sealed class FarmSeedBarPanel : PanelBase
    {
        public const string PanelIdConst = "FarmSeedBarPanel";

        [SerializeField] ItemBarCenterItemView centerItemView;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI hintText;
        [SerializeField] FarmSeedSlotView[] seedSlots;

        public static FarmSeedBarPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as FarmSeedBarPanel;
            }
        }

        public static void TryShow()
        {
            var ui = UIManager.Instance;
            if (ui == null)
            {
                return;
            }

            var panel = ui.ShowPanel(PanelIdConst) as FarmSeedBarPanel;
            panel?.BindFarmEvents(true);
            panel?.Refresh();
        }

        public static void TryHide()
        {
            var panel = Instance;
            panel?.BindFarmEvents(false);
            UIManager.Instance?.HidePanel(PanelIdConst);
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.HUD;

            if (titleText != null)
            {
                titleText.text = "播种模式";
            }

            if (hintText != null)
            {
                hintText.text = "滚轮切换种子 · 左键播前方格 · X退出";
            }

            WireSlotButtons();
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            Refresh();
        }

        public override void Show()
        {
            base.Show();
            BindFarmEvents(true);
            Refresh();
        }

        public override void Hide()
        {
            BindFarmEvents(false);
            base.Hide();
        }

        void WireSlotButtons()
        {
            if (seedSlots == null)
            {
                return;
            }

            for (int i = 0; i < seedSlots.Length; i++)
            {
                var slot = seedSlots[i];
                if (slot == null || slot.Button == null)
                {
                    continue;
                }

                int captured = i;
                slot.Button.onClick.RemoveAllListeners();
                slot.Button.onClick.AddListener(() => OnClickSlot(captured));
            }
        }

        void BindFarmEvents(bool bind)
        {
            var farm = MainGameManager.Instance?.gameLogicManager?.farmSystem;
            if (farm == null)
            {
                return;
            }

            farm.EvOnFarmChanged -= Refresh;
            if (bind)
            {
                farm.EvOnFarmChanged += Refresh;
            }
        }

        void OnClickSlot(int index)
        {
            var farm = MainGameManager.Instance?.gameLogicManager?.farmSystem;
            if (farm == null || !farm.IsPlantingMode)
            {
                return;
            }

            var bag = farm.GetSeedBasket(farm.PlantingLogicAreaId);
            var seeds = ListSeeds(bag);
            if (index < 0 || index >= seeds.Count)
            {
                return;
            }

            farm.SelectSeed(seeds[index]);
            Refresh();
        }

        public void Refresh()
        {
            var farm = MainGameManager.Instance?.gameLogicManager?.farmSystem;
            if (farm == null || !farm.IsPlantingMode)
            {
                return;
            }

            var bag = farm.GetSeedBasket(farm.PlantingLogicAreaId);
            var seeds = ListSeeds(bag);
            string selectedId = farm.SelectedSeedItemId;
            if (string.IsNullOrEmpty(selectedId) && seeds.Count > 0)
            {
                selectedId = seeds[0];
                farm.SelectSeed(selectedId);
            }

            if (centerItemView != null)
            {
                long count = string.IsNullOrEmpty(selectedId) ? 0 : bag.GetItemCount(selectedId);
                centerItemView.RefreshItem(selectedId, count, count > 0);
            }

            if (seedSlots == null)
            {
                return;
            }

            for (int i = 0; i < seedSlots.Length; i++)
            {
                var slot = seedSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (i >= seeds.Count)
                {
                    slot.SetEmpty();
                    continue;
                }

                var id = seeds[i];
                var def = ItemCatalog.GetItemDef(id);
                long count = bag.GetItemCount(id);
                bool selected = id == selectedId;
                slot.Bind(
                    def?.DisplayName ?? id,
                    ItemCatalog.GetIcon(id),
                    count,
                    selected,
                    count > 0);
            }
        }

        static List<string> ListSeeds(PlayerBag bag)
        {
            var result = new List<string>();
            if (bag == null)
            {
                return result;
            }

            for (int i = 0; i < bag.NormalSlots.Count; i++)
            {
                var s = bag.NormalSlots[i];
                if (s == null || s.IsEmpty)
                {
                    continue;
                }

                if (!result.Contains(s.ItemID))
                {
                    result.Add(s.ItemID);
                }
            }

            return result;
        }
    }
}
