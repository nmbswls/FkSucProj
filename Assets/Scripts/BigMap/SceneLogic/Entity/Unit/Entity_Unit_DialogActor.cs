

using System.Collections.Generic;
using My.Dialog;
using My.Map.Entity.AI;
using My.Map.Unit;
using UnityEngine;

namespace My.Map
{

    public abstract partial class BaseUnitLogicEntity:
        IDialogueActor
    {
        public bool DialogControlled { get; set; }

        public bool DialogMoveFinished { get; set; }

        public bool IsDialogMoving;
        public Vector2 DialogMoveTarget;
        
        public virtual void CheckDialogActorBehaviour()
        {
            if(IsDialogMoving && !DialogMoveFinished)
            {
                var dir = DialogMoveTarget - Pos;
                if (dir.magnitude < 0.1f)
                {
                    DialogMoveFinished = true;
                    IsDialogMoving = false;
                }
            }
        }


        public Vector2 GetDialogMoveVel()
        {
            var dir = DialogMoveTarget - Pos;
            if(dir.magnitude < 0.1f)
            {
                return Vector2.zero;
            }

            return dir.normalized;
        }


        public void OnDialogStart()
        {
            DialogControlled = true;

            Debug.Log("OnDialogStart " + this.Id);

            // 接管anim
            //AnimLayers.Add(new AnimLayerStruct() { Name = "idle", Layer = 6, Priority = 5});
            //RemoveAnimLayer();
        }

        public void OnDialogEnd()
        {
            // 重置anim
            DialogControlled = false;

            //AnimLayers
        }

        public void DoDialogMove(Vector2 targetPos, float speed, Vector2? forcedStartPos)
        {
            IsDialogMoving = true;
            DialogMoveTarget = targetPos;
            DialogMoveFinished = false;

            if(forcedStartPos != null)
            {
                TeleportTo(forcedStartPos.Value);
            }
        }


        public void DoDialogAnimation(string animName)
        {
            PlayerAnim(animName, 3);
        }
    }


}