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
        [SerializeField] Image _overrideRing;
        [SerializeField] TextMeshProUGUI _overrideChargeText;

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

            EnsureOverrideVisualRefs();
        }

        void EnsureOverrideVisualRefs()
        {
            if (_overrideRing == null)
            {
                var tr = transform.Find("OverrideRing");
                if (tr != null)
                {
                    _overrideRing = tr.GetComponent<Image>();
                }
            }

            if (_overrideChargeText == null)
            {
                _overrideChargeText = transform.Find("OverrideRing/Charge")?.GetComponent<TextMeshProUGUI>()
                    ?? transform.Find("OverrideCharge")?.GetComponent<TextMeshProUGUI>();
            }

            if (_overrideRing == null)
            {
                var go = new GameObject("OverrideRing", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                go.transform.SetAsLastSibling();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(-5f, -5f);
                rt.offsetMax = new Vector2(5f, 5f);
                _overrideRing = go.GetComponent<Image>();
                _overrideRing.color = new Color(0.35f, 0.85f, 1f, 0.88f);
                _overrideRing.raycastTarget = false;
                go.SetActive(false);
            }

            if (_overrideChargeText == null && _overrideRing != null)
            {
                var textGo = new GameObject("Charge", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(_overrideRing.transform, false);
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = new Vector2(1f, 1f);
                textRt.anchorMax = new Vector2(1f, 1f);
                textRt.pivot = new Vector2(1f, 1f);
                textRt.anchoredPosition = new Vector2(4f, 4f);
                textRt.sizeDelta = new Vector2(16f, 16f);
                _overrideChargeText = textGo.GetComponent<TextMeshProUGUI>();
                _overrideChargeText.fontSize = 12f;
                _overrideChargeText.alignment = TextAlignmentOptions.Center;
                _overrideChargeText.text = "1";
                _overrideChargeText.raycastTarget = false;
            }
        }

        public void Refresh(string skillId, bool isLmbOverride = false)
        {
            EnsureOverrideVisualRefs();
            SetOverrideVisual(isLmbOverride);

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

        void SetOverrideVisual(bool active)
        {
            if (_overrideRing != null)
            {
                _overrideRing.gameObject.SetActive(active);
            }

            if (_overrideChargeText != null)
            {
                _overrideChargeText.gameObject.SetActive(active);
                if (active)
                {
                    _overrideChargeText.text = "1";
                }
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

            if (empty)
            {
                SetOverrideVisual(false);
            }
        }
    }
}
