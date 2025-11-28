
using System;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{ 

    public class ItemCountChooseBox : PanelBase
    {

        public RectTransform Mask;
        public Button ConfirmBtn;
        public Button CancelBtn;

        public InputField MaxCountText;
        public Slider slider;

        // »Øµ÷
        private Action<int> onConfirm;
        private Action onCancel;

        private int minValue = 1;
        private int maxValue = 1;
        private int step = 1;
        private int quantity;

    }
}
