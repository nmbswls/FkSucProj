using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace My.Map.Scene
{
    public class MapSceneDropInteractable : MonoBehaviour, ISceneInteractable
    {
        public long Id { get; set; }
        public long DropId { get; set; }

        public event Action<bool> EventOnInteractStateChanged;

        public void InitFromDrop(long dropId/*, System.Action<int, GameObject> onPicked*/)
        {
            this.DropId = dropId;
            //_particleIndex = particleIndex;
            //_onPicked = onPicked;
        }


        public Vector3 GetHintAnchorPosition()
        {
            return new Vector2(transform.position.x, transform.position.y) + new Vector2(0, 0f);
        }

        public List<string> GetInteractSelections()
        {
            return new List<string>() { "pick" };
        }

        public void SetInteractExpandStatus(bool expanded)
        {
        }

        public void TriggerInteract(string interactSelection)
        {
            Debug.Log("手动拾取触发");
            MainGameManager.Instance.gameLogicManager.globalDropCollection.RemoveDrop(DropId);
        }

        public bool CanInteractEnable()
        {
            return true;
        }
    }
}



