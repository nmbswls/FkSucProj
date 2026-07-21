using System;
using My.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 教团 Overview：秘会单位卡片 + 派遣入口
    public sealed class CultSecretUnitCardView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI stateText;
        [SerializeField] TextMeshProUGUI detailText;
        [SerializeField] Button dispatchButton;

        string _unitId;
        Action<string> _onDispatch;

        public string UnitId => _unitId;

        void Awake()
        {
            ResolveRefs();
            WireDispatch();
        }

        public void Bind(
            string unitId,
            ECultSecretUnitState state,
            string detail,
            bool canDispatch,
            Action<string> onDispatch)
        {
            ResolveRefs();
            _unitId = unitId ?? string.Empty;
            _onDispatch = onDispatch;
            if (nameText != null)
                nameText.text = FormatUnitName(_unitId);
            if (stateText != null)
            {
                stateText.text = FormatState(state);
                stateText.color = ResolveStateColor(state);
            }
            if (detailText != null)
                detailText.text = detail ?? string.Empty;
            WireDispatch();
            if (dispatchButton != null)
            {
                dispatchButton.interactable = canDispatch && _onDispatch != null;
                var label = dispatchButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = canDispatch
                        ? "增幅"
                        : state == ECultSecretUnitState.OnMission || state == ECultSecretUnitState.Assigned
                            ? "执行中"
                            : "不可用";
            }
        }

        void WireDispatch()
        {
            if (dispatchButton == null) return;
            dispatchButton.onClick.RemoveAllListeners();
            dispatchButton.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(_unitId))
                    _onDispatch?.Invoke(_unitId);
            });
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
        }

        TextMeshProUGUI FindText(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
