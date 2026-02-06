using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Scene
{

    public interface ISubInteractHolder
    {
        bool CanSubInteractEnable(int subIdx);

        List<SceneInteractSelection> GetSubInteractSelections(int subIdx);

        bool SubTriggerInteract(int subIdx, int selectionId);
    }


    public class SubInteractHandle : MonoBehaviour, ISceneInteractable
    {
        public int HandleIdx = 0;
        public Transform HintPivot;

        public ISubInteractHolder Owner;

        public string ShowName => "²é¿´";

        public Vector2 Pos => throw new System.NotImplementedException();

        public bool CanInteractEnable()
        {
            if (Owner == null) return false;
            return Owner.CanSubInteractEnable(HandleIdx);
        }

        public Vector3 GetHintAnchorPosition()
        {
            if(HintPivot == null)
            {
                return transform.position;
            }
            return HintPivot.position;
        }

        public float GetHintOffsetInfos()
        {
            return 0;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            return Owner.GetSubInteractSelections(HandleIdx);
        }

        public bool TriggerInteract(int selectionId)
        {
            return Owner.SubTriggerInteract(HandleIdx, selectionId);
        }
    }
}

