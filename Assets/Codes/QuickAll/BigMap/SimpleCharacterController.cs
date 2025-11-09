using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Map.Scene
{

    public class SimpleCharacterController : MonoBehaviour
    {

        private Rigidbody2D rb;
        private Collider2D col;
        private ContactFilter2D contactFilter;

        [Header("Collision")]
        public LayerMask collisionMask;       // 静态障碍层
        public float skin = 0.02f;            // 与墙保持的安全距离
        public int maxSlideIterations = 3;    // 碰撞迭代次数
        public float minMove = 0.0005f;       // 终止阈值

        public SceneUnitPresenter unitPresenter;
        private RaycastHit2D[] castHits = new RaycastHit2D[18];

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            rb.bodyType = RigidbodyType2D.Kinematic; // 关键：运动学刚体
            rb.useFullKinematicContacts = true;      // 让接触事件更准确（可选）
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // 设定碰撞过滤
            contactFilter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = collisionMask,
                useTriggers = false
            };

            unitPresenter = GetComponent<SceneUnitPresenter>();
        }

        void FixedUpdate()
        {
            var velocity = unitPresenter.UnitEntity.activeMoveVec + unitPresenter.UnitEntity.externalVel;
            Vector2 delta = velocity * Time.fixedDeltaTime;
            if (delta.sqrMagnitude < minMove)
            {
                rb.MovePosition(rb.position); // 保持插值
                return;
            }

            Vector2 newPos = KinematicMove(rb.position, delta);
            rb.MovePosition(newPos);
        }

        Vector2 KinematicMove(Vector2 startPos, Vector2 delta)
        {
            Vector2 pos = startPos;
            Vector2 remaining = delta;

            for (int i = 0; i < maxSlideIterations; i++)
            {
                if (remaining.sqrMagnitude < minMove) break;

                // 3) 使用 Collider2D.Cast 进行扫掠，检测前方是否有障碍
                int hitCount = col.Cast(remaining.normalized, contactFilter, castHits, remaining.magnitude + skin);
                if (hitCount == 0)
                {
                    // 无碰撞，直接移动
                    pos += remaining;
                    break;
                }

                // 取最近的命中
                RaycastHit2D hit = ClosestHit(castHits, hitCount);

                // 4) 将位移截断至碰撞点前，留出 skin
                float travel = Mathf.Max(0f, hit.distance - skin);
                Vector2 move = Vector2.ClampMagnitude(remaining, travel);
                pos += move;

                // 5) 计算滑动方向：将剩余位移沿法线切线分量继续
                remaining -= move;
                Vector2 n = hit.normal.normalized;
                // 将 remaining 投影到碰撞面的切线方向：r' = r - dot(r, n)*n
                remaining = remaining - Vector2.Dot(remaining, n) * n;

                // 6) 防止数值抖动
                if (remaining.sqrMagnitude < minMove) break;
            }

            return pos;
        }

        RaycastHit2D ClosestHit(RaycastHit2D[] hits, int count)
        {
            int idx = -1;
            float minDist = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider == null) continue;
                if (hits[i].distance < minDist)
                {
                    minDist = hits[i].distance;
                    idx = i;
                }
            }
            return idx >= 0 ? hits[idx] : default;
        }
    }
}

