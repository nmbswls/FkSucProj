
using My.Map.Entity;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map
{
    public partial class NpcUnitLogicEntity
    {



        public float speedThreshold = 0.05f; // 速度太小不更新面向
        public float deadzoneAngle = 3f;     // 小角度不转动（度）
        public float maxAngularSpeed = 360f; // 每秒最大旋转角度（度/s）
        public float smoothTime = 0.05f;     // 平滑时间（秒）

        

        public override void InitFacing()
        {
            base.InitFacing();
        }

        /// <summary>
        /// 更新朝向
        /// </summary>
        protected override void UpdateFaceDir()
        {
            if (attributeStore.CheckHasState(AttrIdConsts.LockFace))
            {
                return;
            }

            // 外部控制 不处理
            if (ControlledFacing)
            {
                return;
            }

            UpdateFacing();

            
        }

        public void UpdateFacing()
        {
            // 检查是否需要锁定目标朝向
            Vector2? lootTarget = null;
           
            if (combatStateComp != null && combatStateComp.CombatState == NpcCombatStateComp.ECombatState.InCombat && combatStateComp.PrimaryTargetId != 0)
            {
                var targt = LogicManager.GetLogicEntity(combatStateComp.PrimaryTargetId, false);
                if (targt != null)
                {
                    lootTarget = targt.Pos;
                }
            }
            else if(attractInfo != null)
            {
                if(attractInfo.AttractSource != null)
                {
                    lootTarget = attractInfo.AttractSource.Pos;
                }
                else
                {
                    lootTarget = attractInfo.Pos;
                }
            }
            //else if(IsWatchingPlayer)
            //{
            //    lootTarget = LogicManager.playerLogicEntity.Pos;
            //}

            
            Vector2 lookDir = Vector2.zero;
            if (lootTarget != null)
            {
                lookDir = (Vector2)(lootTarget - this.Pos);
            }
            else if (entityMotorComp.DesiredVelocity.magnitude > 1e-2)
            {
                lookDir = entityMotorComp.DesiredVelocity;
            }


            if (lookDir == Vector2.zero || lookDir.sqrMagnitude < 1e-8f)
            {
                return;
            }

            lookDir.Normalize();
            // FaceDir = lookDir;

            _targetAngle = AngleFromDir(lookDir);

            
        }




        

        static Vector2 NormalizeSafe(Vector2 v)
        {
            float m = v.magnitude;
            return m > 1e-8f ? v / m : Vector2.zero;
        }

        static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float delta = Mathf.DeltaAngle(current, target);
            delta = Mathf.Clamp(delta, -maxDelta, maxDelta);
            return current + delta;
        }

        static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime)
        {
            return Mathf.SmoothDampAngle(current, target, ref currentVelocity, smoothTime);
        }
    }

}
