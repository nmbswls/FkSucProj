
using System.Collections.Generic;
using My.Map.Scene;
using UnityEngine;

namespace My.Map
{
    public class MapSceneFader : MonoBehaviour
    {
        private float fadeSpeed = 4f;

        private float targetAlpha;
        private float currAlpha;

        public List<SpriteRenderer> spriteRenderers = new();
        public Collider2D CheckRange;

        ContactFilter2D filter;
        readonly List<Collider2D> hits = new();

        void Awake()
        {

            CheckRange = GetComponent<Collider2D>();

            var comps = GetComponentsInChildren<SpriteRenderer>();
            foreach (var comp in comps)
            {
                spriteRenderers.Add(comp);
            }

            filter.useLayerMask = true;
            filter.layerMask = LayerMask.GetMask("UnitsPlayer");
            filter.useTriggers = true;
        }


        void Update()
        {
            CheckFadePlayer();
            UpdateFade();
        }

        bool IsPlayerInsideRange()
        {
            hits.Clear();
            int cnt = CheckRange.OverlapCollider(filter, hits);
            if (cnt == 0) return false;
            for (int i = 0; i < cnt; i++)
            {
                if (hits[i] != null && hits[i].GetComponentInParent<PlayerScenePresenter>() != null)
                    return true;
            }
            return false;
        }


        void CheckFadePlayer()
        {
            if(MainGameManager.Instance == null || MainGameManager.Instance.playerScenePresenter == null)
            {
                return;
            }

            var dist = (MainGameManager.Instance.playerScenePresenter.transform.position - transform.position);
            dist.z = 0;
            if(dist.sqrMagnitude > 3.0 * 3.0)
            {
                return;
            }

            bool inside = IsPlayerInsideRange();
            bool behind = true;
            //bool behind = false;
            //if (inside)
            //{
            //    behind = MainGameManager.Instance.playerScenePresenter.transform.position.y >
            //             (transform.position.y); //
            //}

            if (inside && behind) SetFade(0.3f);
            else SetFade(1.0f);
        }

        void UpdateFade()
        {
            // 以固定速率向 targetAlpha 靠拢
            float next = Mathf.MoveTowards(currAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

            if (Mathf.Abs(next - currAlpha) > Mathf.Epsilon)
            {
                currAlpha = next;
                if (spriteRenderers != null)
                {
                    foreach (var ceil in spriteRenderers)
                    {
                        ceil.color = new Color(ceil.color.r, ceil.color.g, ceil.color.b, currAlpha);
                    }
                }
            }
            else
            {
                if (currAlpha == targetAlpha)
                {
                    return;
                }
                currAlpha = targetAlpha;
                if (spriteRenderers != null)
                {
                    foreach (var ceil in spriteRenderers)
                    {
                        ceil.color = new Color(ceil.color.r, ceil.color.g, ceil.color.b, currAlpha);
                    }
                }
            }
        }

        public void SetFade(float targetAlpha)
        {
            this.targetAlpha = targetAlpha;
        }

    }
}