using System.Collections;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace My.Map.Scene
{
    public class MapUnitWeaponOne : MonoBehaviour
    {
        public Animator weaponAnim;
        public AnimancerComponent weaponAnimancer;
        public MapUnitWeaponCtrl WeaponCtrl;

        public AnimationClip innerShowClip;

        public long HitId = 0;
        private float _durationTimer = 0;

        public ParticleSystem[] slashEffects;
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
            float angle = 0;
            if (aimDir.x >= 0)
            {
                angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
            }
            else
            {
                angle = Mathf.Atan2(aimDir.y, -aimDir.x) * Mathf.Rad2Deg; // 与 +X 轴夹角
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward); // 绕 Z 轴
            }


            if (aimDir.x >= 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else
            {
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }


            bool isFacingUp = aimDir.y > 0.1f;
            // 2. 处理位移补偿 (关键!)
            // 假设原始 Pivot 在 (0, 0), 向上时移到 (0, 0.5)
            Vector3 targetPos = isFacingUp ? new Vector3(0, 0.1f, 0) : Vector3.zero;
            this.transform.localPosition = Vector3.Lerp(this.transform.localPosition, targetPos, Time.deltaTime * 10f);
        }

        public void ShowWeapon(long hitId, float duration, string weaponAnimName)
        {
            this.HitId = hitId;
            gameObject.SetActive(true);

            this._durationTimer = LogicTime.time + duration;

            // 先尝试获取 clip 长度（简单版：按 clip 名匹配）
            if(weaponAnim != null)
            {
                //float clipLenSec = -1f;
                //var rac = weaponAnim.runtimeAnimatorController;
                //if (rac != null)
                //{
                //    foreach (var clip in rac.animationClips)
                //    {
                //        if (clip != null)
                //        {
                //            clipLenSec = clip.length;
                //            break;
                //        }
                //    }
                //}
                //weaponAnim.speed = 1.0f;
                //if (clipLenSec != -1)
                //{
                //    var speed = clipLenSec / duration;
                //    weaponAnim.speed = speed;
                //}
                //weaponAnim.Play("Show", 0, 0f);
            }

            if(weaponAnimancer != null)
            {
                var clipLenSec = innerShowClip.length;
                var speed = 1.0f;
                if (clipLenSec != -1)
                {
                    speed = clipLenSec / duration;
                }
                var state = weaponAnimancer.Play(innerShowClip);
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



