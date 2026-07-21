using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 教团 Overview：锚点行 + 派遣入口
    public sealed class CultAnchorRowView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI areaText;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button dispatchButton;

        string _logicAreaId;
        string _anchorId;
        string _regionKey;
        Action<string, string, string> _onDispatch;
        Action<string> _onIntel;
        bool _canOpenIntel;

        public string LogicAreaId => _logicAreaId;
        public string AnchorId => _anchorId;
        public string RegionKey => _regionKey;

        void Awake()
        {
            ResolveRefs();
            WireDispatch();
        }

        public void Bind(
            string regionKey,
            string logicAreaId,
            string anchorId,
            string displayName,
            string status,
            bool amplified,
            bool canAmplify,
            Action<string, string, string> onDispatch,
            string intelSummary = null,
            bool canOpenIntel = false,
            Action<string> onIntel = null)
        {
            ResolveRefs();
            _regionKey = regionKey ?? string.Empty;
            _logicAreaId = logicAreaId ?? string.Empty;
            _anchorId = anchorId ?? string.Empty;
            _onDispatch = onDispatch;
            _onIntel = onIntel;
            _canOpenIntel = canOpenIntel && _onIntel != null;
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(displayName) ? _anchorId : displayName;
            if (areaText != null)
                areaText.text = string.Empty;
            if (statusText != null)
            {
                statusText.text = status ?? string.Empty;
                statusText.color = ResolveStatusColor(status, amplified);
            }
            WireDispatch();
            if (dispatchButton != null)
            {
                dispatchButton.interactable = _canOpenIntel || (canAmplify && _onDispatch != null);
                var label = dispatchButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (_canOpenIntel && label != null) label.text = "情报";
                if (label != null) label.text = amplified ? "已增幅" : canAmplify ? "增幅" : "未建立";
            }
        }

        static Color ResolveStatusColor(string status, bool amplified)
        {
            if (status != null && status.StartsWith("封锁", StringComparison.Ordinal))
                return new Color(0.92f, 0.34f, 0.38f, 1f);
            if (status != null && status.StartsWith("被盯梢", StringComparison.Ordinal))
                return new Color(0.94f, 0.58f, 0.34f, 1f);
            if (amplified) return new Color(0.72f, 0.55f, 0.96f, 1f);
            return new Color(0.72f, 0.78f, 0.72f, 1f);
        }

        void WireDispatch()
        {
            if (dispatchButton == null) return;
            dispatchButton.onClick.RemoveAllListeners();
            dispatchButton.onClick.AddListener(() =>
            {
                if (_canOpenIntel)
                    _onIntel?.Invoke(_logicAreaId);
                else if (!string.IsNullOrEmpty(_anchorId))
                    _onDispatch?.Invoke(_regionKey, _logicAreaId, _anchorId);
            });
        }

        void ResolveRefs()
        {
            nameText ??= FindText("NameText");
            areaText ??= FindText("AreaText");
            statusText ??= FindText("StatusText");
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
