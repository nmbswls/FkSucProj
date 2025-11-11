using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Map.Scene
{

    public class SimpleCharacterController : MonoBehaviour
    {
        [Header("Agent")]
        public float moveSpeed = 5f;
        public float maxAccel = 20f;
        public float damping = 0f;              // 线性阻尼（可选）

        [Header("Soft Separation (for Enemy)")]
        public bool enableSeparation = true;    // 玩家可关，敌人开
        public float separationRadius = 1.0f;
        public float separationStrength = 7f;
        public float separationDeadZone = 0.12f;
        public LayerMask separationQueryMask;        // 用于分离的“敌人层”（即其他敌人）
        public string separationTag;

        [Header("Dynamic Non-Penetration")]
        public LayerMask dynamicBlockQueryMask;   
        public float nonPenPadding = 0.05f;     // 最小间隙，减一点避免抖动
        public float dynamicQueryExtra = 1.0f;  // 查询半径冗余

        private Rigidbody2D rb;
        private CircleCollider2D circleCollider;
        private Vector2 velocity;               // 平滑速度
        private static Collider2D[] overlapBuf = new Collider2D[32]; // 复用缓冲

        private float radius;

        public Vector2 DesiredVel { get; set; } = Vector2.zero;


        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            circleCollider = GetComponent<CircleCollider2D>();
            radius = circleCollider.radius;

            separationRadius = radius * 1.5f;
        }


        void FixedUpdate()
        {
            var finalDesiredVel = DesiredVel;

            // 2) 敌人间软性散开（玩家可关闭）
            if (enableSeparation && separationStrength > 0f && separationRadius > 0f)
            {
                Vector2 sepVel = ComputeSeparationVelocity();
                finalDesiredVel += sepVel;
                if (finalDesiredVel.magnitude > moveSpeed)
                    finalDesiredVel = finalDesiredVel.normalized * moveSpeed;
            }

            // 3) 加速度限制与阻尼
            Vector2 accel = (finalDesiredVel - velocity) / Time.fixedDeltaTime;
            float maxA = Mathf.Max(1e-4f, maxAccel);
            if (accel.magnitude > maxA) accel = accel.normalized * maxA;
            velocity += accel * Time.fixedDeltaTime;
            if (damping > 0f) velocity *= Mathf.Clamp01(1f - damping * Time.fixedDeltaTime);

            // 4) 预估目标位置
            Vector2 targetPos = rb.position + velocity * Time.fixedDeltaTime;

            // 5) 对动态单位做“无推挤”的非穿透位置约束
            // 玩家约束敌人；敌人约束玩家（你也可以双向都约束，效果更饱满）
            targetPos = ProjectAgainstDynamics(targetPos, radius, dynamicBlockQueryMask);

            //TryMoveWithStaticSkin();

            rb.MovePosition(targetPos);
        }

        Vector2 ComputeSeparationVelocity()
        {
            int count = Physics2D.OverlapCircleNonAlloc(rb.position, separationRadius, overlapBuf, separationQueryMask);
            if (count <= 0) return Vector2.zero;

            Vector2 sum = Vector2.zero;
            int used = 0;
            for (int i = 0; i < count; i++)
            {
                var col = overlapBuf[i];
                if (!col) continue;
                var other = col.attachedRigidbody;
                if (!other || other == rb) continue;
                if (other.tag != separationTag) continue;

                Vector2 dir = rb.position - other.position;
                float dist = dir.magnitude;
                if (dist < separationDeadZone || dist < 1e-4f) continue;

                float w = Mathf.Clamp01(1f - dist / separationRadius);
                sum += dir / dist * w; // 归一化后按权重
                used++;
            }
            if (used == 0) return Vector2.zero;

            Vector2 sepDir = (sum / used).normalized;
            return sepDir * separationStrength;
        }

        //Vector2 ProjectAgainstDynamics(Vector2 tgt, float selfR, LayerMask mask)
        //{
        //    float queryR = selfR + dynamicQueryExtra;
        //    int count = Physics2D.OverlapCircleNonAlloc(tgt, queryR, overlapBuf, mask);
        //    for (int i = 0; i < count; i++)
        //    {
        //        var col = overlapBuf[i];
        //        if (!col) continue;
        //        var otherRb = col.attachedRigidbody;
        //        if (!otherRb || otherRb == rb) continue;

        //        float otherR = GetApproxRadius(col);
        //        Vector2 to = tgt - otherRb.position;
        //        float dist = to.magnitude;
        //        float minDist = selfR + otherR - Mathf.Max(0f, nonPenPadding);

        //        if (dist < minDist && dist > 1e-4f)
        //        {
        //            Vector2 dir = to / dist;
        //            tgt = otherRb.position + dir * minDist;
        //        }
        //        else if (dist <= 1e-4f)
        //        {
        //            // 完全重合时，用当前移动方向或固定方向
        //            Vector2 dir = (tgt - rb.position);
        //            if (dir.sqrMagnitude < 1e-6f) dir = Vector2.right;
        //            dir.Normalize();
        //            tgt = otherRb.position + dir * minDist;
        //        }
        //    }
        //    return tgt;
        //}

        Vector2 ProjectAgainstDynamics(Vector2 tgt, float selfR, LayerMask mask)
        {
            float queryR = selfR + dynamicQueryExtra;
            int count = Physics2D.OverlapCircleNonAlloc(tgt, queryR, overlapBuf, mask);
            if (count <= 0) return tgt;

            // 原始（未修正）目标方向，用于重合时的首选方向
            Vector2 origDir = tgt - rb.position;
            float origDirLen = origDir.magnitude;
            if (origDirLen > 1e-6f) origDir /= origDirLen; else origDir = Vector2.zero;

            Vector2 totalCorrection = Vector2.zero;

            // 限制每帧最大修正（可调参数，建议公开为 maxCorrectionPerStep）
            float maxCorrection = Mathf.Max(0.05f, 0.5f * moveSpeed * Time.fixedDeltaTime);

            // 迭代次数（1~2次即可）
            const int iters = 1;
            for (int k = 0; k < iters; k++)
            {
                bool anyOverlap = false;

                for (int i = 0; i < count; i++)
                {
                    var col = overlapBuf[i];
                    if (!col) continue;
                    var otherRb = col.attachedRigidbody;
                    if (!otherRb || otherRb == rb) continue;

                    float otherR = GetApproxRadius(col);

                    // 注意用“当前迭代中的临时目标”来测量与对方的距离
                    Vector2 curTgt = tgt + totalCorrection;
                    Vector2 to = curTgt - otherRb.position;
                    float dist = to.magnitude;
                    float minDist = selfR + otherR - Mathf.Max(0f, nonPenPadding);

                    if (dist < minDist)
                    {
                        anyOverlap = true;

                        Vector2 dir;
                        if (dist > 1e-4f)
                        {
                            dir = to / dist;
                        }
                        else
                        {
                            // 重合：优先用本帧原始移动方向；若无，则用当前位置与对方位置的方向；再不行随机微扰
                            dir = origDir;
                            if (dir.sqrMagnitude < 1e-6f)
                            {
                                Vector2 alt = ((Vector2)transform.position - otherRb.position);
                                if (alt.sqrMagnitude > 1e-6f) dir = alt.normalized;
                                else dir = new Vector2(1f, 0f); // 最后兜底
                            }
                        }

                        float push = (minDist - dist);
                        // 只做最小必要修正的增量投影
                        Vector2 corr = dir * push;

                        // 累计修正，但限制最大幅度
                        Vector2 newTotal = totalCorrection + corr;
                        if (newTotal.magnitude > maxCorrection)
                        {
                            newTotal = newTotal.normalized * maxCorrection;
                            totalCorrection = newTotal;
                            // 达到上限就提前退出，避免大跳和振荡
                            break;
                        }
                        else
                        {
                            totalCorrection = newTotal;
                        }
                    }
                }

                // 若本轮没有任何重叠，提前结束
                if (!anyOverlap) break;
            }

            return tgt + totalCorrection;
        }


        float GetApproxRadius(Collider2D col)
        {
            // 估算对方的等效圆半径，可改为读取对方 TopDownAgent.radius
            if (col is CircleCollider2D cc)
                return cc.radius * Mathf.Max(col.transform.lossyScale.x, col.transform.lossyScale.y);
            if (col is CapsuleCollider2D cap)
                return Mathf.Max(cap.size.x, cap.size.y) * 0.5f;
            if (col is BoxCollider2D bc)
                return Mathf.Max(bc.size.x, bc.size.y) * 0.5f;
            return radius;
        }

        bool TryMoveWithStaticSkin(Vector2 currentPos, Vector2 desiredDelta, float skinStatic, LayerMask staticMask, float agentRadius, out Vector2 finalPos)
        {
            finalPos = currentPos;
            Vector2 dir = desiredDelta.normalized;
            float dist = desiredDelta.magnitude;
            if (dist <= 1e-5f) return true;

            // 使用 CircleCast 预测是否会撞静态障碍
            RaycastHit2D hit = Physics2D.CircleCast(currentPos, agentRadius, dir, dist, staticMask);
            if (hit.collider)
            {
                float move = Mathf.Max(0f, hit.distance - skinStatic);
                finalPos = currentPos + dir * move;
                return false; // 被阻挡（但已平滑停在皮肤前）
            }
            else
            {
                finalPos = currentPos + desiredDelta;
                return true; // 未阻挡
            }
        }
    }
}

