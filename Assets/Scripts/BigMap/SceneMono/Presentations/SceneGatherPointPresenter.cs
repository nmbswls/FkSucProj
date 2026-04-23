using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class SceneGatherPointPresenter : ScenePresentationBase<GatherPointLogicEntity>, ISceneInteractable
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;

        public Vector2 Pos => transform.position;
        public string ShowName => gameObject.name;

        public GatherPointLogicEntity GatherPointEntity { get { return (GatherPointLogicEntity)_logic; } }

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => true;
        public bool CanInteractEnable()
        {
            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public float GetHintOffsetInfos()
        {
            return -1;
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

        public bool TriggerInteract(int selectionId)
        {
            //GatherPointEntity.DoGather();

            
            MainGameManager.Instance.gameLogicManager.playerLogicEntity.abilityController.TryUseAbility("player_common_interact", overrideParams: new Dictionary<string, string>()
            {
                ["InteractTime"] = GatherPointEntity.cacheConfig.GatherTime.ToString(),
            }, phaseOverrideAnims: new Dictionary<string, string>()
            {
                ["Interacting"] = GatherPointEntity.cacheConfig.GatherAnim
            },
            onAbilityEnd: (complete) => 
            {
                if(complete)
                {
                    GatherPointEntity.DoGather();
                }
            });

            return true;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}

