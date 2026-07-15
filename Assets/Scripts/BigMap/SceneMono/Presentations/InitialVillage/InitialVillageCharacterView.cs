using UnityEngine;

namespace My.ScenePresentation
{
    public sealed class InitialVillageCharacterView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite leftSprite;
        [SerializeField] Sprite rightSprite;
        [SerializeField] bool faceRight;

        void Awake()
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            ApplyDirection();
        }

        public void SetFacing(bool right)
        {
            faceRight = right;
            ApplyDirection();
        }

        void ApplyDirection()
        {
            if (spriteRenderer != null) spriteRenderer.sprite = faceRight ? rightSprite : leftSprite;
        }
    }
}
