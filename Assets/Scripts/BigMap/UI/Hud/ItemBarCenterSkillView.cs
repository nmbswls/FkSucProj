using My;
using My.Config;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // ItemBar 轮盘中心：显示 LMB 对应技能图标
    public class ItemBarCenterSkillView : MonoBehaviour
    {
        [SerializeField] Image _icon;
        [SerializeField] TextMeshProUGUI _keyHint;
        [SerializeField] GameObject _emptyRoot;

        void Awake()
        {
            if (_icon == null)
            {
                _icon = transform.Find("SlotIcon")?.GetComponent<Image>()
                    ?? transform.GetComponent<Image>();
            }

            if (_keyHint == null)
            {
                _keyHint = transform.Find("Hint2")?.GetComponent<TextMeshProUGUI>();
            }

            if (_keyHint != null)
            {
                _keyHint.text = "LMB";
            }
        }

        public void Refresh(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                SetEmpty(true);
                return;
            }

            SetEmpty(false);

            Sprite iconSprite = ResolveSkillIcon(skillId);
            if (_icon != null)
            {
                _icon.enabled = iconSprite != null;
                _icon.sprite = iconSprite;
            }
        }

        static Sprite ResolveSkillIcon(string skillId)
        {
            var player = MainGameManager.Instance?.gameLogicManager?.playerLogicEntity;
            if (player != null
                && player.ablilityManager.SkillRuntimes.TryGetValue(skillId, out var skillRuntime)
                && skillRuntime?.cacheConfig != null
                && !string.IsNullOrEmpty(skillRuntime.cacheConfig.IconPath))
            {
                return LoadSkillSprite(skillRuntime.cacheConfig.IconPath);
            }

            var cfg = SkillLibrary.GetSkillConfig(skillId);
            if (cfg != null && !string.IsNullOrEmpty(cfg.IconPath))
            {
                return LoadSkillSprite(cfg.IconPath);
            }

            return SimpleResManager.Load<Sprite>("Sprites/Skill/fallback");
        }

        static Sprite LoadSkillSprite(string iconPath)
        {
            return SimpleResManager.Load<Sprite>($"Sprites/Skill/{iconPath}");
        }

        void SetEmpty(bool empty)
        {
            if (_emptyRoot != null)
            {
                _emptyRoot.SetActive(empty);
            }

            if (_icon != null)
            {
                _icon.enabled = !empty;
            }
        }
    }
}
