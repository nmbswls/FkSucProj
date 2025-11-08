using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapProjectile : MonoBehaviour
{
    public LogicProjectileInfo bindingProjInfo;

    private IMapProjectileMotion _motion;
    private Transform _body;
    private Transform _shadow;           // 仅抛物用
    private SpriteRenderer _shadowSR;

    private float _lifetime;
    private bool _despawned;

    private IMapProjectileMotion CreateProjectileMotion(MotionDataBase motionData)
    {
        switch(motionData)
        {
            case LinearMotionData linerData:
                {
                    return new MapProjectileLinearMotion();
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
    }

    private void FixedUpdate()
    {
        if (_despawned || _motion == null) return;

        float dt = Time.fixedDeltaTime;
        _motion.Tick(dt);

        // 更新本体可视（非抛物：直接贴 Position）
        if (!(_motion is MapProjectileParabolaMotion))
        {
            Vector2 pos = _motion.Position;
            transform.position = pos;
            if (_body != null) _body.position = pos;
            if (bindingProjInfo.pData.rotateBodyToVelocity && _body != null)
            {
                var fwd = _motion.Forward;
                if (fwd.sqrMagnitude > 0.0001f) _body.right = fwd;
            }
        }

        if (_motion.IsFinished)
        {
            Despawn();
        }
    }

    private void SetupBodyAndShadow()
    {
        // 清理旧的
        //foreach (Transform child in transform) Destroy(child.gameObject);

        //// Body
        //if (bindingProjInfo.data.bodyPrefab != null)
        //    _body = Instantiate(bindingProjInfo.data.bodyPrefab, transform).transform;
        //else
        //    _body = transform;

        // Shadow 先不创建；仅抛物会要求
        _shadow = null;
        _shadowSR = null;
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
        //var md = (ParabolaMotionData)data.motionData;
        //// body 抬升
        //Vector2 bodyPos = new Vector2(groundPos.x, groundPos.y + z * md.lift);
        //if (_body != null) _body.position = bodyPos;

        //// 朝向
        //if (data.rotateBodyToVelocity && _body != null && forward.sqrMagnitude > 0.0001f)
        //    _body.right = forward;

        //// shadow 在地面
        //if (_shadow != null)
        //{
        //    _shadow.position = groundPos;
        //    float zAbs = Mathf.Max(0f, z);
        //    float scale = md.shadowScaleByZ.Evaluate(zAbs);
        //    _shadow.localScale = Vector3.one * scale;

        //    if (_shadowSR != null)
        //    {
        //        float a = md.shadowAlphaByZ.Evaluate(zAbs);
        //        var c = _shadowSR.color; c.a = a; _shadowSR.color = c;
        //    }
        //}
    }

    private void Despawn()
    {
        if (_despawned) return;
        _despawned = true;
        Destroy(gameObject);
    }
}
