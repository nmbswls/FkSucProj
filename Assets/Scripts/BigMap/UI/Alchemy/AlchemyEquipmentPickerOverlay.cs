using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Alchemy
{
    public sealed class AlchemyEquipmentPickerOverlay : MonoBehaviour
    {
        public enum PickerMode
        {
            Furnace,
            Tool,
        }

        [SerializeField] GameObject root;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] Button closeButton;
        [SerializeField] RectTransform listRoot;
        [SerializeField] AlchemyEquipmentPickerCell cellTemplate;
        [SerializeField] Button confirmButton;

        readonly List<AlchemyEquipmentPickerCell> _cells = new();
        readonly List<string> _toolSelection = new();
        readonly List<string> _ownedToolIds = new();

        PickerMode _mode;
        Action<string> _onFurnacePicked;
        Action<IReadOnlyList<string>> _onToolsPicked;
        string _selectedFurnaceId;

        void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(ConfirmTools);
            }

            if (cellTemplate != null)
            {
                cellTemplate.gameObject.SetActive(false);
            }

            Hide();
        }

        public void ShowFurnacePicker(
            IReadOnlyList<string> furnaceIds,
            string selectedFurnaceId,
            Action<string> onPicked)
        {
            _mode = PickerMode.Furnace;
            _onFurnacePicked = onPicked;
            _onToolsPicked = null;
            _selectedFurnaceId = selectedFurnaceId;
            if (titleText != null)
            {
                titleText.text = "选择炼金炉";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(false);
            }

            Show();
            RebuildFurnaceCells(furnaceIds);
        }

        public void ShowToolPicker(
            IReadOnlyList<string> ownedToolIds,
            IReadOnlyList<string> selectedToolIds,
            Action<IReadOnlyList<string>> onPicked)
        {
            _mode = PickerMode.Tool;
            _onToolsPicked = onPicked;
            _onFurnacePicked = null;
            _ownedToolIds.Clear();
            _toolSelection.Clear();
            if (ownedToolIds != null)
            {
                for (int i = 0; i < ownedToolIds.Count; i++)
                {
                    var toolId = ownedToolIds[i];
                    if (!string.IsNullOrEmpty(toolId))
                    {
                        _ownedToolIds.Add(toolId);
                    }
                }
            }

            if (selectedToolIds != null)
            {
                for (int i = 0; i < selectedToolIds.Count; i++)
                {
                    var toolId = selectedToolIds[i];
                    if (!string.IsNullOrEmpty(toolId) && !_toolSelection.Contains(toolId))
                    {
                        _toolSelection.Add(toolId);
                    }
                }
            }

            if (titleText != null)
            {
                titleText.text = "选择工具";
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(true);
            }

            Show();
            RebuildToolCells();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public bool IsOpen => root != null && root.activeSelf;

        void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        void ConfirmTools()
        {
            if (_mode != PickerMode.Tool)
            {
                Hide();
                return;
            }

            _onToolsPicked?.Invoke(_toolSelection);
            Hide();
        }

        void RebuildFurnaceCells(IReadOnlyList<string> furnaceIds)
        {
            ClearCells();
            if (cellTemplate == null || listRoot == null || furnaceIds == null)
            {
                return;
            }

            for (int i = 0; i < furnaceIds.Count; i++)
            {
                var furnaceId = furnaceIds[i];
                var furnace = My.Config.AlchemyCatalog.GetFurnace(furnaceId);
                string name = furnace?.DisplayName ?? furnaceId;
                string hint = furnace != null ? $"素材格 {furnace.MaxMaterialSlots}" : string.Empty;
                var cell = Instantiate(cellTemplate, listRoot);
                cell.gameObject.SetActive(true);
                cell.Bind(furnaceId, name, hint, furnaceId == _selectedFurnaceId, OnFurnaceCellPicked);
                _cells.Add(cell);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
        }

        void RebuildToolCells()
        {
            ClearCells();
            if (cellTemplate == null || listRoot == null)
            {
                return;
            }

            AddToolCell(string.Empty, "无工具", "不使用任何工具", _toolSelection.Count == 0);
            for (int i = 0; i < _ownedToolIds.Count; i++)
            {
                var toolId = _ownedToolIds[i];
                var tool = My.Config.AlchemyCatalog.GetTool(toolId);
                string name = tool?.DisplayName ?? toolId;
                bool selected = _toolSelection.Contains(toolId);
                AddToolCell(toolId, name, string.Empty, selected);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
        }

        void AddToolCell(string toolId, string displayName, string hint, bool selected)
        {
            var cell = Instantiate(cellTemplate, listRoot);
            cell.gameObject.SetActive(true);
            cell.Bind(toolId, displayName, hint, selected, OnToolCellPicked);
            _cells.Add(cell);
        }

        void OnFurnaceCellPicked(string furnaceId)
        {
            _onFurnacePicked?.Invoke(furnaceId);
            Hide();
        }

        void OnToolCellPicked(string toolId)
        {
            if (_mode != PickerMode.Tool)
            {
                return;
            }

            if (string.IsNullOrEmpty(toolId))
            {
                _toolSelection.Clear();
                RebuildToolCells();
                return;
            }

            if (_toolSelection.Contains(toolId))
            {
                _toolSelection.Remove(toolId);
            }
            else
            {
                _toolSelection.Add(toolId);
            }

            RebuildToolCells();
        }

        void ClearCells()
        {
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i] != null)
                {
                    Destroy(_cells[i].gameObject);
                }
            }

            _cells.Clear();
        }
    }
}
