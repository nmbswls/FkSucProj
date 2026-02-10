using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    [RequireComponent(typeof(Animator))]
    public class HomeBgNpc : MonoBehaviour
    {
        private Queue<Vector3> _pathQueue;
        private Vector3 _currentTarget;
        private Vector3 _pathOffset; // 个人专属的路径偏移量
        private float _speed;

        private SpriteRenderer _sr;
        private Animator _anim;

        public bool IsActive { get; private set; } = false;

        void Awake()
        {
            _sr = GetComponentInChildren<SpriteRenderer>();
            _anim = GetComponent<Animator>();
        }

        public void Init(Vector3 startPos, Queue<Vector3> pathRoute, Vector3 offset)
        {
            transform.position = startPos;
            _pathQueue = pathRoute;
            _pathOffset = offset;
            _speed = Random.Range(1.0f, 1.5f); // 随机速度

            // 随机颜色差异
            float grey = Random.Range(0.9f, 1f);
            _sr.color = new Color(grey, grey, grey, 1f);

            // 随机动画起始帧
            if (_anim)
            {
                _anim.Play("Walk", 0, Random.value);
                _anim.speed = _speed * 0.3f;
            }

            IsActive = true;
            gameObject.SetActive(true);

            SetNextTarget();
        }

        void SetNextTarget()
        {
            if (_pathQueue.Count > 0)
            {
                // 取出下一个点，并加上偏移量
                _currentTarget = _pathQueue.Dequeue() + _pathOffset;
            }
            else
            {
                // 没路了，回收
                Recycle();
            }
        }

        void Update()
        {
            if (!IsActive) return;

            // 1. 移动逻辑
            Vector3 dir = (_currentTarget - transform.position);
            float dist = dir.magnitude;

            // 如果距离很近，就算到达
            if (dist < 0.1f)
            {
                SetNextTarget();
                return;
            }

            Vector3 moveDir = dir.normalized;
            transform.position += moveDir * _speed * Time.deltaTime;

            // 2. 视觉表现 (Top-Down 专属)

            // A. 左右翻转 (根据 X 轴移动方向)
            if (Mathf.Abs(moveDir.x) > 0.1f)
            {
                _sr.flipX = moveDir.x < 0;
            }

            // B. 层级排序 (Y-Sorting)
            // 在 Top-Down 游戏中，Y 越大(越靠上)应该被遮挡，Order 越小
            // 将 Y 坐标映射为 Order，精度设为 100
            _sr.sortingOrder = -(int)(transform.position.y * 100);

            // C. (可选) 如果你有 4 方向动画，这里根据 moveDir 的 x/y 切换 Animator 状态
            // if (Mathf.Abs(moveDir.y) > Mathf.Abs(moveDir.x)) { Play("WalkUp/Down"); }
        }

        void Recycle()
        {
            IsActive = false;
            gameObject.SetActive(false);
        }
    }

}

