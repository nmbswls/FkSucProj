using UnityEngine;

namespace My.Map.Scene
{
    // 单个弹药槽显示，只管自己的 active/inactive 两态切换，完全无状态逻辑。
    public class SkillProxyOrbSlotView : MonoBehaviour
    {
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private GameObject inactiveVisual;

        public void SetActive(bool active)
        {
            if (activeVisual != null)
            {
                activeVisual.SetActive(active);
            }

            if (inactiveVisual != null)
            {
                inactiveVisual.SetActive(!active);
            }
        }
    }
}
