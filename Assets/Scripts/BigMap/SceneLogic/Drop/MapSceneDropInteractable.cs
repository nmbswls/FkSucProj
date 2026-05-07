using System;
using System.Collections;
using System.Collections.Generic;
using My.Config;
using My.Map.Drop;
using UnityEngine;


namespace My.Map.Scene
{
    public class MapSceneDropInteractable : MonoBehaviour, ISceneInteractable
    {
        public string ShowName => cacheItemName;
        private string cacheItemName;

        public Vector2? SrcPos;
        public bool IsFlying;

        public long Id { get { return DropData?.Id ?? 0; } }


        public DropData DropData { get; protected set; }
        public bool AutoPick { get; set; }
        public bool Picking { get; set; }

        public Vector2 Pos => transform.position;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => false;

        public FlyToPlayerMover flyToPlayerMover;

        private void Awake()
        {
            flyToPlayerMover = GetComponent<FlyToPlayerMover>();
        }

        public void InitFromDrop(DropData dropData,  Vector3? srcPos/*, System.Action<int, GameObject> onPicked*/, bool autoPick)
        {
            this.DropData = dropData;
            this.SrcPos = srcPos;
            this.AutoPick = autoPick;

            if (srcPos != null)
            {
                IsFlying = true;
                transform.position = srcPos.Value;
            }
            else
            {
                IsFlying = false;
                transform.position = dropData.Position;
            }

            var itemCfg = ItemCatalog.GetItemDef(dropData.ItemId);
            cacheItemName = itemCfg?.DisplayName ?? "?";

            flyToPlayerMover.Clear();
            Picking = false;
        }

        public void Update()
        {
            if(!Picking && IsFlying && SrcPos != null)
            {
                transform.position = Vector2.Lerp(transform.position, DropData.Position, 6f * Time.deltaTime);
                Vector2 pos2 = transform.position;

                if ((DropData.Position - pos2).magnitude < 0.01f)
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

        public bool TriggerInteract(int selectionId)
        {
            Debug.Log("TriggerInteract: pick drop");
            MainGameManager.Instance.gameLogicManager.globalDropCollection.PickDrop(DropData.Id);
            return true;
        }

        public bool CanInteractEnable()
        {
            if (IsFlying)
            {
                return false;
            }
            if(AutoPick)
            {
                return false;
            }
            return true;
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }

        public void DoRecycle()
        {
            this.flyToPlayerMover.Clear();
            this.DropData = null;
            this.Picking = false;
            this.AutoPick = false;

            gameObject.SetActive(false);
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}



