// Scripts/UI/Panels/LoadingOverlayPanel.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace My.UI
{
    public class LoadingOverlayPanel : PanelBase, IInputConsumer
    {
        [SerializeField] private TextMeshProUGUI loadingText;

        public override void Setup(object data = null)
        {
            loadingText.text = data as string ?? "Loading...";
        }

        public override bool CanFocus => true;
        public override int FocusPriority => 1000;

        // ÍÌµôËùÓÐÊäÈë£¬·ÀÖ¹´©Í¸
        public bool OnConfirm() => true;
        public bool OnCancel() => true;
        public bool OnNavigate(Vector2 dir) => true;
        public bool OnHotkey(int index) => true;

        public bool OnScroll(float deltaY)
        {
            return true;
        }
    }
}

