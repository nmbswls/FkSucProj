using Config.Map;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace My.Map.Scene
{
    public class InteractPointPresenter : ScenePresentationBase<InteractPointLogic>, ISceneInteractable
    {
        [SerializeField] private SpriteRenderer icon;
        [SerializeField] private GameObject highlightFx;

        public event Action<bool> EventOnInteractStateChanged;

        public string ShowName => gameObject.name;

        public InteractPointLogic RealLogic { get { return (InteractPointLogic)_logic; } }

        public Vector3 GetHintAnchorPosition()
        {
            return GetWorldPosition();
        }


        public void TriggerInteract(int selectionId)
        {
            RealLogic.DoTriggerInteract(selectionId);
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            string intName = "int";
            {
                if (RealLogic.CurrStatusId == 0)
                {
                    var stateConf = RealLogic.cacheCfg.MainStatusInfo;
                    intName = stateConf.InteractName;
                }
            }
            ret.Add(new SceneInteractSelection() { 
                SelectId = 1,
                SelectContent = intName,
                Selectable = true,
            });
            return ret;
        }

        public bool CanInteractEnable()
        {
            return true;
        }
    }
}

