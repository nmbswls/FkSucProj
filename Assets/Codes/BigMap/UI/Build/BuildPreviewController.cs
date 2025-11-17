
using UnityEngine;

namespace My
{
    public class BuildPreviewController : MonoBehaviour
    {
        public SpriteRenderer ghostRenderer;
        public Material validMat;
        public Material invalidMat;

        public void SetSprite(Sprite s)
        {
            if (ghostRenderer != null) ghostRenderer.sprite = s;
        }

        public void UpdatePreview(bool valid, Vector3 worldPos)
        {
            transform.position = worldPos;
            if (ghostRenderer != null)
            {
                ghostRenderer.sharedMaterial = valid ? validMat : invalidMat;
            }
        }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }
    }
}