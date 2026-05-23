using My;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class SceneUnitHpBarItem : MonoBehaviour
    {
        public Image Fill;

        public IScenePresentation Binding { get; private set; }

        public void Bind(IScenePresentation target)
        {
            Binding = target;
            gameObject.SetActive(true);
        }

        public void SetFill(long hp, long maxHp)
        {
            if (Fill == null)
            {
                return;
            }

            Fill.fillAmount = maxHp > 0 ? Mathf.Clamp01(hp / (float)maxHp) : 0f;
        }

        public void Unbind()
        {
            Binding = null;
            gameObject.SetActive(false);
        }
    }
}
