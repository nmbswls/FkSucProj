using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public sealed class RuneSlotView : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image Bg;
        public Image Icon;
        public Image LockOverlay;
        public Image AddOverlay;
        public Image SelectionFrame;
        public TextMeshProUGUI SlotLabel;
        public TextMeshProUGUI NameText;
        public Button ClickButton;

        RuneSlotBinder _binder;
        RuneInfoProvider _infoProvider;
        RuneLoadoutPanel _panel;
        RuneSlotVisualState _state;
        string _displayRuneId;
        bool _selected;

        static readonly Color NormalBg = new Color(0.14f, 0.12f, 0.20f, 1f);
        static readonly Color SelectedBg = new Color(0.42f, 0.34f, 0.14f, 1f);
        static readonly Color EmptyBg = new Color(0.10f, 0.09f, 0.15f, 1f);

        public RuneSlotBinder Binder => _binder;
        public RuneSlotVisualState State => _state;
        public string DisplayRuneId => _displayRuneId;

        void Awake()
        {
            _binder = GetComponent<RuneSlotBinder>();
            _infoProvider = GetComponent<RuneInfoProvider>();

            if (ClickButton != null)
            {
                ClickButton.onClick.AddListener(OnClick);
            }
        }

        public void BindPanel(RuneLoadoutPanel panel)
        {
            _panel = panel;
        }

        public void Refresh(PlayerRuneSystem runeSystem, bool selected)
        {
            if (_binder == null || runeSystem == null)
            {
                return;
            }

            _selected = selected;
            if (_binder.SlotKind == RuneSlotKind.Fixed)
            {
                RefreshFixedFromBinder(runeSystem);
                return;
            }

            RefreshEquippableFromBinder(runeSystem);
        }

        void RefreshFixedFromBinder(PlayerRuneSystem runeSystem)
        {
            string runeId = _binder.FixedRuneId;
            bool unlocked = !string.IsNullOrEmpty(runeId) && runeSystem.OwnsRune(runeId);
            _displayRuneId = unlocked ? runeId : string.Empty;
            _state = unlocked ? RuneSlotVisualState.Unlocked : RuneSlotVisualState.Locked;
            _infoProvider.SetFixedSlot(runeId, unlocked);
            ApplyCommonVisual(unlocked ? RuneCatalog.GetOrDefault(runeId) : null, _selected, showAdd: false);
            if (SlotLabel != null)
            {
                SlotLabel.text = "常驻";
            }

            if (NameText != null)
            {
                NameText.text = unlocked ? _infoProvider.GetDisplayName() : "???";
            }
        }

        void RefreshEquippableFromBinder(PlayerRuneSystem runeSystem)
        {
            var slot = _binder.EquipSlot;
            bool slotUnlocked = RuneLoadoutPanel.IsEquipSlotUnlocked(runeSystem, slot);
            string equippedRuneId = runeSystem.GetEquipped(slot);
            _displayRuneId = equippedRuneId;
            if (!slotUnlocked)
            {
                _state = RuneSlotVisualState.Locked;
            }
            else if (string.IsNullOrEmpty(equippedRuneId))
            {
                _state = RuneSlotVisualState.Empty;
            }
            else
            {
                _state = RuneSlotVisualState.Equipped;
            }

            _infoProvider.SetEquippableSlot(slot, equippedRuneId, slotUnlocked);
            var def = RuneCatalog.GetOrDefault(equippedRuneId);
            ApplyCommonVisual(def, _selected, showAdd: slotUnlocked && string.IsNullOrEmpty(equippedRuneId));
            if (SlotLabel != null)
            {
                SlotLabel.text = RuneCatalog.GetSlotDisplayName(slot);
            }

            if (NameText != null)
            {
                if (!slotUnlocked)
                {
                    NameText.text = "未解锁";
                }
                else if (def != null)
                {
                    NameText.text = def.Name;
                }
                else
                {
                    NameText.text = "空";
                }
            }
        }

        void ApplyCommonVisual(RuneData def, bool selected, bool showAdd)
        {
            if (Bg != null)
            {
                Bg.color = selected ? SelectedBg : (_state == RuneSlotVisualState.Empty ? EmptyBg : NormalBg);
            }

            if (SelectionFrame != null)
            {
                SelectionFrame.gameObject.SetActive(selected);
            }

            if (LockOverlay != null)
            {
                LockOverlay.gameObject.SetActive(_state == RuneSlotVisualState.Locked);
            }

            if (AddOverlay != null)
            {
                AddOverlay.gameObject.SetActive(showAdd);
            }

            Sprite sprite = null;
            if (def != null && !string.IsNullOrEmpty(def.Icon))
            {
                sprite = SimpleResManager.Load<Sprite>(def.Icon);
            }

            if (Icon != null)
            {
                Icon.sprite = sprite;
                Icon.enabled = sprite != null;
                Icon.color = _state == RuneSlotVisualState.Locked ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
            }
        }

        void OnClick()
        {
            _panel?.OnSlotClicked(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            var ctrl = RuneDragDropController.Instance;
            if (ctrl == null || !ctrl.IsDragging || ctrl.Payload == null)
            {
                return;
            }

            if (_binder == null || _binder.SlotKind != RuneSlotKind.Equippable)
            {
                return;
            }

            if (_state == RuneSlotVisualState.Locked)
            {
                return;
            }

            _panel?.TryEquipFromDrag(_binder.EquipSlot, ctrl.Payload.RuneId, ctrl);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_binder == null || _binder.SlotKind != RuneSlotKind.Equippable)
            {
                return;
            }

            if (_state != RuneSlotVisualState.Equipped || string.IsNullOrEmpty(_displayRuneId))
            {
                return;
            }

            RuneDragDropController.Instance?.BeginDrag(new RuneDragPayload
            {
                RuneId = _displayRuneId,
                SourceType = RuneDragSourceType.EquipSlot,
                SourceEquipSlot = _binder.EquipSlot,
            });
        }

        public void OnDrag(PointerEventData eventData)
        {
            RuneDragDropController.Instance?.UpdateDrag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            RuneDragDropController.Instance?.EndDrag(eventData.position);
        }
    }
}
