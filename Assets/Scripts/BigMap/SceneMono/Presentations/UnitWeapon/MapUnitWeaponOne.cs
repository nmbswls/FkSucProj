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

        [Header("轨道位移配置")]
        public Vector2 maxPositionOffset = new Vector2(0.2f, 0.4f);
        public float posLerpSpeed = 15f;

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
                ClearWeapon(HitId);
            }
        }

        public void OnWeaponAimDirUpdate(Vector2 aimDir)
        {
            if (aimDir == Vector2.zero) return;

            // 1. 处理所有部件的旋转和翻转
            float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
            bool isLookingLeft = aimDir.x < 0;

            foreach (var part in weaponParts)
            {
                // 每个部件加上自己的固定偏移角（如果有的话）
                part.rotator.rotation = Quaternion.AngleAxis(baseAngle + part.angleOffset, Vector3.forward);
                if(part.spriteVisual != null)
                {
                    part.spriteVisual.flipY = isLookingLeft;
                }

                if(part.NeedFlipObjs != null)
                {
                    foreach(var oneObj in part.NeedFlipObjs)
                    {
                        oneObj.localScale = new Vector3(1, isLookingLeft ? 1 : -1, 1);
                    }
                }
            }

            // 2. 整体椭圆轨道位移 (和之前一样)
            float targetX = aimDir.x * maxPositionOffset.x;
            float targetY = aimDir.y * maxPositionOffset.y;
            Vector3 targetPos = new Vector3(targetX, targetY, 0);

            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * posLerpSpeed);
        }

        //public void OnWeaponAimDirUpdate(Vector2 aimDir)
        //{
        //    float angle = 0;
        //    if (aimDir.x >= 0)
        //    {
        //        angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
        //    }
        //    else
        //    {
        //        angle = Mathf.Atan2(aimDir.y, -aimDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
        //    }

        //    for (int i = 0; i < transform.childCount; i++)
        //    {
        //        var child = transform.GetChild(i);
        //        child.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
        //    }


        //    if (aimDir.x >= 0)
        //    {
        //        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        //    }
        //    else
        //    {
        //        transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        //    }


        //    bool isFacingUp = aimDir.y > 0.1f;
        //    // 2. 处理位移补偿 (关键!)
        //    // 假设原始 Pivot 在 (0, 0), 向上时移到 (0, 0.5)
        //    Vector3 targetPos = isFacingUp ? new Vector3(0, 0.1f, 0) : Vector3.zero;
        //    //this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, targetPos, Time.deltaTime * 10f);
        //}

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

        public void ClearWeapon(long hitId)
        {
            if(hitId != HitId)
            {
                return;
            }
            gameObject.SetActive(false);
            HitId = 0;
            _durationTimer = 0;
        }
    }
}



