using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Scene
{

    public interface ISubInteractHolder
    {
        bool CanSubInteractEnable(int subIdx);

        List<SceneInteractSelection> GetSubInteractSelections(int subIdx);

        bool SubTriggerInteract(int subIdx, int selectionId, int playerId);
    }


    public class SubInteractHandle : MonoBehaviour, ISceneInteractable
    {
        public int HandleIdx = 0;
        public Transform HintPivot;

        public ISubInteractHolder Owner;

        public string ShowName => "查看";

        public Vector2 Pos => transform.position;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => true;

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

        public bool TriggerInteract(int selectionId, int playerId)
        {
            return Owner.SubTriggerInteract(HandleIdx, selectionId, playerId);
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}

