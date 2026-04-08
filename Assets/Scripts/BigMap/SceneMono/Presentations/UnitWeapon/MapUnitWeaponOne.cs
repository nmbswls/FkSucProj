using System.Collections;
using System.Collections.Generic;
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

        public string WeaponName;
        public Animator weaponAnim;
        public AnimancerComponent weaponAnimancer;
        public MapUnitWeaponCtrl WeaponCtrl;

        public AnimationClip innerShowClip;

        public long HitId = 0;
        private float _durationTimer = 0;

        public ParticleSystem[] slashEffects;
        //public GameObject[] ColliderArray;

        public float WeaponViewLength; // 武器视觉长度


        [Header("武器部件列表")]
        public WeaponPart[] weaponParts;


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

        public void PlaySlash(int idx) 
        {
            if (idx < 0 || idx >= slashEffects.Length) return;
            slashEffects[idx].Play(); 
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            var scenePresenter = other.GetComponentInParent<IScenePresentation>();
            if (scenePresenter == null) return;
            if(scenePresenter.GetLogicEntity() == null)
            {
                Debug.LogError("MapUnitWeaponOne OnTriggerEnter2D triiger no binding logic");
                return;
            }

            if(scenePresenter is not SceneUnitPresenter
                && scenePresenter is not SceneDestroyObjPresenter)
            {
                return;
            }
            WeaponCtrl.OnWeaponTriggerHit(HitId, scenePresenter.GetLogicEntity());
        }

        private void Update()
        {
            if(HitId != 0 && LogicTime.time >= this._durationTimer)
            {
                ClearWeapon();
            }

            if(HitId != 0)
            {
                WeaponCtrl.UnitPresenter.UnitEntity.HitWindowRegistry.activeHitWindows.TryGetValue(HitId, out var window);
                if(window == null)
                {
                    //Debug.LogError("trigger baodi clear");
                    ClearWeapon();
                }
            }
        }

        //public void OnWeaponAimDirUpdate(Vector2 aimDir)
        //{
        //    if (aimDir == Vector2.zero) return;

        //    // 1. 处理所有部件的旋转和翻转
        //    float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        //    bool isLookingLeft = aimDir.x < 0;

        //    foreach (var part in weaponParts)
        //    {
        //        // 每个部件加上自己的固定偏移角（如果有的话）
        //        part.rotator.rotation = Quaternion.AngleAxis(baseAngle + part.angleOffset, Vector3.forward);
        //        if(part.spriteVisual != null)
        //        {
        //            part.spriteVisual.flipY = isLookingLeft;
        //        }

        //        if(part.NeedFlipObjs != null)
        //        {
        //            foreach(var oneObj in part.NeedFlipObjs)
        //            {
        //                oneObj.localScale = new Vector3(1, isLookingLeft ? 1 : -1, 1);
        //            }
        //        }
        //    }

        //    // 2. 整体椭圆轨道位移 (和之前一样)
        //    float targetX = aimDir.x * maxPositionOffset.x;
        //    float targetY = aimDir.y * maxPositionOffset.y;
        //    Vector3 targetPos = new Vector3(targetX, targetY, 0);

        //    transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * posLerpSpeed);
        //}

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
                part.rotator.rotation = Quaternion.AngleAxis(realAimAngle + part.angleOffset, Vector3.forward);

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

            Vector2 localDir = aimDir;
            if (!isFacingRight)
            {
                localDir.x *= -1f;
            }

            float orbitAngle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg;

            orbitAngle = Mathf.Clamp(orbitAngle, minAngle, maxAngle);
            // 用受限的角度计算椭圆上的点
            float rad = orbitAngle * Mathf.Deg2Rad;
            Vector2 targetPos = new Vector2(
                Mathf.Cos(rad) * radiusX,
                Mathf.Sin(rad) * radiusY
            );

            // 需求 2：加上轨迹中点偏移
            targetPos += centerOffset;

            // 【核心技巧：镜像输出】如果角色实际上是朝左的，把X坐标再翻转回去
            if (!isFacingRight)
            {
                targetPos.x *= -1f;
            }

            // 最后应用平滑移动 (保留了你原有的 Lerp 设计，使动作更顺滑)
            Vector3 finalPos = new Vector3(targetPos.x, targetPos.y, 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, finalPos, Time.deltaTime * posLerpSpeed);
        }

        public void ShowWeapon(long hitId, float duration, string weaponAnimName)
        {
            this.HitId = hitId;
            gameObject.SetActive(true);

            this._durationTimer = LogicTime.time + duration;

            // 先尝试获取 clip 长度（简单版：按 clip 名匹配）
            if(weaponAnimancer != null)
            {
                var clipRes = SimpleResManager.Load<AnimationClip>($"Anim/player/{weaponAnimName}");

                var clipLenSec = clipRes.length;
                var speed = 1.0f;
                if (clipLenSec != -1)
                {
                    speed = clipLenSec / duration;
                }
                var state = weaponAnimancer.Play(clipRes);
                state.Speed = speed;
            }
        }

        public void ClearWeapon()
        {
            gameObject.SetActive(false);
            HitId = 0;
            _durationTimer = 0;
        }
    }
}



