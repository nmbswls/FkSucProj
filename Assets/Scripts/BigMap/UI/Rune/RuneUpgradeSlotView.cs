using System;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Rune
{
    public sealed class RuneUpgradeSlotView : MonoBehaviour
    {
        [SerializeField] Button clickButton;
        [SerializeField] Image nodeBackground;
        [SerializeField] Image iconImage;
        [SerializeField] Image lockOverlay;
        [SerializeField] Image selectionFrame;
        [SerializeField] TextMeshProUGUI labelText;
        [SerializeField] Color lockedColor = new(0.35f, 0.35f, 0.4f, 1f);
        [SerializeField] Color availableColor = new(0.9f, 0.75f, 0.2f, 1f);
        [SerializeField] Color unlockedColor = new(0.35f, 0.85f, 0.45f, 1f);

        RuneUpgradeInfo _def;
        int _layoutSlot;
        bool _isInitial;
        PlayerRuneSystem _runeSystem;
        Action<int, string> _onClicked;

        public int LayoutSlot => _layoutSlot;
        public string UpgradeId => _def?.UpgradeId;

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        public void Bind(
            int layoutSlot,
            RuneUpgradeNodeView nodeView,
            bool isInitial,
            PlayerRuneSystem runeSystem,
            Action<int, string> onClicked)
        {
            _layoutSlot = layoutSlot;
            _def = nodeView?.Def;
            _isInitial = isInitial;
            _runeSystem = runeSystem;
            _onClicked = onClicked;

            if (labelText != null)
            {
                string name = _def != null && !string.IsNullOrEmpty(_def.Name) ? _def.Name : $"#{layoutSlot}";
                if (isInitial)
                {
                    name = $"[初始] {name}";
                }

                labelText.text = name;
            }

            if (iconImage != null)
            {
                Sprite sprite = null;
                if (_def != null && !string.IsNullOrEmpty(_def.Icon))
                {
                    sprite = SimpleResManager.Load<Sprite>(_def.Icon);
                }

                iconImage.sprite = sprite;
                iconImage.enabled = sprite != null;
            }

            var state = nodeView?.State ?? ERuneUpgradeNodeState.Locked;
            if (isInitial && state != ERuneUpgradeNodeState.Unlocked)
            {
                state = ERuneUpgradeNodeState.Unlocked;
            }

            Refresh(state, false);
            EnsureHoverProvider(state);
        }

        public void Refresh(ERuneUpgradeNodeState state, bool selected)
        {
            if (nodeBackground != null)
            {
                nodeBackground.color = state switch
                {
                    ERuneUpgradeNodeState.Available => availableColor,
                    ERuneUpgradeNodeState.Unlocked => unlockedColor,
                    _ => lockedColor,
                };
            }

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(state == ERuneUpgradeNodeState.Locked);
            }

            if (selectionFrame != null)
            {
                selectionFrame.gameObject.SetActive(selected);
            }

            if (clickButton != null)
            {
                clickButton.interactable = state != ERuneUpgradeNodeState.Locked;
            }

            EnsureHoverProvider(state);
        }

        void EnsureHoverProvider(ERuneUpgradeNodeState state)
        {
            if (_def == null || string.IsNullOrEmpty(_def.UpgradeId))
            {
                return;
            }

            var hover = GetComponent<RuneUpgradeHoverProvider>();
            if (hover == null)
            {
                hover = gameObject.AddComponent<RuneUpgradeHoverProvider>();
            }

            hover.Configure(_def.UpgradeId, state, _isInitial, _runeSystem);

            if (nodeBackground != null)
            {
                nodeBackground.raycastTarget = true;
            }
        }

        void OnClicked()
        {
            if (_def == null || string.IsNullOrEmpty(_def.UpgradeId))
            {
                return;
            }

            _onClicked?.Invoke(_layoutSlot, _def.UpgradeId);
        }
    }
}
