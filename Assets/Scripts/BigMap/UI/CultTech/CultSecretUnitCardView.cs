using System;
using My.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 教团 Overview：秘会单位卡片 + 派遣/强化入口
    public sealed class CultSecretUnitCardView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI stateText;
        [SerializeField] TextMeshProUGUI detailText;
        [SerializeField] Button dispatchButton;
        [SerializeField] Button upgradeButton;

        string _unitId;
        Action<string> _onDispatch;
        Action<string> _onUpgrade;

        public string UnitId => _unitId;

        void Awake()
        {
            ResolveRefs();
            WireButtons();
        }

        public void Bind(
            string unitId,
            ECultSecretUnitState state,
            string detail,
            bool canDispatch,
            Action<string> onDispatch,
            bool canUpgrade = false,
            Action<string> onUpgrade = null)
        {
            ResolveRefs();
            _unitId = unitId ?? string.Empty;
            _onDispatch = onDispatch;
            _onUpgrade = onUpgrade;
            if (nameText != null)
                nameText.text = FormatUnitName(_unitId);
            if (stateText != null)
            {
                stateText.text = FormatState(state);
                stateText.color = ResolveStateColor(state);
            }
            if (detailText != null)
                detailText.text = detail ?? string.Empty;
            WireButtons();
            if (dispatchButton != null)
            {
                dispatchButton.interactable = canDispatch && _onDispatch != null;
                var label = dispatchButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = canDispatch
                        ? "派遣"
                        : state == ECultSecretUnitState.OnMission || state == ECultSecretUnitState.Assigned
                            ? "执行中"
                            : "不可用";
            }

            EnsureUpgradeButton();
            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(canUpgrade && _onUpgrade != null);
                upgradeButton.interactable = canUpgrade && _onUpgrade != null;
                var label = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = "强化";
            }
        }

        void WireButtons()
        {
            if (dispatchButton != null)
            {
                dispatchButton.onClick.RemoveAllListeners();
                dispatchButton.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(_unitId))
                        _onDispatch?.Invoke(_unitId);
                });
            }

            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveAllListeners();
                upgradeButton.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(_unitId))
                        _onUpgrade?.Invoke(_unitId);
                });
            }
        }

        void EnsureUpgradeButton()
        {
            if (upgradeButton != null) return;
            var t = transform.Find("UpgradeButton");
            if (t != null)
            {
                upgradeButton = t.GetComponent<Button>();
                return;
            }

            // 模板内补一个强化按钮（仅本卡私有，不另建 prefab）
            if (dispatchButton == null) return;
            var go = new GameObject("UpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            var src = dispatchButton.GetComponent<RectTransform>();
            if (src != null)
            {
                rt.anchorMin = src.anchorMin;
                rt.anchorMax = src.anchorMax;
                rt.pivot = src.pivot;
                rt.anchoredPosition = src.anchoredPosition + new Vector2(0f, src.rect.height + 6f);
                rt.sizeDelta = src.sizeDelta;
            }
            else
            {
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-8f, 40f);
                rt.sizeDelta = new Vector2(72f, 28f);
            }

            var img = go.GetComponent<Image>();
            img.color = new Color(0.22f, 0.18f, 0.32f, 1f);
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "强化";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            upgradeButton = go.GetComponent<Button>();
        }

        static string FormatUnitName(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return "未知秘会";
            if (unitId.StartsWith("secret_unit_", StringComparison.Ordinal))
            {
                var suffix = unitId.Substring("secret_unit_".Length);
                return $"秘会 · {suffix}";
            }
            return unitId;
        }

        static string FormatState(ECultSecretUnitState state)
        {
            switch (state)
            {
                case ECultSecretUnitState.Available: return "空闲";
                case ECultSecretUnitState.Assigned: return "已指派";
                case ECultSecretUnitState.OnMission: return "任务中";
                case ECultSecretUnitState.Recovering: return "恢复中";
                case ECultSecretUnitState.Lost: return "失落";
                default: return state.ToString();
            }
        }

        static Color ResolveStateColor(ECultSecretUnitState state)
        {
            switch (state)
            {
                case ECultSecretUnitState.Available: return new Color(0.58f, 0.86f, 0.62f, 1f);
                case ECultSecretUnitState.Assigned: return new Color(0.86f, 0.76f, 0.46f, 1f);
                case ECultSecretUnitState.OnMission: return new Color(0.62f, 0.76f, 0.96f, 1f);
                case ECultSecretUnitState.Recovering: return new Color(0.78f, 0.65f, 0.9f, 1f);
                case ECultSecretUnitState.Lost: return new Color(0.92f, 0.38f, 0.42f, 1f);
                default: return Color.white;
            }
        }

        void ResolveRefs()
        {
            nameText ??= FindText("NameText");
            stateText ??= FindText("StateText");
            detailText ??= FindText("DetailText");
            if (dispatchButton == null)
            {
                var t = transform.Find("DispatchButton");
                if (t != null) dispatchButton = t.GetComponent<Button>();
            }
            if (upgradeButton == null)
            {
                var t = transform.Find("UpgradeButton");
                if (t != null) upgradeButton = t.GetComponent<Button>();
            }
        }

        TextMeshProUGUI FindText(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
