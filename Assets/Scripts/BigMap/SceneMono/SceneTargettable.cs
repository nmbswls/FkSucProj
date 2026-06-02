using My.Map;
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
        public bool CheckHitHeightValid(float atkHeight, float tolerance = 0.2f)
        {
            var entity = BelongPresenter?.GetLogicEntity();
            if (entity == null || Collider == null)
            {
                return false;
            }

            float logicHeight = MapLogicPosition.GetEffectiveLogicY(entity);

            float heightMax = Collider.bounds.max.y - Collider.transform.position.y;
            float heightMin = Collider.bounds.min.y - Collider.transform.position.y;

            if (atkHeight - tolerance > heightMax + logicHeight)
            {
                return false;
            }

            if (atkHeight + tolerance < heightMin + logicHeight)
            {
                return false;
            }

            return true;
        }
    }

}