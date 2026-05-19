using UnityEngine;

namespace My.SecretBase
{
    public class SecretBaseParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float parallaxFactor = 0.5f;
        [SerializeField] private float baseX;

        public void ApplyOffset(float scrollX)
        {
            var p = transform.position;
            p.x = baseX + scrollX * parallaxFactor;
            transform.position = p;
        }
    }
}
