using cfg.demo;
using My.Map.Entity;
using My.Player;
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

        public Image icon;
        public Image background;
        public Button btnLearn;

        string _skillId;
        int _entryId;
        bool _isLearned;
        bool _isSelected;
        bool _canDrag;
        bool _canLearn;
        ISkillDropBehavior _skillDropBehavior;
        System.Action<int> _onSelected;
        System.Action<int> _onLearnClicked;

        public int EntryId => _entryId;

        void Awake()
        {
            if (btnLearn != null)
            {
                btnLearn.onClick.AddListener(OnLearnButtonClicked);
            }
        }

        public void Bind(
            SkillLearnEntry entry,
            bool isLearned,
            bool isSelected,
            ISkillDropBehavior skillDropBehavior,
            System.Action<int> onSelected,
            System.Action<int> onLearnClicked)
        {
            _skillId = entry?.SkillId;
            _entryId = entry?.EntryId ?? 0;
            _isLearned = isLearned;
            _isSelected = isSelected;
            _canDrag = isLearned && !string.IsNullOrEmpty(_skillId);
            _skillDropBehavior = skillDropBehavior;
            _onSelected = onSelected;
            _onLearnClicked = onLearnClicked;

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            _canLearn = !isLearned &&
                        _entryId > 0 &&
                        mgr != null &&
                        mgr.CanLearnSkillFromEntry(_entryId, out _);

            if (icon != null)
            {
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
            }

            RefreshSelectionVisual();
            EnsureHoverProvider();
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            RefreshSelectionVisual();
        }

        void RefreshSelectionVisual()
        {
            if (background != null)
            {
                if (_isSelected && !_isLearned)
                {
                    background.color = SelectedBg;
                }
                else
                {
                    background.color = _isLearned ? LearnedBg : UnlearnedBg;
                }
            }

            if (btnLearn != null)
            {
                bool showLearn = _isSelected && !_isLearned;
                btnLearn.gameObject.SetActive(showLearn);
                btnLearn.interactable = _canLearn;
            }
        }

        void EnsureHoverProvider()
        {
            if (_entryId <= 0 || string.IsNullOrEmpty(_skillId))
            {
                return;
            }

            var hover = GetComponent<SkillInSchoolHoverProvider>();
            if (hover == null)
            {
                hover = gameObject.AddComponent<SkillInSchoolHoverProvider>();
            }

            hover.Configure(_entryId, _skillId, _isLearned);

            if (background != null)
            {
                background.raycastTarget = true;
            }
        }

        void OnLearnButtonClicked()
        {
            if (_isLearned || !_isSelected || _entryId <= 0)
            {
                return;
            }

            _onLearnClicked?.Invoke(_entryId);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _canDrag || _isLearned || _entryId <= 0)
            {
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
