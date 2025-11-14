using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneGatherPointPresenter : ScenePresentationBase<GatherPointLogicEntity>, ISceneInteractable
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;


        public string ShowName => gameObject.name;

        public GatherPointLogicEntity GatherPointEntity { get { return (GatherPointLogicEntity)_logic; } }

        public bool CanInteractEnable()
        {
            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();

            bool selectable;
            if(GatherPointEntity.LeftCount <= 0)
            {
                selectable = false;
            }
            else
            {
                selectable = true;
            }

            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "Gather",
                Selectable = selectable
            });
            return ret;
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public void TriggerInteract(int selectionId)
        {
            GatherPointEntity.DoGather();
        }
    }
}

