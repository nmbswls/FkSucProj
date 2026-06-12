
using DG.Tweening;
using My.Map.Entity;
using TMPro;
using UnityEditor;
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

        string _boundSkillId;
        string _denyMessage;
        static readonly Color UsableIconColor = Color.white;
        static readonly Color DeniedIconColor = new(0.55f, 0.55f, 0.55f, 0.75f);

        public string BoundSkillId => _boundSkillId;

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

            ApplyKeyHint(slotIdx, false);
        }

        public void ApplyKeyHint(int slotIdx, bool humanQuickBar)
        {
            if (KeyName == null)
            {
                return;
            }

            if (humanQuickBar)
            {
                switch (slotIdx)
                {
                    case 0:
                        KeyName.text = "LMB";
                        return;
                    case 1:
                        KeyName.text = "RMB";
                        return;
                    case 2:
                        KeyName.text = "Sp";
                        return;
                    default:
                        KeyName.text = (slotIdx - 2).ToString();
                        return;
                }
            }

            KeyName.text = (slotIdx + 1).ToString();
        }

        public void BindingSkill(SkillRuntime skillData, bool hint = false)
        {
            this.skillData = skillData;
            _boundSkillId = skillData?.cacheConfig?.SkillId;

            Sprite iconSprite = null;
            if (skillData.cacheConfig != null && !string.IsNullOrEmpty(skillData.cacheConfig.IconPath))
            {
                iconSprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{skillData.cacheConfig.IconPath}");
                //IconPath
            }
            
            if(iconSprite == null)
            {
                iconSprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/fallback");
            }

            icon.sprite = iconSprite;

            if (button)
            {
                button.interactable = true;
            }

            RefreshUsability();

            if (hint)
            {
                DoRefreshHint();
            }
        }

        // 左键动态槽：优先运行时；无注册时仅按配置显示图标
        public void BindingSkillId(string skillId, bool hint = false)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                Clear();
                return;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player != null
                && player.ablilityManager.SkillRuntimes.TryGetValue(skillId, out var skillRuntime)
                && skillRuntime != null)
            {
                BindingSkill(skillRuntime, hint);
                return;
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg == null)
            {
                Clear();
                return;
            }

            skillData = null;
            _boundSkillId = skillId;
            emptyIcon.gameObject.SetActive(false);
            icon.gameObject.SetActive(true);

            Sprite iconSprite = null;
            if (!string.IsNullOrEmpty(cfg.IconPath))
            {
                iconSprite = SimpleResManager.Load<Sprite>($"Sprites/Skill/{cfg.IconPath}");
            }

            if (iconSprite == null)
            {
                iconSprite = SimpleResManager.Load<Sprite>("Sprites/Skill/fallback");
            }

            icon.sprite = iconSprite;

            if (button)
            {
                button.interactable = true;
            }

            RefreshUsability();

            if (hint)
            {
                DoRefreshHint();
            }
        }

        public void RefreshUsability()
        {
            _denyMessage = null;
            if (string.IsNullOrEmpty(_boundSkillId))
            {
                ApplyDenyVisual(false, null);
                return;
            }

            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player == null)
            {
                ApplyDenyVisual(false, null);
                return;
            }

            bool canUse = SkillCastConditionUtil.TryEvaluateReadiness(
                player,
                player.ablilityManager,
                _boundSkillId,
                out _denyMessage);
            ApplyDenyVisual(!canUse, _denyMessage);
        }

        void ApplyDenyVisual(bool denied, string denyMessage)
        {
            if (lockOverlay != null)
            {
                lockOverlay.SetActive(denied);
            }

            if (icon != null)
            {
                icon.color = denied ? DeniedIconColor : UsableIconColor;
            }

            if (button)
            {
                button.interactable = true;
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
            _boundSkillId = null;
            _denyMessage = null;
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
