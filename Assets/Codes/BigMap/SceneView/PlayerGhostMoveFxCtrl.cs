using DG.Tweening;
using UnityEngine;

namespace My.Map.View
{

    public class PlayerGhostMoveFxCtrl : MonoBehaviour
    {
        [Header("References")]
        public SpriteRenderer playerSR;       // 玩家显示组件
        public GameObject orbPrefab;          // FX_Orb2D 预制体（独立特效）

        [Header("Timings")]
        public float playerFadeOut = 0.2f;    // 玩家隐去时间
        public float orbFadeIn = 0.15f;       // 光点淡入时间
        public float orbMoveDuration = 0.5f;  // 光点飞行时间
        public float orbFadeOut = 0.15f;      // 光点淡出时间
        public float playerFadeIn = 0.2f;     // 玩家恢复显示时间

        [Header("Ease")]
        public Ease moveEase = Ease.InOutCubic;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="player"></param>
        /// <param name="targetPos"></param>
        /// <param name="onFXReachTarget"></param>
        /// <param name="onFXComplete"></param>
        public void PlayMoveFx(Transform player, Vector3 targetPos, System.Action onFXReachTarget, System.Action onFXComplete)
        {
            // 1) 生成光点特效（建议对象池管理，这里用简单 Instantiate）
            //var orbGO = Instantiate(orbPrefab);
            //orbGO.transform.position = player.position;
            //orbGO.SetActive(true);
            this.gameObject.SetActive(true);
            this.transform.position = player.position;

            // 2) 取组件
            var orbSR = GetComponentInChildren<SpriteRenderer>();
            var trail = GetComponentInChildren<TrailRenderer>();
            var psList = GetComponentsInChildren<ParticleSystem>(); // 可多个

            // 3) 初始化可见性与粒子
            SetAlpha(playerSR, 1f);
            SetAlpha(orbSR, 0f);
            foreach (var ps in psList) { ps.Clear(); ps.Play(); }
            if (trail) trail.Clear();

            // 4) 构建显示用序列
            Sequence seq = DOTween.Sequence();

            // 玩家淡出（仅显示侧，不移动玩家）
            seq.Append(DOTween.To(() => playerSR.color, c => playerSR.color = c,
                new Color(playerSR.color.r, playerSR.color.g, playerSR.color.b, 0f), playerFadeOut));

            // 光点淡入
            seq.Join(DOTween.To(() => orbSR.color, c => orbSR.color = c,
                new Color(orbSR.color.r, orbSR.color.g, orbSR.color.b, 1f), orbFadeIn));

            // 隐藏玩家渲染器（可避免动画影响）
            seq.AppendCallback(() => playerSR.enabled = false);

            // 光点飞行到目标
            seq.Append(transform.DOMove(targetPos, orbMoveDuration).SetEase(moveEase));

            // 抵达：通知逻辑层可进行实际“瞬移”
            seq.AppendCallback(() =>
            {
                // 可触发一个抵达爆闪（提升 emission 或触发独立发射器）
                Burst(psList);
                onFXReachTarget?.Invoke();
            });

            // 光点淡出
            seq.Append(DOTween.To(() => orbSR.color, c => orbSR.color = c,
                new Color(orbSR.color.r, orbSR.color.g, orbSR.color.b, 0f), orbFadeOut));

            // 逻辑层此时应已把 player.position = targetPos
            // 显示层恢复玩家
            seq.AppendCallback(() => {
                playerSR.enabled = true;
                SetAlpha(playerSR, 0f);
            });
            seq.Append(DOTween.To(() => playerSR.color, c => playerSR.color = c,
                new Color(playerSR.color.r, playerSR.color.g, playerSR.color.b, 1f), playerFadeIn));

            // 收尾与回调
            seq.OnComplete(() =>
            {
                foreach (var ps in psList) { ps.Stop(); }
                //Destroy(orbGO); // 或归还对象池
                onFXComplete?.Invoke();
            });
        }

        void SetAlpha(SpriteRenderer sr, float a)
        {
            var c = sr.color; c.a = a; sr.color = c;
        }

        void Burst(ParticleSystem[] psList)
        {
            //foreach (var ps in psList)
            //{
            //    var emission = ps.emission;
            //    // 临时提高发射率作为简单爆闪
            //    var original = emission.rateOverTime.constant;
            //    emission.rateOverTime = original * 3f;
            //    DOVirtual.DelayedCall(0.12f, () => emission.rateOverTime = original);
            //}
        }
    }
}