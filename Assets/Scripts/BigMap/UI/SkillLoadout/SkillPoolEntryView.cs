using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public class SkillPoolEntryView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public TMP_Text label;
        public Image icon;
        public Image background;

        string _skillId;
        ISkillDropBehavior _skillDropBehavior;

        public void Bind(string skillId, ISkillDropBehavior skillDropBehavior)
        {
            _skillId = skillId;
            _skillDropBehavior = skillDropBehavior;

            if (label != null)
                label.text = string.IsNullOrEmpty(skillId) ? string.Empty : skillId;

            if (icon != null)
            {
                var cfg = !string.IsNullOrEmpty(skillId) ? SkillLibrary.GetSkillConfig(skillId) : null;
                if (cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
                {
                    var sp = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
                    icon.sprite = sp;
                    icon.enabled = sp != null;
                }
                else
                {
                    icon.sprite = null;
                    icon.enabled = false;
                }
            }
        }

        public void SetVisible(bool v) => gameObject.SetActive(v);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(_skillId) || _skillDropBehavior == null)
                return;
            _skillDropBehavior.OnBeginDragFromPool(_skillId);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _skillDropBehavior?.OnDragFromPool(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_skillDropBehavior != null)
                _skillDropBehavior.OnEndDragFromPool();
            else
                SkillDragSession.End();
        }
    }
}
