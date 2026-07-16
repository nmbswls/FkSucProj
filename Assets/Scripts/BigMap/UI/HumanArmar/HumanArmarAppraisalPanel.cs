using System;
using System.Collections.Generic;
using My.Config;
using My.Player;
using My.Player.Bag;
using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    /// <summary>
    /// The only UI entry point that can identify human armor. It is opened by Fei in Secret Base.
    /// </summary>
    public sealed class HumanArmarAppraisalPanel : PanelWithInput
    {
        public const string PanelIdConst = "HumanArmarAppraisalPanel";

        readonly List<GameObject> _cells = new();
        readonly List<Entry> _entries = new();
        PlayerInventorySystem _inventory;
        ItemStack _selected;
        string _selectedSource;
        Transform _content;
        GameObject _cellTemplate;
        TMP_Text _title;
        TMP_Text _source;
        TMP_Text _status;
        TMP_Text _result;
        readonly List<TMP_Text> _affixTexts = new();
        Button _appraiseButton;

        void Awake()
        {
            panelId = PanelIdConst;
            BindView();
        }

        public override void Show()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                UIManager.Instance.HidePanel(PanelIdConst);
                return;
            }

            base.Show();
            HumanArmarCatalog.BeginAppraisalSession();
            BindInventory(glm.playerDataManager?.InventorySystem);
            Refresh();
        }

        public override void Hide()
        {
            BindInventory(null);
            _selected = null;
            _selectedSource = null;
            HumanArmarCatalog.EndAppraisalSession();
            base.Hide();
        }

        void BindView()
        {
            var root = transform.Find("BuiltRoot") ?? transform;
            _content = root.Find("Grid/Viewport/Content") ?? root.Find("Grid");
            _cellTemplate = root.Find("CellTemplate")?.gameObject;
            _title = root.Find("Detail/Title")?.GetComponent<TMP_Text>();
            _source = root.Find("Detail/Source")?.GetComponent<TMP_Text>();
            _status = root.Find("Detail/Status")?.GetComponent<TMP_Text>();
            _result = root.Find("Detail/Result")?.GetComponent<TMP_Text>();
            for (int i = 1; i <= 4; i++)
            {
                var text = root.Find($"Detail/Affix{i}")?.GetComponent<TMP_Text>();
                if (text != null) _affixTexts.Add(text);
            }
            _appraiseButton = root.Find("Detail/AppraiseButton")?.GetComponent<Button>();
            _appraiseButton?.onClick.AddListener(AppraiseSelected);
            var close = root.Find("CloseButton")?.GetComponent<Button>();
            close?.onClick.AddListener(Close);
            if (_cellTemplate != null) _cellTemplate.SetActive(false);
        }

        void BindInventory(PlayerInventorySystem inventory)
        {
            if (_inventory == inventory) return;
            if (_inventory?.MainBag != null) _inventory.MainBag.EvOnBagUpdate -= Refresh;
            if (_inventory?.WarehouseBag != null) _inventory.WarehouseBag.EvOnBagUpdate -= Refresh;
            _inventory = inventory;
            if (_inventory?.MainBag != null) _inventory.MainBag.EvOnBagUpdate += Refresh;
            if (_inventory?.WarehouseBag != null) _inventory.WarehouseBag.EvOnBagUpdate += Refresh;
        }

        public void Refresh()
        {
            RebuildEntries();
            RebuildGrid();
            RefreshDetail();
        }

        void RebuildEntries()
        {
            _entries.Clear();
            AddEntries(_inventory?.MainBag, "随身包");
            AddEntries(_inventory?.WarehouseBag, "仓库");
            if (_selected != null && !_entries.Exists(x => ReferenceEquals(x.Stack, _selected)))
            {
                _selected = null;
                _selectedSource = null;
            }
            if (_selected == null && _entries.Count > 0)
            {
                _selected = _entries[0].Stack;
                _selectedSource = _entries[0].Source;
            }
        }

        void AddEntries(PlayerBag bag, string source)
        {
            if (bag == null) return;
            AddEntries(bag.NormalSlots, source);
            AddEntries(bag.ExtraSlots, source);
        }

        void AddEntries(List<ItemStack> slots, string source)
        {
            if (slots == null) return;
            foreach (var stack in slots)
            {
                if (stack == null || stack.IsEmpty || !HumanArmarCatalog.IsHumanArmar(stack.ItemID)) continue;
                _entries.Add(new Entry(stack, source));
            }
        }

        void RebuildGrid()
        {
            foreach (var cell in _cells) if (cell != null) Destroy(cell);
            _cells.Clear();
            if (_content == null || _cellTemplate == null) return;
            foreach (var entry in _entries)
            {
                var cell = Instantiate(_cellTemplate, _content);
                cell.SetActive(true);
                cell.name = $"Armar_{entry.Stack.ItemInstanceId}";
                var label = cell.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    var def = ItemCatalog.GetItemDef(entry.Stack.ItemID);
                    var instance = HumanArmarCatalog.GetInstance(entry.Stack);
                    label.text = $"{def?.DisplayName ?? entry.Stack.ItemID}\n{entry.Source}\n{(instance?.IsIdentified == true ? "已鉴定" : "未鉴定")}";
                }
                var button = cell.GetComponent<Button>();
                var captured = entry;
                button?.onClick.AddListener(() => Select(captured));
                _cells.Add(cell);
            }
        }

        void Select(Entry entry)
        {
            _selected = entry.Stack;
            _selectedSource = entry.Source;
            RefreshGridSelection();
            RefreshDetail();
        }

        void RefreshGridSelection()
        {
            for (int i = 0; i < _cells.Count && i < _entries.Count; i++)
            {
                var image = _cells[i].GetComponent<Image>();
                if (image != null) image.color = ReferenceEquals(_entries[i].Stack, _selected)
                    ? new Color(.42f, .28f, .16f, 1f) : new Color(.16f, .18f, .20f, 1f);
            }
        }

        void RefreshDetail()
        {
            var instance = HumanArmarCatalog.GetInstance(_selected);
            var def = _selected == null ? null : ItemCatalog.GetItemDef(_selected.ItemID);
            if (_title != null) _title.text = def?.DisplayName ?? "未选择护具";
            if (_source != null) _source.text = _selected == null ? string.Empty : $"来源：{_selectedSource}\n部件：{_selected.ItemID}";
            if (_status != null) _status.text = instance?.IsIdentified == true ? "鉴定状态：已鉴定" : "鉴定状态：未鉴定";
            if (_appraiseButton != null) _appraiseButton.interactable = instance != null && !instance.IsIdentified;
            if (_result != null)
            {
                _result.text = instance?.IsIdentified == true
                    ? "鉴定结果："
                    : BuildValuePreview(_selected);
            }
            var lines = instance?.IsIdentified == true ? HumanArmarCatalog.GetAffixDisplayLines(_selected) : Array.Empty<string>();
            for (int i = 0; i < _affixTexts.Count; i++)
                _affixTexts[i].text = i < lines.Count ? lines[i] : string.Empty;
        }

        static string BuildValuePreview(ItemStack stack)
        {
            if (stack == null) return "未选择护具";
            var progression = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem;
            long preview = progression?.GetFinalAttribute((int)EYCAttribute.HumanArmarValuePreview) ?? 0;
            if (preview <= 0) return "估值：需要学习模糊估值";

            long value = HumanArmarCatalog.GetPotentialMarketValue(stack);
            if (value <= 0) return "估值：暂时无法判断";
            long precision = progression?.GetFinalAttribute((int)EYCAttribute.HumanArmarValuePrecision) ?? 0;
            double error = precision > 0 ? .12 : .35;
            long min = Math.Max(0, (long)Math.Floor(value * (1d - error)));
            long max = (long)Math.Ceiling(value * (1d + error));
            return $"估值范围：{min} - {max}";
        }

        void AppraiseSelected()
        {
            if (_selected == null || !HumanArmarCatalog.TryIdentify(_selected)) return;
            Refresh();
        }

        void Close() => UIManager.Instance.HidePanel(PanelIdConst);

        public bool OnCancel()
        {
            Close();
            return true;
        }

        readonly struct Entry
        {
            public Entry(ItemStack stack, string source) { Stack = stack; Source = source; }
            public ItemStack Stack { get; }
            public string Source { get; }
        }
    }
}
