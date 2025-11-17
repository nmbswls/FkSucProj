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

        public string ShowName => gameObject.name;

        public event Action<bool> EventOnInteractStateChanged;

        public Vector2? SrcPos;
        public Vector2 DropPos;
        public bool IsFlying;

        public void InitFromDrop(long dropId, Vector2 dropPos,  Vector3? srcPos/*, System.Action<int, GameObject> onPicked*/)
        {
            this.DropId = dropId;
            this.DropPos = dropPos;
            this.SrcPos = srcPos;

            if (srcPos != null)
            {
                IsFlying = true;
                transform.position = srcPos.Value;
            }
            else
            {
                IsFlying = false;
            }

            //_particleIndex = particleIndex;
            //_onPicked = onPicked;
        }

        public void Update()
        {
            if(IsFlying && SrcPos != null)
            {
                transform.position = Vector2.Lerp(transform.position, DropPos, 0.5f * Time.deltaTime);
                Vector2 pos2 = transform.position;

                if ((DropPos - pos2).magnitude < 0.01f)
                {
                    IsFlying = false;
                }
            }
        }


        public Vector3 GetHintAnchorPosition()
        {
            return new Vector2(transform.position.x, transform.position.y) + new Vector2(0, 0f);
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "pick",

            });
            return ret;
        }

        public void SetInteractExpandStatus(bool expanded)
        {
        }

        public void TriggerInteract(int selectionId)
        {
            Debug.Log("手动拾取触发");
            MainGameManager.Instance.gameLogicManager.globalDropCollection.RemoveDrop(DropId, false);
        }

        public bool CanInteractEnable()
        {
            if (IsFlying)
            {
                return false;
            }
            return true;
        }
    }
}



