using UnityEngine;

namespace My.UI.Alchemy
{
    // 将子节点在圆环上均匀排布，供炼金素材格使用。
    public sealed class AlchemyCircularSlotLayout : MonoBehaviour
    {
        [SerializeField] float radius = 120f;
        [SerializeField] float startAngleDeg = 90f;

        public void ApplyLayout(int activeCount)
        {
            int childCount = transform.childCount;
            if (childCount <= 0 || activeCount <= 0)
            {
                return;
            }

            float step = 360f / activeCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = transform.GetChild(i) as RectTransform;
                if (child == null)
                {
                    continue;
                }

                bool active = i < activeCount;
                child.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                float angleRad = (startAngleDeg - step * i) * Mathf.Deg2Rad;
                child.anchoredPosition = new Vector2(
                    Mathf.Cos(angleRad) * radius,
                    Mathf.Sin(angleRad) * radius);
            }
        }
    }
}
