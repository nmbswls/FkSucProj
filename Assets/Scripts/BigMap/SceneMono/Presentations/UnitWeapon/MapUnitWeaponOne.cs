using Animancer;
using UnityEngine;

namespace My.Map.Scene
{

    // 定义单个武器部件的数据结构
    [System.Serializable]
    public class WeaponPart
    {
        public Transform rotator;          // 旋转节点
        public SpriteRenderer spriteVisual; // 渲染节点

        public Transform[] NeedFlipObjs;

        // 可选：某些部件可能需要轻微的瞄准延迟或角度偏移
        public float angleOffset = 0f;
    }

    public class MapUnitWeaponOne : MonoBehaviour
    {
        /// <summary>
        /// 武器显示状态
        /// </summary>
        public bool IsShown;

        // 攻击结束后仍保持可见（人类武器快捷栏常驻装备）
        public bool KeepVisibleWhenIdle;

        public string WeaponName;
        public Animator weaponAnim;
        public AnimancerComponent weaponAnimancer;
        public MapUnitWeaponCtrl WeaponCtrl;

        public AnimationClip innerShowClip;

        public long HitId = 0;
        private float _durationTimer = 0;

        public ParticleSystem[] slashEffects;

        public float WeaponViewLength; // 武器视觉长度

        [Header("武器部件列表")]
        public WeaponPart[] weaponParts;

        [Header("代码挥舞（攻击时）")]
        public bool UseCodeSwing;
        public float SwingStartAngle;
        public float SwingEndAngle;
        [Tooltip("朝右语义：true=从高角挥向低角；false=从低角挥向高角。朝左由 facingSign 镜像，与顺时针无关。")]
        public bool SwingTopToBottom = true;
        public Collider2D HitCollider;

        [Header("轨道位移与裁剪配置")]
        public float posLerpSpeed = 15f;
        [Tooltip("轨道横向半径")]
        public float radiusX = 0.2f;
        [Tooltip("轨道纵向半径 (压扁制造2D透视)")]
        public float radiusY = 0.4f;
        [Tooltip("向上瞄准的最大角度限制 (基于角色朝右时计算)")]
        public float maxAngle = 60f;
        [Tooltip("向下瞄准的最大角度限制 (防穿模核心)")]
        public float minAngle = -60f;
        [Tooltip("轨道的中心偏移量 (x>0代表往脸前推)")]
        public Vector2 centerOffset = new Vector2(-0.15f, -0f);

        bool _isSwinging;
        float _swingStartTime;
        float _swingDuration;
        float _aimBaseDeg;
        bool _aimFacingRight;

        public bool IsSwinging => _isSwinging;

        void Awake()
        {
            if (UseCodeSwing && HitCollider != null)
            {
                HitCollider.enabled = false;
            }
        }

        public void PlaySlash(int idx)
        {
            if (idx < 0 || idx >= slashEffects.Length) return;
            slashEffects[idx].Play();
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            var scenePresenter = other.GetComponentInParent<IScenePresentation>();
            if (scenePresenter == null) return;
            if (scenePresenter.GetLogicEntity() == null)
            {
                Debug.LogError("MapUnitWeaponOne OnTriggerEnter2D triiger no binding logic");
                return;
            }

            if (scenePresenter is not SceneUnitPresenter
                && scenePresenter is not SceneDestroyObjPresenter)
            {
                return;
            }

            Vector2? hitPoint = null;
            if (HitCollider != null)
            {
                var distance = HitCollider.Distance(other);
                hitPoint = distance.isValid ? distance.pointB : other.ClosestPoint(HitCollider.bounds.center);
            }

            WeaponCtrl.OnWeaponTriggerHit(HitId, scenePresenter.GetLogicEntity(), hitPoint);
        }

        private void Update()
        {
            if (_isSwinging)
            {
                UpdateCodeSwingRotation();
            }

            if (HitId != 0 && LogicTime.time >= _durationTimer)
            {
                ClearWeapon();
            }

            if (HitId != 0)
            {
                WeaponCtrl.UnitPresenter.UnitEntity.HitWindowRegistry.activeHitWindows.TryGetValue(HitId, out var window);
                if (window == null)
                {
                    ClearWeapon();
                }
            }
        }

        /// <summary>
        /// 外部每帧调用，更新武器的瞄准位姿
        /// </summary>
        public void OnWeaponAimDirUpdate(Vector2 aimDir)
        {
            if (aimDir == Vector2.zero) return;

            bool isFacingRight = aimDir.x >= 0;
            float realAimAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;

            foreach (var part in weaponParts)
            {
                if (!_isSwinging)
                {
                    part.rotator.rotation = Quaternion.AngleAxis(
                        realAimAngle + (isFacingRight ? part.angleOffset : -part.angleOffset),
                        Vector3.forward);
                }

                if (part.spriteVisual != null)
                {
                    part.spriteVisual.flipY = !isFacingRight;
                }

                if (part.NeedFlipObjs != null)
                {
                    foreach (var oneObj in part.NeedFlipObjs)
                    {
                        oneObj.localScale = new Vector3(1, isFacingRight ? 1 : -1, 1);
                    }
                }
            }

            UpdateOrbitPosition(aimDir, isFacingRight);
        }

