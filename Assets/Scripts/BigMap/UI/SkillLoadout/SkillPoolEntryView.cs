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

        public TMP_Text label;
        public Image icon;
        public Image background;

        string _skillId;
        int _entryId;
        bool _canDrag;
        bool _canLearn;
        ISkillDropBehavior _skillDropBehavior;
        System.Action<int> _onLearnClicked;

        public void Bind(
            SkillLearnEntry entry,
            bool isLearned,
            ISkillDropBehavior skillDropBehavior,
            System.Action<int> onLearnClicked)
        {
            _skillId = entry?.SkillId;
            _entryId = entry?.EntryId ?? 0;
            _canDrag = isLearned && !string.IsNullOrEmpty(_skillId);
            _skillDropBehavior = skillDropBehavior;
            _onLearnClicked = onLearnClicked;

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            _canLearn = !isLearned &&
                        _entryId > 0 &&
                        mgr != null &&
                        mgr.CanLearnSkillFromEntry(_entryId, out _);

            if (label != null)
            {
                string display = ResolveDisplayName(entry);
                if (!isLearned)
                {
                    display += _canLearn ? "\n点击学习" : "\n(未学)";
                }

                label.text = display;
                label.alpha = isLearned ? 1f : _canLearn ? 0.85f : 0.55f;
            }

            if (background != null)
            {
                background.color = isLearned ? LearnedBg : UnlearnedBg;
            }

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
        }

        static string ResolveDisplayName(SkillLearnEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(entry.DisplayName))
            {
                return entry.DisplayName;
            }

            if (!string.IsNullOrEmpty(entry.SkillId))
            {
                var cfg = SkillLibrary.GetSkillConfig(entry.SkillId);
                if (cfg != null && !string.IsNullOrEmpty(cfg.Desc))
                {
                    return cfg.Desc;
                }

                return entry.SkillId;
            }

            return string.Empty;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || _canDrag || !_canLearn)
            {
                return;
            }

            _onLearnClicked?.Invoke(_entryId);
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

            if (_skillDropBehavior != null)
            {
                _skillDropBehavior.OnEndDragFromPool();
            }
            else
            {
                SkillDragSession.End();
            }
        }
    }
}
