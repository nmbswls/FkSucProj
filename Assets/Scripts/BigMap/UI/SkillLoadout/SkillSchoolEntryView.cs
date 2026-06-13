using System;
using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public sealed class SkillSchoolEntryView : MonoBehaviour
    {
        static readonly Color UnlockedBg = new Color(0.28f, 0.3f, 0.35f, 1f);
        static readonly Color LockedBg = new Color(0.16f, 0.17f, 0.2f, 0.92f);
        static readonly Color UnlockedMask = Color.white;
        static readonly Color LockedMask = new Color(0.55f, 0.55f, 0.58f, 0.72f);

        [SerializeField] Button clickButton;
        [SerializeField] Image background;
        [SerializeField] Image maskBackground;
        [SerializeField] Image lockOverlay;
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] Image icon;

        int _tabIndex;
        int _schoolId;
        bool _unlocked;
        Action<int> _onSchoolClicked;

        void Awake()
        {
            EnsureReferences();
            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(OnClicked);
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        public int TabIndex => _tabIndex;
        public int SchoolId => _schoolId;
        public bool IsUnlocked => _unlocked;

        public void Bind(int tabIndex, SkillSchool school, Action<int> onSchoolClicked)
        {
            EnsureReferences();

            _tabIndex = tabIndex;
            _schoolId = school?.SchoolId ?? 0;
            _onSchoolClicked = onSchoolClicked;
            _unlocked = _schoolId > 0 && SkillSchoolAccessUtil.IsSchoolUnlocked(_schoolId);

            gameObject.SetActive(true);

            if (label != null)
            {
                label.text = school != null && !string.IsNullOrEmpty(school.DisplayName)
                    ? school.DisplayName
                    : "未开放";
            }

            RefreshIcon(school);
            RefreshLockedVisual();
        }

        void EnsureReferences()
        {
            if (clickButton == null)
            {
                clickButton = GetComponent<Button>();
            }

            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (label == null)
            {
                label = transform.Find("Lbl")?.GetComponent<TextMeshProUGUI>();
            }

            if (icon == null)
            {
                var iconTr = transform.Find("Mask/Icon") ?? transform.Find("Icon");
                icon = iconTr?.GetComponent<Image>();
            }

            if (maskBackground == null)
            {
                maskBackground = transform.Find("Mask")?.GetComponent<Image>();
            }

            if (lockOverlay == null)
            {
                var lockTr = transform.Find("Lock");
                if (lockTr != null)
                {
                    lockOverlay = lockTr.GetComponent<Image>();
                }
            }

            if (icon != null)
            {
                icon.raycastTarget = false;
            }

            if (lockOverlay != null)
            {
                lockOverlay.raycastTarget = false;
            }

            if (label != null)
            {
                label.raycastTarget = false;
            }
        }

        void RefreshIcon(SkillSchool school)
        {
            if (icon == null)
            {
                return;
            }

            if (school == null || string.IsNullOrEmpty(school.IconPath))
            {
                icon.sprite = null;
                icon.enabled = false;
                return;
            }

            var sprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{school.IconPath}");
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = _unlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        void RefreshLockedVisual()
        {
            if (background != null)
            {
                background.color = _unlocked ? UnlockedBg : LockedBg;
            }

            if (maskBackground != null)
            {
                maskBackground.color = _unlocked ? UnlockedMask : LockedMask;
            }

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(!_unlocked);
            }

            if (label != null)
            {
                label.color = _unlocked
                    ? Color.white
                    : new Color(0.75f, 0.75f, 0.78f, 0.85f);
            }

            if (clickButton != null)
            {
                clickButton.interactable = true;
            }
        }

        void OnClicked()
        {
            _onSchoolClicked?.Invoke(_schoolId);
        }
    }
}
