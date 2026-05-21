using System;
using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class BedroomDeploySavePointListRowView : MonoBehaviour
    {
        [SerializeField] Image bg;
        [SerializeField] TextMeshProUGUI title;

        SavePoint _savePoint;
        Button _button;

        public SavePoint BoundSavePoint => _savePoint;
        public event Action<SavePoint> Clicked;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (bg == null && _button != null)
                bg = _button.targetGraphic as Image;
            if (title == null)
                title = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_button != null)
            {
                BedroomDeployMapRowView.ApplySimpleRowButton(_button);
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        public void Bind(SavePoint sp, bool selected)
        {
            _savePoint = sp;
            if (title != null)
                title.text = sp != null ? sp.DisplayName : string.Empty;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (bg != null)
                bg.color = selected ? BedroomDeployUiColors.RowBgSelected : BedroomDeployUiColors.RowBgNormal;
        }

        void OnClick()
        {
            if (_savePoint != null)
                Clicked?.Invoke(_savePoint);
        }
    }
}
