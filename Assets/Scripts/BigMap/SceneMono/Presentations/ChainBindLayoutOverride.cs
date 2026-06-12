using UnityEngine;

namespace My.Map.Scene
{
    // 可选：挂在 SceneUnitPresenter 上，微调锁链布局相对 sprite bounds 的映射
    public class ChainBindLayoutOverride : MonoBehaviour
    {
        [Range(0.5f, 1f)]
        public float insetMul = 1f;

        public Vector2 halfExtentScale = Vector2.one;
        public Vector2 centerOffsetLocal;
        [Range(0.3f, 1.2f)]
        public float bulgeMul = 1f;
    }
}
