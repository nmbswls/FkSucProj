using TMPro;
using UnityEngine;

namespace My.UI
{
    public sealed class RumorIntelShopHintRowView : MonoBehaviour
    {
        TMP_Text _label;

        void EnsureLabel()
        {
            if (_label == null)
            {
                _label = GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        void Awake()
        {
            EnsureLabel();
        }

        public void Apply(string msg)
        {
            EnsureLabel();
            if (_label != null)
            {
                _label.text = msg;
            }
        }
    }
}
