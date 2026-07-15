using System.Collections.Generic;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class JingYuanCarriedPanel : PanelBase
    {
        public const string PanelIdConst = "JingYuanCarriedPanel";

        Transform _content;
        GameObject _template;
        TextMeshProUGUI _capacityText;
        TextMeshProUGUI _overflowText;
        PlayerJingYuanEssenceSystem _system;
        readonly List<GameObject> _cells = new();
        int _selectedIndex = -1;

        void Awake() => panelId = PanelIdConst;

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
            _overflowText = root?.Find("Overflow")?.GetComponent<TextMeshProUGUI>();
            var close = root?.Find("CloseButton")?.GetComponent<Button>();
            close?.onClick.AddListener(() => UIManager.Instance.HidePanel(PanelIdConst));
        }

        void Update()
        {
            if (_system != null && _system.IsTemporaryOverCapacity) RefreshOverflowText();
        }

        void RefreshContent()
        {
            if (_content == null || _template == null || _system == null) return;
            foreach (var cell in _cells) if (cell != null) Destroy(cell);
            _cells.Clear();
            _template.SetActive(false);
            RefreshOverflowText();
            for (var i = 0; i < _system.Temporary.Count; i++)
            {
                var essence = _system.Temporary[i];
                if (essence == null) continue;
                var index = i;
                var cell = Instantiate(_template, _content);
                cell.name = $"Essence_{essence.InstanceId}";
                cell.SetActive(true);
                _cells.Add(cell);
                BindCell(cell, essence, index);
            }
        }

        void RefreshOverflowText()
        {
            if (_capacityText != null) _capacityText.text = $"随身精元 {_system.Temporary.Count}/{_system.TemporaryCapacity}";
            if (_overflowText == null) return;
            if (!_system.IsTemporaryOverCapacity)
            {
                _overflowText.text = "随身精元未超限";
                _overflowText.color = Color.white;
                return;
            }
            _overflowText.text = $"超限 {_system.GetTemporaryOverflowCount()} 个，{Mathf.CeilToInt(_system.TemporaryOverflowRemainingSeconds)} 秒后分解超限区";
            _overflowText.color = new Color(1f, .55f, .35f, 1f);
        }

        void BindCell(GameObject cell, PremiumEssenceInstance essence, int index)
        {
            var image = cell.GetComponent<Image>();
            if (image != null) image.color = index == _selectedIndex ? new Color(.3f, .24f, .4f) : new Color(.14f, .18f, .22f);
            var label = cell.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var icon = cell.transform.Find("Icon")?.GetComponent<Image>();
            var info = JingYuanEssenceCatalog.GetTypeInfo(essence.TypeId);
            if (label != null) label.text = $"{info?.DisplayName ?? essence.TypeId.ToString()}\nLv {essence.DropLevel}  {essence.Concentration}%";
            if (icon != null && info != null) icon.sprite = SimpleResManager.Load<Sprite>($"Sprites/{info.IconPath}");
            var select = cell.GetComponent<Button>();
            select?.onClick.AddListener(() => SelectIndex(index));
            var equip = cell.transform.Find("EquipButton")?.GetComponent<Button>();
            equip?.onClick.AddListener(() => _system.TryEquip(essence.InstanceId));
        }

        void SelectIndex(int index)
        {
            if (_selectedIndex >= 0 && _selectedIndex != index)
            {
                _system.TrySwapTemporary(_selectedIndex, index);
                _selectedIndex = -1;
            }
            else
            {
                _selectedIndex = index;
            }
            RefreshContent();
        }
    }
}
