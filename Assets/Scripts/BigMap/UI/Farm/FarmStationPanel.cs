using System.Collections.Generic;
using My;
using My.Farm;
using My.Player;
using My.Player.Bag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 农业小站独特面板：播种规划、派工收割、产物仓（prefab 驱动）
    public sealed class FarmStationPanel : PanelBase
    {
        public const string PanelIdConst = "FarmStationPanel";

        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] TMP_InputField workforceInput;
        [SerializeField] Button openWarehouseButton;
        [SerializeField] Button closeButton;
        [SerializeField] FarmStationPlanRowView[] planRows;
        [SerializeField] FarmProduceWarehouseLootAdapter warehouseAdapter;

        string _logicAreaId = FarmCatalog.DefaultLogicAreaId;

        public static FarmStationPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as FarmStationPanel;
            }
        }

        public static void Open(string logicAreaId)
        {
            var ui = UIManager.Instance;
            if (ui == null)
            {
                return;
            }

            var panel = ui.ShowPanel(PanelIdConst) as FarmStationPanel;
            if (panel == null)
            {
                return;
            }

            panel._logicAreaId = string.IsNullOrEmpty(logicAreaId)
                ? FarmCatalog.DefaultLogicAreaId
                : logicAreaId;
            panel.Refresh();
        }

        public static void Close()
        {
            UIManager.Instance?.HidePanel(PanelIdConst);
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelIdConst;
            }

            layer = UILayer.Popup;

            if (workforceInput != null)
            {
                workforceInput.onEndEdit.RemoveAllListeners();
                workforceInput.onEndEdit.AddListener(OnWorkforceEdit);
            }

            if (openWarehouseButton != null)
            {
                openWarehouseButton.onClick.RemoveAllListeners();
                openWarehouseButton.onClick.AddListener(OpenWarehouse);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            WirePlanRows();
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (data is string areaId && !string.IsNullOrEmpty(areaId))
            {
                _logicAreaId = areaId;
            }

            Refresh();
        }

        void WirePlanRows()
        {
            if (planRows == null)
            {
                return;
            }

            for (int i = 0; i < planRows.Length; i++)
            {
                var row = planRows[i];
                if (row == null)
                {
                    continue;
                }

                row.BindEdit(OnPlanEdit);
            }
        }

        void OnWorkforceEdit(string value)
        {
            if (!int.TryParse(value, out int n))
            {
                return;
            }

            MainGameManager.Instance?.gameLogicManager?.farmSystem?.SetHarvestWorkforce(_logicAreaId, n);
            Refresh();
        }

        void OnPlanEdit(string cropId, string value)
        {
            if (string.IsNullOrEmpty(cropId) || !int.TryParse(value, out int n))
            {
                return;
            }

            MainGameManager.Instance?.gameLogicManager?.farmSystem?.SetPlanTarget(_logicAreaId, cropId, n);
            Refresh();
        }

        void OpenWarehouse()
        {
            if (warehouseAdapter == null)
            {
                warehouseAdapter = GetComponent<FarmProduceWarehouseLootAdapter>();
            }

            if (warehouseAdapter == null)
            {
                return;
            }

            warehouseAdapter.LogicAreaId = _logicAreaId;
            UIOrchestrator.Instance?.TryEnterLootDetailMode(warehouseAdapter);
        }

        public void Refresh()
        {
            var farm = MainGameManager.Instance?.gameLogicManager?.farmSystem;
            if (farm == null)
            {
                return;
            }

            var persist = farm.GetOrCreateTownFarm(_logicAreaId);
            bool built = farm.IsFarmStationBuilt(_logicAreaId);
            if (statusText != null)
            {
                statusText.text = built
                    ? "小站已建成 · 日结算自动补种/派工收割/镇民养护"
                    : "小站未建成（仅手种手收）";
            }

            if (workforceInput != null && !workforceInput.isFocused)
            {
                workforceInput.text = persist.HarvestWorkforce.ToString();
            }

            if (planRows == null)
            {
                return;
            }

            for (int i = 0; i < planRows.Length; i++)
            {
                var row = planRows[i];
                if (row == null || string.IsNullOrEmpty(row.CropId))
                {
                    continue;
                }

                int target = 0;
                for (int p = 0; p < persist.AutoPlantPlan.Count; p++)
                {
                    if (persist.AutoPlantPlan[p].CropId == row.CropId)
                    {
                        target = persist.AutoPlantPlan[p].TargetCount;
                        break;
                    }
                }

                row.SetTarget(target);
            }
        }
    }

    // prefab 内预置的规划行
    public sealed class FarmStationPlanRowView : MonoBehaviour
    {
        [SerializeField] string cropId;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TMP_InputField targetInput;

        public string CropId => cropId;

        public void BindEdit(System.Action<string, string> onEdit)
        {
            if (nameText != null)
            {
                var crop = FarmCatalog.GetCrop(cropId);
                nameText.text = crop?.DisplayName ?? cropId;
            }

            if (targetInput == null || onEdit == null)
            {
                return;
            }

            string id = cropId;
            targetInput.onEndEdit.RemoveAllListeners();
            targetInput.onEndEdit.AddListener(v => onEdit(id, v));
        }

        public void SetTarget(int target)
        {
            if (targetInput != null && !targetInput.isFocused)
            {
                targetInput.text = target.ToString();
            }
        }
    }

    public sealed class FarmProduceWarehouseLootAdapter : MonoBehaviour, ILootableObj
    {
        public string LogicAreaId = FarmCatalog.DefaultLogicAreaId;
        public event System.Action<int> EnOnUnrealed;

        FarmSystem Farm => MainGameManager.Instance?.gameLogicManager?.farmSystem;

        public List<ItemStack> LootItems
        {
            get
            {
                var bag = Farm?.GetProduceWarehouse(LogicAreaId);
                return bag != null ? bag.NormalSlots : new List<ItemStack>();
            }
        }

        public bool IsRevealed(int itemIdx) => true;
        public void TickUnReveal(float dt) { }
        public int GetCurrUnrealed() => -1;

        public void RemoveFromIndex(int index, int count)
        {
            var bag = Farm?.GetProduceWarehouse(LogicAreaId);
            if (bag == null || index < 0 || index >= bag.NormalSlots.Count)
            {
                return;
            }

            var stack = bag.NormalSlots[index];
            if (stack == null || stack.IsEmpty)
            {
                return;
            }

            stack.Count -= count;
            if (stack.Count <= 0)
            {
                bag.NormalSlots[index] = null;
            }

            bag.CompactPackPrimary();
        }

        public EContainerType GetContainerType() => EContainerType.LootPoint;
        public IItemContainer GetLootItemContainer() => Farm?.GetProduceWarehouse(LogicAreaId);
        public void TryUseLootPoint() { }
    }
}
