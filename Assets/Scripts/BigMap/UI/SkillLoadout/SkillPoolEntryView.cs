using cfg.demo;
using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillPoolEntryView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        static readonly Color LearnedBg = new Color(0.22f, 0.26f, 0.34f, 1f);
        static readonly Color UnlearnedBg = new Color(0.14f, 0.14f, 0.18f, 0.85f);
        static readonly Color SelectedBg = new Color(0.32f, 0.42f, 0.55f, 1f);
        static readonly Color SelectedFrame = new Color(0.72f, 0.82f, 0.95f, 1f);
        static readonly Color NormalFrame = new Color(0.566f, 0.566f, 0.566f, 1f);

        [SerializeField] Image icon;
        [SerializeField] Image background;
        [SerializeField] Image frame;
        [SerializeField] GameObject unlearnOverlay;
        [SerializeField] TextMeshProUGUI unlearnHintText;
        [SerializeField] SkillInSchoolHoverProvider hoverProvider;

        string _skillId;
        int _entryId;
        bool _isLearned;
        bool _isSelected;
        bool _canDrag;
        ISkillDropBehavior _skillDropBehavior;
        System.Action<int> _onSelected;
        System.Action<int> _onDetailClicked;

        public int EntryId => _entryId;

        public void Bind(
            SkillLearnEntry entry,
            bool isLearned,
            bool isSelected,
            ISkillDropBehavior skillDropBehavior,
            System.Action<int> onSelected,
            System.Action<int> onDetailClicked)
        {
            if (!ValidatePrefabRefs())
            {
                return;
            }

            _skillId = entry?.SkillId;
            _entryId = entry?.EntryId ?? 0;
            _isLearned = isLearned;
            _isSelected = isSelected;
            _canDrag = isLearned && !string.IsNullOrEmpty(_skillId);
            _skillDropBehavior = skillDropBehavior;
            _onSelected = onSelected;
            _onDetailClicked = onDetailClicked;

            var cfg = !string.IsNullOrEmpty(_skillId) ? SkillLibrary.GetSkillConfig(_skillId) : null;
            if (cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
            {
                var sp = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
                icon.sprite = sp;
                icon.enabled = sp != null;
                icon.color = isLearned ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
            else
            {
                icon.sprite = null;
                icon.enabled = false;
            }

            RefreshLearnedVisual();
            RefreshSelectionVisual();

            if (_entryId > 0 && !string.IsNullOrEmpty(_skillId))
            {
                hoverProvider.Configure(_entryId, _skillId, _isLearned);
            }
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshSelectionVisual();
        }

        bool ValidatePrefabRefs()
        {
            bool ok = true;

            if (icon == null)
            {
                Debug.LogError("[SkillPoolEntryView] Missing icon reference.", this);
                ok = false;
            }

            if (background == null)
            {
                Debug.LogError("[SkillPoolEntryView] Missing background reference.", this);
                ok = false;
            }

            if (frame == null)
            {
                Debug.LogError("[SkillPoolEntryView] Missing frame reference.", this);
                ok = false;
            }

            if (unlearnOverlay == null)
            {
                Debug.LogError("[SkillPoolEntryView] Missing unlearnOverlay reference.", this);
                ok = false;
            }

            if (unlearnHintText == null)
            {
                Debug.LogError("[SkillPoolEntryView] Missing unlearnHintText reference.", this);
                ok = false;
            }

            if (hoverProvider == null)
            {
                Debug.LogError("[SkillPoolEntryView] Missing hoverProvider reference.", this);
                ok = false;
            }

            return ok;
        }

        void RefreshLearnedVisual()
        {
            unlearnOverlay.SetActive(!_isLearned);

            if (_isLearned)
            {
                unlearnHintText.gameObject.SetActive(false);
                return;
            }

            unlearnHintText.gameObject.SetActive(true);
            unlearnHintText.text = ResolveCellHintText();
        }

        string ResolveCellHintText()
        {
            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr != null && mgr.CanLearnSkillFromEntry(_entryId, out _))
            {
                return "可学习";
            }

            return "未学习";
        }

        void RefreshSelectionVisual()
        {
            if (_isSelected && !_isLearned)
            {
                background.color = SelectedBg;
            }
            else
            {
                background.color = _isLearned ? LearnedBg : UnlearnedBg;
            }

            frame.color = _isSelected ? SelectedFrame : NormalFrame;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _entryId <= 0)
            {
                return;
            }

            if (_isLearned)
            {
                _onDetailClicked?.Invoke(_entryId);
                return;
            }

            _onSelected?.Invoke(_entryId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_canDrag || string.IsNullOrEmpty(_skillId) || _skillDropBehavior == null)
            {
                return;
            }

            _skillDropBehavior.OnBeginDragFromPool(_skillId);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_canDrag)
            {
                return;
            }

            _skillDropBehavior?.OnDragFromPool(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_canDrag)
            {
                return;
            }

            SkillDragSession.EndDrag(eventData);
        }
    }
}
