using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class JingYuanWarehousePanel : PanelBase
    {
        public const string PanelIdConst = "JingYuanWarehousePanel";

        Transform _content;
        GameObject _template;
        TextMeshProUGUI _capacityText;
        PlayerJingYuanEssenceSystem _system;
        readonly List<GameObject> _cells = new();

        void Awake()
        {
            panelId = PanelIdConst;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            BindSystem();
            BindView();
            RefreshContent();
        }

        public override void Show()
        {
            base.Show();
            BindSystem();
            BindView();
            RefreshContent();
        }

        public override void Hide()
        {
            if (_system != null) _system.EventOnChanged -= RefreshContent;
            _system = null;
            base.Hide();
        }

        void BindSystem()
        {
            var next = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.JingYuanEssenceSystem;
            if (_system == next) return;
            if (_system != null) _system.EventOnChanged -= RefreshContent;
            _system = next;
            if (_system != null) _system.EventOnChanged += RefreshContent;
        }

        void BindView()
        {
            if (_content != null) return;
            var root = transform.Find("BuiltRoot");
            _content = root?.Find("Scroll/Viewport/Content");
            _template = root?.Find("CellTemplate")?.gameObject;
            _capacityText = root?.Find("Capacity")?.GetComponent<TextMeshProUGUI>();
            var close = root?.Find("CloseButton")?.GetComponent<Button>();
            if (close != null)
            {
                close.onClick.RemoveAllListeners();
                close.onClick.AddListener(() => UIManager.Instance.HidePanel(PanelIdConst));
            }
        }

        void RefreshContent()
        {
            if (_content == null || _template == null || _system == null) return;
            foreach (var cell in _cells) if (cell != null) Destroy(cell);
            _cells.Clear();
            _template.SetActive(false);
            if (_capacityText != null) _capacityText.text = $"Warehouse {_system.Warehouse.Count}/{_system.WarehouseCapacity}";
            foreach (var essence in _system.Warehouse)
            {
                if (essence == null) continue;
                var cell = Instantiate(_template, _content);
                cell.name = $"Essence_{essence.InstanceId}";
                cell.SetActive(true);
                _cells.Add(cell);
                BindCell(cell, essence);
            }
        }

        static void BindCell(GameObject cell, PremiumEssenceInstance essence)
        {
            var label = cell.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var icon = cell.transform.Find("Icon")?.GetComponent<Image>();
            var info = JingYuanEssenceCatalog.GetTypeInfo(essence.TypeId);
            if (label != null)
            {
                label.text = $"{info?.DisplayName ?? essence.TypeId.ToString()}\nLv {essence.DropLevel}  Q{essence.QualityTier}\nConcentration {essence.Concentration}%\nShelf life {essence.RemainingShelfLifeDays}d";
            }
            if (icon != null && info != null && !string.IsNullOrEmpty(info.IconPath))
            {
                icon.sprite = SimpleResManager.Load<Sprite>($"Sprites/{info.IconPath}");
                icon.preserveAspect = true;
            }
        }
    }
}
