
using UnityEngine;

namespace My
{
    public class BuildPreviewController : MonoBehaviour
    {
        public GameObject ghostPrefab;
        public SpriteRenderer ghostRenderer;

        public void Awake()
        {
            var go = GameObject.Instantiate(ghostPrefab, MainGameManager.Instance.SceneEffectLayer);
            ghostRenderer = go.GetComponentInChildren<SpriteRenderer>();

            go.gameObject.SetActive(false);
        }

        public void SetSprite(Sprite s)
        {
            if (ghostRenderer != null) ghostRenderer.sprite = s;
        }

        public void UpdatePreview(bool valid, Vector3 worldPos)
        {
            transform.position = worldPos;
            //if (ghostRenderer != null)
            //{
            //    ghostRenderer.sharedMaterial = valid ? validMat : invalidMat;
            //}
            Material mat = ghostRenderer.material;
            mat.SetFloat("_State", valid ? 0 : 1);
        }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
        }
    }
}