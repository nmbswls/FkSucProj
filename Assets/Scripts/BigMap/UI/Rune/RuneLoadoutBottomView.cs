using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using DG.Tweening;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    // 底部辅助面板：与 detail 脱钩，负责 owned 选择器与槽位锁定提示
    public sealed class RuneLoadoutBottomView : MonoBehaviour
    {
        const float SlideDuration = 0.28f;
        const float HiddenExtraOffset = 24f;
        const string OwnedCellPrefabPath = "UI/Prefabs/PlayerProgressionHubPanelSub/RuneOwnedCell";

        enum BottomMode
        {
            None,
            OwnedPicker,
            SlotLocked,
        }

        [SerializeField] RectTransform areaRoot;
        [SerializeField] ScrollRect ownedScroll;
        [SerializeField] RectTransform ownedGrid;
        [SerializeField] RuneOwnedCell ownedCellTemplate;
        [SerializeField] TextMeshProUGUI ownedEmptyHint;

        [SerializeField] GameObject lockedContentRoot;
        [SerializeField] TextMeshProUGUI lockedHintText;

        readonly List<RuneOwnedCell> _ownedCells = new();

        BottomMode _mode = BottomMode.None;
        ERuneEquipSlot _targetEquipSlot = ERuneEquipSlot.None;
        string _selectedOwnedRuneId;
        RuneLoadoutPanel _panel;
        bool _visible;

        Vector2 _shownAnchoredPos;
        Vector2 _hiddenAnchoredPos;
        Tweener _slideTween;

        public bool IsVisible => _visible;
        public RectTransform AreaRoot => areaRoot != null ? areaRoot : transform as RectTransform;

        void Awake()
        {
            if (areaRoot == null)
            {
                areaRoot = transform as RectTransform;
            }

            EnsureOwnedCellTemplate();
            if (ownedCellTemplate != null)
            {
                ownedCellTemplate.gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            KillTween();
        }

        public void CacheLayoutAnchors()
        {
            if (AreaRoot == null)
            {
                return;
            }

            var savedPos = AreaRoot.anchoredPosition;
            _shownAnchoredPos = Vector2.zero;
            AreaRoot.anchoredPosition = _shownAnchoredPos;
            LayoutRebuilder.ForceRebuildLayoutImmediate(AreaRoot);

            float height = AreaRoot.rect.height;
            if (height <= 1f)
            {
                height = 156f;
            }

            _hiddenAnchoredPos = _shownAnchoredPos + new Vector2(0f, -(height + HiddenExtraOffset));
            AreaRoot.anchoredPosition = savedPos;
        }

        public void ResetVisualState()
        {
            KillTween();
            _visible = false;
            _mode = BottomMode.None;
            _targetEquipSlot = ERuneEquipSlot.None;
            _selectedOwnedRuneId = null;
            ClearOwnedCells();
            SetModeRootActive(BottomMode.None);

            if (AreaRoot != null)
            {
                AreaRoot.gameObject.SetActive(true);
                AreaRoot.anchoredPosition = _hiddenAnchoredPos;
            }
        }

        public void ShowOwnedPicker(RuneLoadoutPanel panel, ERuneEquipSlot equipSlot, PlayerRuneSystem runeSystem)
        {
            _panel = panel;
            _targetEquipSlot = equipSlot;
            _mode = BottomMode.OwnedPicker;
            SetModeRootActive(_mode);
            RefreshOwnedGrid(runeSystem);

            if (_visible)
            {
                if (AreaRoot != null)
                {
                    AreaRoot.anchoredPosition = _shownAnchoredPos;
                }

                return;
            }

            PlayShow();
        }

        public void ShowSlotLocked(ERuneEquipSlot equipSlot, PlayerRuneSystem runeSystem)
        {
            _panel = null;
            _targetEquipSlot = equipSlot;
            _mode = BottomMode.SlotLocked;
            SetModeRootActive(_mode);

            if (lockedHintText != null)
            {
                lockedHintText.text = RuneCatalog.GetEquipSlotLockHint(equipSlot, runeSystem);
            }

            if (_visible)
            {
                if (AreaRoot != null)
                {
                    AreaRoot.anchoredPosition = _shownAnchoredPos;
                }

                return;
            }

            PlayShow();
        }

        public void Refresh(PlayerRuneSystem runeSystem)
        {
            if (!_visible)
            {
                return;
            }

            switch (_mode)
            {
                case BottomMode.OwnedPicker:
                    RefreshOwnedGrid(runeSystem);
                    break;
                case BottomMode.SlotLocked:
                    if (lockedHintText != null)
                    {
                        lockedHintText.text = RuneCatalog.GetEquipSlotLockHint(_targetEquipSlot, runeSystem);
                    }

                    break;
            }
        }

        public void Hide(bool animate = true)
        {
            if (!_visible)
            {
                _mode = BottomMode.None;
                SetModeRootActive(BottomMode.None);
                ClearOwnedCells();
                return;
            }

            _visible = false;
            if (!animate)
            {
                KillTween();
                if (AreaRoot != null)
                {
                    AreaRoot.anchoredPosition = _hiddenAnchoredPos;
                }

                _mode = BottomMode.None;
                SetModeRootActive(BottomMode.None);
                ClearOwnedCells();
                return;
            }

            KillTween();
            _slideTween = AreaRoot
                .DOAnchorPos(_hiddenAnchoredPos, SlideDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _mode = BottomMode.None;
                    SetModeRootActive(BottomMode.None);
                    ClearOwnedCells();
                });
        }

        public void NotifyOwnedRuneSelected(string runeId)
        {
            _selectedOwnedRuneId = runeId;
        }

        void SetModeRootActive(BottomMode mode)
        {
            if (ownedScroll != null)
            {
                ownedScroll.gameObject.SetActive(mode == BottomMode.OwnedPicker);
            }

            if (ownedEmptyHint != null && mode != BottomMode.OwnedPicker)
            {
                ownedEmptyHint.gameObject.SetActive(false);
            }

            if (lockedContentRoot != null)
            {
                lockedContentRoot.SetActive(mode == BottomMode.SlotLocked);
            }
        }

        void PlayShow()
        {
            if (AreaRoot == null)
            {
                return;
            }

            _visible = true;
            KillTween();
            AreaRoot.gameObject.SetActive(true);
            AreaRoot.anchoredPosition = _hiddenAnchoredPos;
            _slideTween = AreaRoot
                .DOAnchorPos(_shownAnchoredPos, SlideDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        void RefreshOwnedGrid(PlayerRuneSystem runeSystem)
        {
            if (_mode != BottomMode.OwnedPicker || runeSystem == null)
            {
                ClearOwnedCells();
                return;
            }

            if (!EnsureOwnedCellTemplate())
            {
                return;
            }

            var owned = runeSystem.GetOwnedByType(ERuneType.Equippable)
                .OrderBy(x => x.RuneId)
                .ToList();

            ClearOwnedCells();
            for (int i = 0; i < owned.Count; i++)
            {
                var def = owned[i];
                var cellGo = Instantiate(ownedCellTemplate.gameObject, ownedGrid, false);
                cellGo.SetActive(true);
                var cell = cellGo.GetComponent<RuneOwnedCell>();
                if (cell == null)
                {
                    Destroy(cellGo);
                    continue;
                }

                bool canEquip = def.EquipSlot == _targetEquipSlot;
                string equippedId = runeSystem.GetEquipped(def.EquipSlot);
                bool isEquipped = equippedId == def.RuneId;
                bool selected = _selectedOwnedRuneId == def.RuneId;
                cell.Bind(_panel, def, isEquipped, selected, i, canEquip);
                _ownedCells.Add(cell);
            }

            if (ownedEmptyHint != null)
            {
                ownedEmptyHint.gameObject.SetActive(owned.Count == 0);
                ownedEmptyHint.text = owned.Count == 0 ? "暂无已拥有的装配符文" : string.Empty;
            }

            RebuildOwnedGridLayout();
        }

        bool EnsureOwnedCellTemplate()
        {
            if (ownedCellTemplate != null)
            {
                return true;
            }

            var prefab = Resources.Load<GameObject>(OwnedCellPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[RuneLoadoutBottomView] Failed to load RuneOwnedCell prefab.");
                return false;
            }

            ownedCellTemplate = prefab.GetComponent<RuneOwnedCell>();
            if (ownedCellTemplate == null)
            {
                Debug.LogError("[RuneLoadoutBottomView] RuneOwnedCell prefab missing RuneOwnedCell component.");
            }

            return ownedCellTemplate != null;
        }

        void RebuildOwnedGridLayout()
        {
            if (ownedGrid == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(ownedGrid);

            if (ownedScroll != null)
            {
                if (ownedScroll.content != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(ownedScroll.content);
                }

                if (ownedScroll.viewport != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(ownedScroll.viewport);
                }

                ownedScroll.verticalNormalizedPosition = 1f;
            }
        }

        void ClearOwnedCells()
        {
            foreach (var cell in _ownedCells)
            {
                if (cell != null)
                {
                    Destroy(cell.gameObject);
                }
            }

            _ownedCells.Clear();
        }

        void KillTween()
        {
            if (_slideTween != null && _slideTween.IsActive())
            {
                _slideTween.Kill();
            }

            _slideTween = null;
            AreaRoot?.DOKill();
        }
    }
}
