using My.Map.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SceneUnitBuffHeadHintItem : MonoBehaviour
    {
        public Image IconImage;

        public SceneUnitPresenter BindingUnit { get; private set; }
        public long BoundBuffInstanceId { get; private set; }

        public void Bind(SceneUnitPresenter unit, Sprite icon, long buffInstanceId)
        {
            BindingUnit = unit;
            BoundBuffInstanceId = buffInstanceId;
            SetIcon(icon);
            gameObject.SetActive(true);
        }

        public void SetIcon(Sprite icon)
        {
            if (IconImage != null)
            {
                IconImage.sprite = icon;
                IconImage.enabled = icon != null;
            }
        }

        public void Unbind()
        {
            BindingUnit = null;
            BoundBuffInstanceId = 0;
            SetIcon(null);
            gameObject.SetActive(false);
        }
    }
}
