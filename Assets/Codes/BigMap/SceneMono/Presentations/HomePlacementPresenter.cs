
using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My
{
    /// <summary>
    /// ½¨ÖþÎï
    /// </summary>
    public class HomePlacementPresenter : ScenePresentationBase<HomePlacementLogicEntity>, ISceneInteractable
    {

        public HomePlacementLogicEntity PlacementEntity { get { return (HomePlacementLogicEntity)_logic; } }

        public string ShowName => throw new System.NotImplementedException();

        public Vector2 Pos => _logic.Pos;

        public List<ISceneInteractable> bindingSceneInteractables = new();

        public bool CanInteractEnable()
        {
            return false;
        }

        public void TriggerInteract(int selectionId)
        {
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            return new();
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }
    }
}