using System.Collections;
using System.Collections.Generic;
using My.Map.Fight;
using UnityEngine;

namespace My
{

    public class MapProjectile : MonoBehaviour
    {
        public LogicProjectileInfo bindingProjInfo;

        public int WarnEffectId;

        public Transform ViewRoot;

        public event System.Action<Vector2> EventEntityHit;

        // 钩爪 bullet 发射后广播：(projectile, casterId)
        public static event System.Action<MapProjectile, long> GrappleHookFired;

        private IMapProjectileMotion _motion;
        private Transform _body;
        private Transform _shadow;           // 仅抛物用
        private SpriteRenderer _shadowSR;

        private float _lifetime;
        private bool _despawned;
        private bool _motionFrozen;

        public void SetMotionFrozen(bool frozen)
        {
            _motionFrozen = frozen;
        }

        private void Awake()
        {
            ViewRoot = transform.Find("view");
        }

        private IMapProjectileMotion CreateProjectileMotion(MotionDataBase motionData)
        {
            switch (motionData)
            {
                case LinearMotionData linerData:
                    {
                        return new MapProjectileLinearMotion();
                    }
                case InstanceMotionData instanceData:
                    {
                        return new MapProjectileInstanceMotion();
                    }
                case ParabolaMotionData motion2:
                    {
                        return new MapProjectileParabolaMotion();
                    }
            }
            return null;
        }

        // 外部发射接口
        public void Launch(LogicProjectileInfo info, Transform homingTarget = null)
        {
            if (info == null)
            {
                Debug.LogError("Projectile data  missing.");
                Destroy(gameObject);
                return;
            }

            this.bindingProjInfo = info;

            // 实例化可视
            SetupBodyAndShadow();

            // 创建Motion实例并初始化
            _motion = CreateProjectileMotion(info.pData.motionData);

            _motion.Initialize(this);

            // 初始放置
            transform.position = MainGameManager.Instance.GetWorldPosFromLogicPos(info.spawnPos);
            //if (_body != null) _body.position = position;

            _lifetime = info.pData.maxLifetime;
            _despawned = false;

        if (info.pData.id == GrappleHookSpecs.BulletId)
        {
            var hookCtrl = GetComponent<GrappleHookLineCtrl>();
            hookCtrl?.InitFromLaunch();
            GrappleHookFired?.Invoke(this, info.ownerEntity?.Id ?? 0);
        }
        }

        public void NotifyEntityHit(Vector2 hitWorldPos)
        {
            EventEntityHit?.Invoke(hitWorldPos);
        }

        private void FixedUpdate()
        {
            if (_despawned || _motion == null) return;

            float dt = Time.fixedDeltaTime;

            if (!_motionFrozen)
            {
                _motion.Tick(dt);
                transform.position = _motion.Position;
            }

            if (_motionFrozen || _motion.IsFinished)
            {
                if (TryDeferDespawn())
                {
                    return;
                }

                if (_motion.IsFinished)
                {
                    Despawn();
                }
            }
        }

        bool TryDeferDespawn()
        {
            var hookCtrl = GetComponent<GrappleHookLineCtrl>();
            if (hookCtrl == null)
            {
                return false;
            }

            return hookCtrl.TryDeferDespawn();
        }


        private void LateUpdate()
        {
            TickWarnPreview();
        }

        private void TickWarnPreview()
        {
            if (!bindingProjInfo.pData.showRangeWarn)
            {
                return;
            }

            if (bindingProjInfo.pData.ProjShape.Type == My.Map.Fight.FightStruct.EShapeType.None)
            {
                return;
            }

            if (WarnEffectId == 0)
            {
                MainGameManager.Instance.ShowRangeWarnEffect(bindingProjInfo.pData.ProjShape, _motion.Position, _motion.Forward, 999, Vector2.zero);
            }

            if (WarnEffectId != 0)
            {
                MainGameManager.Instance.UpdateRangeWarnEffect(WarnEffectId, _motion.Position, _motion.Forward);
            }
        }

        private void SetupBodyAndShadow()
        {
            if (ViewRoot == null)
                ViewRoot = transform.Find("view") ?? transform;

            _body = ViewRoot.Find("body");
            if (_body == null)
            {
                for (int i = 0; i < ViewRoot.childCount; i++)
                {
                    var child = ViewRoot.GetChild(i);
                    if (IsShadowNode(child.name))
                        continue;
                    if (bindingProjInfo.pData.showRangeWarn && child.name == "Circle")
                    {
                        child.gameObject.SetActive(false);
                        continue;
                    }
                    if (child.GetComponentInChildren<SpriteRenderer>() != null)
                    {
                        _body = child;
                        break;
                    }
                }
            }

            _shadow = ViewRoot.Find("shadow") ?? ViewRoot.Find("Showdow");
            if (_shadow == null)
            {
                _shadow = transform.Find("shadow") ?? transform.Find("Showdow");
            }
            _shadowSR = _shadow != null ? _shadow.GetComponentInChildren<SpriteRenderer>() : null;
        }

        private static bool IsShadowNode(string name)
        {
            return name.Equals("shadow", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("Showdow", System.StringComparison.OrdinalIgnoreCase);
        }

        // 供抛物Motion调用：配置视觉元素
        public void ConfigureParabolaVisual(ParabolaMotionData md)
        {
            if (_shadow == null)
            {
                //if (data.shadowPrefab != null)
                //{
                //    _shadow = Instantiate(data.shadowPrefab, transform).transform;
                //    _shadowSR = _shadow.GetComponentInChildren<SpriteRenderer>();
                //}
                //else
                //{
                //    _shadow = new GameObject("Shadow").transform;
                //    _shadow.SetParent(transform, false);
                //    var sr = _shadow.gameObject.AddComponent<SpriteRenderer>();
                //    sr.color = new Color(0, 0, 0, 0.5f);
                //    _shadowSR = sr;
                //}
            }
        }

        // 供抛物Motion调用：每帧更新可视
        public void UpdateParabolaVisual(Vector2 groundPos, float z, Vector2 forward)
        {
            var md = (ParabolaMotionData)bindingProjInfo.pData.motionData;
            // body 抬升
            Vector2 bodyPos = new Vector2(groundPos.x, groundPos.y + z * md.lift);
            //if (_body != null) _body.position = bodyPos;

            if (_body != null)
                _body.localPosition = new Vector3(0, z * md.lift, 0);

            if (bindingProjInfo.pData.rotateBodyToVelocity && _body != null && forward.sqrMagnitude > 0.0001f)
                _body.right = forward;

            // shadow 在地面
            if (_shadow != null)
            {
                if (_shadow.parent == ViewRoot)
                    _shadow.localPosition = Vector3.zero;
                else
                    _shadow.position = groundPos;
                float zAbs = Mathf.Max(0f, z);
                float t = Mathf.Clamp01(zAbs / 5f);
                float scale = Mathf.Lerp(1f, 0.5f, t);
                _shadow.localScale = Vector3.one * scale;

                if (_shadowSR != null)
                {
                    float a = Mathf.Lerp(1f, 0.5f, t);
                    var c = _shadowSR.color; c.a = a; _shadowSR.color = c;
                }
            }
        }

        private void Despawn()
        {
            if (_despawned) return;
            _despawned = true;

            if (WarnEffectId != 0)
            {
                MainGameManager.Instance.DestroySceneFxEffect(WarnEffectId);
                WarnEffectId = 0;
            }
            Destroy(gameObject);
        }
    }


}
