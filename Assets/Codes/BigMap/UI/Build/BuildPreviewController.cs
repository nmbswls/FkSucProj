
using System.Runtime.CompilerServices;
using UnityEngine;

namespace My
{
    public class BuildPreviewController : MonoBehaviour
    {
        public GameObject ghostPrefab;

        public GameObject ghostGo;
        public SpriteRenderer ghostRenderer;

        public float CellSize = 1;

        public void Awake()
        {
            ghostGo = GameObject.Instantiate(ghostPrefab, MainGameManager.Instance.SceneEffectLayer);
            ghostRenderer = ghostGo.GetComponentInChildren<SpriteRenderer>();
            ghostGo.gameObject.SetActive(false);
        }

        public void InitPreview(HomeFacilityCfg obj)
        {

            if (ghostRenderer != null)
            {
                ghostRenderer.sprite = obj.sprite;

                int sx = obj.pivot.x;
                int sy = obj.pivot.y;

                Vector3 local = new Vector3((-sx)* CellSize, (-sy) * CellSize, 0);
                ghostRenderer.transform.localPosition = local;
            }

            RefreshRotation(EPlacementRotation.R0);
        }

        public void RefreshRotation(EPlacementRotation rot)
        {
            if (ghostRenderer != null)
            {
                switch (rot)
                {
                    case EPlacementRotation.R0:
                        {
                            ghostRenderer.transform.localEulerAngles = new Vector3(0, 0, 0);
                        }
                        break;
                    case EPlacementRotation.R90:
                        {
                            ghostRenderer.transform.localEulerAngles = new Vector3(0, 0, 90);
                        }
                        break;
                    case EPlacementRotation.R180:
                        {
                            ghostRenderer.transform.localEulerAngles = new Vector3(0, 0, 180);
                        }
                        break;
                    case EPlacementRotation.R270:
                        {
                            ghostRenderer.transform.localEulerAngles = new Vector3(0, 0, 270);
                        }
                        break;
                }
            }
        }


        public void UpdatePreview(bool valid, Vector3 worldPos)
        {
            ghostGo.transform.position = worldPos;
            //if (ghostRenderer != null)
            //{
            //    ghostRenderer.sharedMaterial = valid ? validMat : invalidMat;
            //}
            Material mat = ghostRenderer.material;
            mat.SetFloat("_State", valid ? 0 : 1);
        }

        public void Show(bool show)
        {
            ghostGo.gameObject.SetActive(show);
        }
    }
}