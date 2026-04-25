using System.Collections;
using UnityEngine;

namespace My.Map
{
    // 内城设施旁背景 NPC：池化实例 + 工作/休息两点循环
    public class HomeBgNpc : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Animator _anim;

        public int LastStyleId { get; private set; }
        public bool IsActive { get; private set; }

        private Coroutine _routine;
        private float _moveSpeed = 1.2f;

        private void Awake()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            _anim = GetComponent<Animator>();
        }

        public void ApplyStyle(int styleId)
        {
            LastStyleId = styleId;
            if (_sr != null)
            {
                _sr.color = HomeBgNpcPool.GetStyleTint(styleId);
            }
        }

        public void BeginFacilityWorkRestLoop(Vector3 workPos, Vector3 restPos, float workSeconds, float restSeconds)
        {
            StopFacilityRoutine();
            _routine = StartCoroutine(FacilityLoop(workPos, restPos, workSeconds, restSeconds));
        }

        public void StopFacilityRoutine()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            IsActive = false;
            if (_anim != null)
            {
                _anim.speed = 0f;
            }
        }

        private IEnumerator FacilityLoop(Vector3 workPos, Vector3 restPos, float workSeconds, float restSeconds)
        {
            IsActive = true;
            while (gameObject.activeInHierarchy)
            {
                yield return MoveTo(workPos);
                yield return PlayIdle(workSeconds, atWork: true);
                yield return MoveTo(restPos);
                yield return PlayIdle(restSeconds, atWork: false);
            }
        }

        private IEnumerator MoveTo(Vector3 target)
        {
            if (_anim != null && _anim.runtimeAnimatorController != null)
            {
                _anim.speed = 0.35f;
                _anim.Play("Walk", 0, Random.value);
            }

            while ((target - transform.position).sqrMagnitude > 0.02f)
            {
                Vector3 dir = (target - transform.position).normalized;
                transform.position += dir * (_moveSpeed * Time.deltaTime);
                if (_sr != null && Mathf.Abs(dir.x) > 0.05f)
                {
                    _sr.flipX = dir.x < 0;
                    _sr.sortingOrder = -(int)(transform.position.y * 100);
                }

                yield return null;
            }
        }

        private IEnumerator PlayIdle(float seconds, bool atWork)
        {
            if (_anim != null && _anim.runtimeAnimatorController != null)
            {
                _anim.speed = 1f;
                _anim.Play(atWork ? "Working" : "Idle", 0, 0f);
            }

            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }
}
