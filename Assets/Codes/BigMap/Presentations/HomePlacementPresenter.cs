
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

        public List<ISceneInteractable> bindingSceneInteractables = new();

        public bool CanInteractEnable()
        {
            return false;
        }

        public void TriggerInteract(int selectionId)
        {
            throw new System.NotImplementedException();
        }

        public Vector3 GetHintAnchorPosition()
        {
            throw new System.NotImplementedException();
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            throw new System.NotImplementedException();
        }
    }
}