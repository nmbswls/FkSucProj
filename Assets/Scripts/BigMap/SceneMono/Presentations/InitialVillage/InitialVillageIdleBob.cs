using UnityEngine;

namespace My.ScenePresentation
{
    public sealed class InitialVillageIdleBob : MonoBehaviour
    {
        [SerializeField] float amplitude = 0.025f;
        [SerializeField] float speed = 1.8f;
        Vector3 _baseLocalPosition;

        void Awake() => _baseLocalPosition = transform.localPosition;

        void Update()
        {
            var position = _baseLocalPosition;
            position.y += Mathf.Sin(Time.time * speed) * amplitude;
            transform.localPosition = position;
        }
    }
}
