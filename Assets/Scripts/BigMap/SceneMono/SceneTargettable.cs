
using UnityEngine;

namespace My
{
    /// <summary>
    /// 场景target
    /// </summary>
    public class SceneTargettable : MonoBehaviour
    {
        public IScenePresentation BelongPresenter;
        public float CenterHight = 0.0f;
        public string SpecialTag;
        public bool IsInteract; // 是否是交互体

        [HideInInspector]
        public Collider2D Collider;

        private void Awake()
        {
            BelongPresenter = GetComponentInParent<IScenePresentation>();
            Collider = GetComponent<Collider2D>();
        }

        public float GetOverlapHitHeight(Vector2 hitPos)
        {
            var cloestPos = Collider.ClosestPoint(hitPos);
            return CenterHight;
        }

        /// <summary>
        /// 检查
        /// </summary>
        /// <param name="atkHeight"></param>
        /// <returns></returns>
        public bool CheckHitHeightValid(float atkHeight)
        {
            var zOffset = BelongPresenter.GetLogicEntity().OffsetZ;

            float heightMax = Collider.bounds.max.y - Collider.transform.position.y;
            float heightMin = Collider.bounds.min.y - Collider.transform.position.y;

            if(atkHeight - 0.1f >  heightMax + zOffset)
            {
                return false;
            }

            if (atkHeight + 0.1f < heightMin + zOffset)
            {
                return false;
            }

            // zOffset
            return true;
        }
    }

}