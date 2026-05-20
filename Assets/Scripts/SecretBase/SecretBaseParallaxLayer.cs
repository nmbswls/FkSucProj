using UnityEngine;

namespace My.SecretBase
{
    // 子物体挂此组件，由 SecretBaseSceneRoot 驱动偏移。
    public class SecretBaseParallaxLayer : MonoBehaviour
    {
        [SerializeField] float factor = 0.5f;
        [SerializeField] float baseX;

        public void ApplyOffset(float scrollX)
        {
            var p = transform.position;
            p.x = baseX + scrollX * factor;
            transform.position = p;
        }
    }
}
