using DG.Tweening;
using My.Map.Entity;
using My.UI.SkillLoadout;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace My.UI
{
    // 技能槽视图基类，对应 ItemCellBase 在道具格中的地位。
    // 仅处理 config 层的视觉展示：图标、hover 配置、清空状态。
    // 不访问任何 singleton，不持有 SkillRuntime，不做可用性判断。
    // 需要运行时状态（冷却、可用性、runtime 绑定）的子类自行维护。
    public abstract class SkillSlotBase : MonoBehaviour, IPointerClickHandler
    {
        const float ShineDuration = 0.4f;
        static readonly Vector2 ShineStartPos = new Vector2(-25f, 25f);
        static readonly Vector2 ShineEndPos = new Vector2(25f, -25f);

        public Image icon;
        public Image emptyIcon;
        public Image cooldownMask;
        public GameObject lockOverlay;
        public RectTransform shineRect;

        [SerializeField]
        protected SkillEquippedHoverProvider hoverProvider;

        public string BoundSkillId { get; private set; }

        protected abstract void OnClick();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnClick();
            }
        }

        // 按 config 绑定技能图标与 hover，不涉及运行时状态
        public void BindByConfig(string skillId)
        {
            BoundSkillId = skillId;

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            Sprite sprite = null;
            if (cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
            {
                sprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
            }

            if (sprite == null)
            {
                sprite = SimpleResManager.Load<Sprite>("Sprites/Skill/fallback");
            }

            ApplyIcon(sprite);
            ConfigureHover(skillId);
        }

        public void Clear()
        {
            BoundSkillId = null;
            ConfigureHover(null);

            if (emptyIcon != null)
            {
                emptyIcon.gameObject.SetActive(true);
            }

            if (icon != null)
            {
                icon.gameObject.SetActive(false);
                icon.color = Color.white;
            }

            if (lockOverlay != null)
            {
                lockOverlay.SetActive(false);
            }
        }

        // 子类调用：设置图标 sprite 并切换到填充状态
        protected void ApplyIcon(Sprite sprite)
        {
            if (emptyIcon != null)
            {
                emptyIcon.gameObject.SetActive(false);
            }

            if (icon != null)
            {
                icon.gameObject.SetActive(true);
                icon.sprite = sprite;
            }
        }

        // 子类调用：设置图标色调（可用性视觉）
        protected void SetIconTint(Color color)
        {
            if (icon != null)
            {
                icon.color = color;
            }
        }

        // 子类调用：设置 lock 遮罩可见性
        protected void SetLockOverlay(bool visible)
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(visible);
            }
        }

        protected void ConfigureHover(string skillId)
        {
            if (hoverProvider != null)
            {
                hoverProvider.Configure(skillId);
            }
        }

        protected void PlayShine()
        {
            if (shineRect == null)
            {
                return;
            }

            DOTween.Kill(shineRect);
            var seq = DOTween.Sequence();
            seq.AppendCallback(() =>
            {
                shineRect.gameObject.SetActive(true);
                shineRect.localPosition = ShineStartPos;
            });
            seq.Append(shineRect.DOLocalMove(ShineEndPos, ShineDuration).SetEase(Ease.Linear));
            seq.OnComplete(() => shineRect.gameObject.SetActive(false));
        }
    }
}
