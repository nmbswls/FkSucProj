using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class YesNoMsgBox : PanelWithInput
    {
        public const string PanelId = "YesNoMsgBox";

        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text messageText;
        [SerializeField] Button btnConfirm;
        [SerializeField] Button btnCancel;

        Action _onConfirm;
        Action _onCancel;

        public static YesNoMsgBox Show(
            string title,
            string message,
            Action onConfirm = null,
            Action onCancel = null)
        {
            var panel = UIManager.Instance.ShowPanel(PanelId) as YesNoMsgBox;
            if (panel == null)
            {
                Debug.LogError("YesNoMsgBox: panel not found");
                return null;
            }

            panel.RefreshData(title, message, onConfirm, onCancel);
            return panel;
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = PanelId;
            }
        }

        public void RefreshData(string title, string message, Action onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }

            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }

        void OnEnable()
        {
            if (btnConfirm != null)
            {
                btnConfirm.onClick.RemoveAllListeners();
                btnConfirm.onClick.AddListener(OnClickConfirm);
            }

            if (btnCancel != null)
            {
                btnCancel.onClick.RemoveAllListeners();
                btnCancel.onClick.AddListener(OnClickCancel);
            }
        }

        void OnClickConfirm()
        {
            _onConfirm?.Invoke();
            Close();
        }

        void OnClickCancel()
        {
            _onCancel?.Invoke();
            Close();
        }

        public void Close()
        {
            _onConfirm = null;
            _onCancel = null;
            UIManager.Instance.HidePanel(PanelId);
        }

        public override bool OnCancel()
        {
            OnClickCancel();
            return true;
        }
    }
}
