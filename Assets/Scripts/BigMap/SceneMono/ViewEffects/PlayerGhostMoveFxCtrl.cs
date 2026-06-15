using DG.Tweening;
using My.Map.Scene;
using UnityEngine;

namespace My.Map.View
{
    public class PlayerGhostMoveFxCtrl : MonoBehaviour
    {
        public SpriteRenderer playerSR;
        public Ease moveEase = Ease.InOutCubic;

        public Sequence PlayMoveFx(
            SpriteRenderer playerSprite,
            Transform playerTransform,
            Vector3 targetPos,
            GhostRelocateTimings timings,
            System.Action onReachTarget,
            System.Action onPlayerFadeInStart,
            System.Action onComplete)
        {
            playerSR = playerSprite;
            gameObject.SetActive(true);
            transform.position = playerTransform.position;

            var orbSR = GetComponentInChildren<SpriteRenderer>();
            var trail = GetComponentInChildren<TrailRenderer>();
            var psList = GetComponentsInChildren<ParticleSystem>();

            SetAlpha(playerSR, 1f);
            if (orbSR != null)
            {
                SetAlpha(orbSR, 0f);
            }

            foreach (var ps in psList)
            {
                ps.Clear();
                ps.Play();
            }

            if (trail)
            {
                trail.Clear();
            }

            Sequence seq = DOTween.Sequence();

            seq.Append(DOTween.To(
                () => playerSR.color,
                c => playerSR.color = c,
                new Color(playerSR.color.r, playerSR.color.g, playerSR.color.b, 0f),
                timings.PlayerFadeOut));

            if (orbSR != null)
            {
                seq.Join(DOTween.To(
                    () => orbSR.color,
                    c => orbSR.color = c,
                    new Color(orbSR.color.r, orbSR.color.g, orbSR.color.b, 1f),
                    timings.OrbFadeIn));
            }

            seq.AppendCallback(() => playerSR.enabled = false);
            seq.Append(transform.DOMove(targetPos, timings.OrbMove).SetEase(moveEase));
            seq.AppendCallback(() =>
            {
                Burst(psList);
                onReachTarget?.Invoke();
            });

            if (orbSR != null)
            {
                seq.Append(DOTween.To(
                    () => orbSR.color,
                    c => orbSR.color = c,
                    new Color(orbSR.color.r, orbSR.color.g, orbSR.color.b, 0f),
                    timings.OrbFadeOut));
            }

            seq.AppendCallback(() =>
            {
                playerSR.enabled = true;
                SetAlpha(playerSR, 0f);
                onPlayerFadeInStart?.Invoke();
            });
            seq.Append(DOTween.To(
                () => playerSR.color,
                c => playerSR.color = c,
                new Color(playerSR.color.r, playerSR.color.g, playerSR.color.b, 1f),
                timings.PlayerFadeIn));

            seq.OnComplete(() =>
            {
                foreach (var ps in psList)
                {
                    ps.Stop();
                }

                onComplete?.Invoke();
            });

            return seq;
        }

        void SetAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null)
            {
                return;
            }

            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        void Burst(ParticleSystem[] psList)
        {
        }
    }
}
