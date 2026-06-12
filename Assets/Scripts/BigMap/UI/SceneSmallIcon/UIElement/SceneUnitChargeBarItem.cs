using My;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class SceneUnitChargeBarItem : MonoBehaviour
    {
        public Image Fill;

        public IScenePresentation Binding { get; private set; }

        public void Bind(IScenePresentation target)
        {
            Binding = target;
            gameObject.SetActive(true);
        }

        public void SetFill(float progress01)
        {
            if (Fill == null)
            {
                return;
            }

            Fill.fillAmount = Mathf.Clamp01(progress01);
        }

        public void Unbind()
        {
            Binding = null;
            gameObject.SetActive(false);
        }
    }
}
