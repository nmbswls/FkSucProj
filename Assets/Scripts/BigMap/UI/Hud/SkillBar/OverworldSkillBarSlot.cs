
using DG.Tweening;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace My.UI
{

    public class OverworldSkillBarSlot : MonoBehaviour, IHighlightableObj
    {

        protected OverworldSkillBar OwnerBar { get; private set; }
        public int SlotIdx = -1;

        public Image icon;
        public Image cooldownMask; // type: Filled
        public Button button;
        public GameObject lockOverlay;
        public Image emptyIcon;
        public GameObject Outline;
        public TextMeshProUGUI KeyName;

        public SkillRuntime skillData;

        public RectTransform shineRect;   // 高光图片的RectTransform

        private Vector2 shineStartPos = new Vector2(-25, 25);
        private Vector2 shineEndPos = new Vector2(25, -25);
        private float shineDuration = 0.4f;// 扫光耗时

        public void Setup(OverworldSkillBar bar, int slotIdx)
        {
            this.OwnerBar = bar;
            this.SlotIdx = slotIdx;

            emptyIcon.gameObject.SetActive(false);
            icon.gameObject.SetActive(true);

            if (lockOverlay) lockOverlay.SetActive(false);
            if (button)
            {
                //button.interactable = data.unlocked && data.isUsable;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {

                    //BattleUIModeManager.Instance.PreviewPlayerCastSkill(skillData);
                    OwnerBar.OnSkillSlotClicked(SlotIdx);
                });
            }

            KeyName.text = (slotIdx + 1).ToString();
        }

        public void BindingSkill(SkillRuntime skillData, bool hint = false)
        {
            this.skillData = skillData;
            if (skillData.cacheConfig != null)
            {
                var spriteRes = SimpleResManager.Load<Sprite>($"Sprites/Skill/{skillData.cacheConfig.IconPath}");
                //IconPath
                icon.sprite = spriteRes;
            }

            if(hint)
            {
                DoRefreshHint();
            }
        }

        private void DoRefreshHint()
        {
            DOTween.Kill(shineRect);

            // 创建一个动画序列
            Sequence sequence = DOTween.Sequence();
            // 4. 准备高光扫过：激活高光物体，并重置到左下角起点
            sequence.AppendCallback(() =>
            {
                shineRect.gameObject.SetActive(true);
                shineRect.localPosition = shineStartPos;
            });

            // 5. 高光扫过动画：从左下移动到右上
            sequence.Append(shineRect.DOLocalMove(shineEndPos, shineDuration)
                    .SetEase(Ease.Linear)); // 扫光一般用匀速(Linear)

            // 6. 扫光结束后，隐藏高光物体
            sequence.OnComplete(() =>
            {
                shineRect.gameObject.SetActive(false);
            });
        }

        public void SetCooldown(float ratio)
        {
            if (cooldownMask)
            {
                //cooldownMask.fillAmount = Mathf.Clamp01(ratio);
            }
        }

        public void Clear()
        {
            skillData = null;
            if (emptyIcon)
            {
                emptyIcon.gameObject.SetActive(true);
            }
            if (icon)
            {
                icon.gameObject.SetActive(false);
            }
            if (lockOverlay) lockOverlay.SetActive(false);
            if (button)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = false;
            }
            SetCooldown(0f);
        }

        public void SetHighlightStatus(bool isHighlight)
        {
            if (this == null || gameObject == null)
            {
                return;
            }

            Outline.gameObject.SetActive(isHighlight);
        }
    }

}
