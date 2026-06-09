using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.BodyPart
{
    // OneSlot：一段连线 + 一个里程碑节点
    public sealed class BodyPartProgressSlotView : MonoBehaviour
    {
        static readonly Color LitSlotColor = new Color(0.88f, 0.74f, 0.32f, 1f);
        static readonly Color LockedSlotColor = new Color(0.32f, 0.3f, 0.4f, 0.75f);
        static readonly Color LitLineColor = new Color(0.88f, 0.74f, 0.32f, 1f);
        static readonly Color LockedLineColor = new Color(0.23529412f, 0.23529412f, 0.23529412f, 1f);
        static readonly Color SelectedSlotColor = new Color(0.55f, 0.42f, 0.78f, 1f);

        [SerializeField] GameObject lineRoot;
        [SerializeField] Image lineTrackImage;
        [SerializeField] Image lineFillImage;
        [SerializeField] Button clickButton;
        [SerializeField] Image slotImage;
        [SerializeField] GameObject litRoot;
        [SerializeField] GameObject lockedRoot;
        [SerializeField] TextMeshProUGUI levelText;

        int _milestoneId;
        bool _unlocked;
        bool _selected;
        System.Action<int> _onSelected;

        public int MilestoneId => _milestoneId;

        public void Bind(
            BodyPartProgressInfo cfg,
            int segmentStartLevel,
            int currentLevel,
            bool hideLine,
            bool selected,
            System.Action<int> onSelected)
        {
            _milestoneId = cfg != null ? cfg.Id : 0;
            _onSelected = onSelected;

            if (levelText != null)
            {
                levelText.text = cfg != null ? cfg.Level.ToString() : string.Empty;
            }

            _unlocked = cfg != null && currentLevel >= cfg.Level;
            _selected = selected;
            if (litRoot != null)
            {
                litRoot.SetActive(_unlocked);
            }

            if (lockedRoot != null)
            {
                lockedRoot.SetActive(!_unlocked);
            }

            ApplySlotVisual();

            if (lineRoot != null)
            {
                lineRoot.SetActive(!hideLine);
            }

            float segmentFill = cfg != null
                ? ComputeSegmentFill(segmentStartLevel, cfg.Level, currentLevel)
                : 0f;

            if (lineFillImage != null)
            {
                lineFillImage.fillAmount = segmentFill;
                lineFillImage.color = segmentFill > 0f ? LitLineColor : LockedLineColor;
            }
            else if (lineTrackImage != null)
            {
                lineTrackImage.color = segmentFill >= 1f ? LitLineColor : LockedLineColor;
            }

            if (clickButton != null)
            {
                clickButton.onClick.RemoveAllListeners();
                clickButton.onClick.AddListener(OnClicked);
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplySlotVisual();
        }

        void ApplySlotVisual()
        {
            if (slotImage == null || litRoot != null || lockedRoot != null)
            {
                return;
            }

            slotImage.color = _selected
                ? SelectedSlotColor
                : (_unlocked ? LitSlotColor : LockedSlotColor);
        }

        static float ComputeSegmentFill(int startLevel, int endLevel, int currentLevel)
        {
            if (endLevel <= startLevel)
            {
                return currentLevel >= endLevel ? 1f : 0f;
            }

            if (currentLevel <= startLevel)
            {
                return 0f;
            }

            if (currentLevel >= endLevel)
            {
                return 1f;
            }

            return Mathf.Clamp01((currentLevel - startLevel) / (float)(endLevel - startLevel));
        }

        void OnClicked()
        {
            if (_milestoneId > 0)
            {
                _onSelected?.Invoke(_milestoneId);
            }
        }
    }
}