        void UpdateOrbitPosition(Vector2 aimDir, bool isFacingRight)
        {
            Vector2 localDir = aimDir;
            if (!isFacingRight)
            {
                localDir.x *= -1f;
            }

            float orbitAngle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;
            orbitAngle = Mathf.Clamp(orbitAngle, minAngle, maxAngle);

            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector2 targetPos = new Vector2(
                Mathf.Cos(rad) * radiusX,
                Mathf.Sin(rad) * radiusY
            );

            targetPos += centerOffset;

            if (!isFacingRight)
            {
                targetPos.x *= -1f;
            }

            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                new Vector3(targetPos.x, targetPos.y, 0),
                Time.deltaTime * posLerpSpeed);
        }

        public void ShowWeapon(long hitId, float duration, string weaponAnimName)
        {
            HitId = hitId;
            gameObject.SetActive(true);
            _durationTimer = LogicTime.time + duration;

            BeginCodeSwingIfNeeded(duration);

            if (weaponAnimancer == null || string.IsNullOrEmpty(weaponAnimName))
            {
                return;
            }

            var clipRes = SimpleResManager.Load<AnimationClip>($"Anim/weapon/{weaponAnimName}");
            if (clipRes == null)
            {
                Debug.LogWarning($"MapUnitWeaponOne ShowWeapon clip not found: {weaponAnimName}");
                return;
            }

            var clipLenSec = clipRes.length;
            var speed = 1.0f;
            if (clipLenSec > 0f)
            {
                speed = clipLenSec / duration;
            }

            var state = weaponAnimancer.Play(clipRes, 0.1f, FadeMode.FromStart);
            state.Speed = speed;
        }

        void BeginCodeSwingIfNeeded(float duration)
        {
            if (!UseCodeSwing)
            {
                return;
            }

            var look = WeaponCtrl != null && WeaponCtrl.UnitPresenter != null
                ? WeaponCtrl.UnitPresenter.UnitEntity.CurrentLook
                : Vector2.right;

            if (look == Vector2.zero)
            {
                look = Vector2.right;
            }

            _aimFacingRight = look.x >= 0;
            _aimBaseDeg = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
            _swingStartTime = LogicTime.time;
            _swingDuration = Mathf.Max(duration, 0.0001f);
            _isSwinging = true;

            if (HitCollider != null)
            {
                HitCollider.enabled = true;
            }

            ResetSpriteLocalRotation();
            UpdateCodeSwingRotation();
        }

        void UpdateCodeSwingRotation()
        {
            if (!_isSwinging || weaponParts == null)
            {
                return;
            }

            float t = Mathf.Clamp01((LogicTime.time - _swingStartTime) / _swingDuration);
            float swingDelta = InterpolateSwingAngle(SwingStartAngle, SwingEndAngle, SwingTopToBottom, t);
            float facingSign = _aimFacingRight ? 1f : -1f;

            foreach (var part in weaponParts)
            {
                if (part?.rotator == null)
                {
                    continue;
                }

                float signedOffset = _aimFacingRight ? part.angleOffset : -part.angleOffset;
                part.rotator.rotation = Quaternion.AngleAxis(
                    _aimBaseDeg + swingDelta * facingSign + signedOffset,
                    Vector3.forward);
            }
        }

        static float InterpolateSwingAngle(float start, float end, bool topToBottom, float t)
        {
            t = Mathf.Clamp01(t);
            float shortest = Mathf.DeltaAngle(start, end);
            // 最短路径角度减小 = 朝右语义下的「从上到下」
            bool shortestGoesDown = shortest < 0f;
            float delta = topToBottom == shortestGoesDown
                ? shortest
                : shortest > 0f ? shortest - 360f : shortest + 360f;
            return start + delta * t;
        }

        void EndCodeSwing()
        {
            if (!_isSwinging)
            {
                return;
            }

            _isSwinging = false;
            _swingDuration = 0f;

            if (HitCollider != null)
            {
                HitCollider.enabled = false;
            }
        }

        public void ClearWeapon()
        {
            EndCodeSwing();
            HitId = 0;
            _durationTimer = 0;

            if (!KeepVisibleWhenIdle)
            {
                gameObject.SetActive(false);
                return;
            }

            ResetEquipIdlePose();
        }

        // 常驻装备：攻击结束后清掉 Animancer 结束态，交回瞄准驱动
        void ResetEquipIdlePose()
        {
            if (weaponAnimancer != null)
            {
                weaponAnimancer.Stop();
            }

            ResetSpriteLocalRotation();

            if (weaponParts == null)
            {
                return;
            }

            foreach (var part in weaponParts)
            {
                if (part?.rotator != null)
                {
                    part.rotator.localRotation = Quaternion.identity;
                }
            }
        }

        void ResetSpriteLocalRotation()
        {
            if (weaponParts == null)
            {
                return;
            }

            foreach (var part in weaponParts)
            {
                if (part?.spriteVisual != null)
                {
                    part.spriteVisual.transform.localRotation = Quaternion.identity;
                }
            }
        }
    }
}

