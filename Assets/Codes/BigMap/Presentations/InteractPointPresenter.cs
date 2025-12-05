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
        public List<GameObject> Blocks;

        public event Action<bool> EventOnInteractStateChanged;

        public virtual string ShowName => gameObject.name;

        public InteractPointLogic RealLogic { get { return (InteractPointLogic)_logic; } }

        public Vector3 GetHintAnchorPosition()
        {
            return GetWorldPosition();
        }


        public void TriggerInteract(int selectionId)
        {
            RealLogic.TryTriggerInteract(selectionId);
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();

            var logicInts = RealLogic.InteractInfos;

            foreach (var i in logicInts)
            {
                bool canInt = RealLogic.CheckTriggerInteract(i.InteractId);
                
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = i.InteractId,
                    SelectContent = canInt ? i.Label : i.UnLabel,
                    Selectable = canInt,
                });
            }
            
            return ret;
        }

        public bool CanInteractEnable()
        {
            return RealLogic.InteractInfos.Count > 0;
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);

            RealLogic.OnStatusChange += OnStatusChanged;
        }

        public override void Unbind()
        {
            if(RealLogic != null)
            {
                RealLogic.OnStatusChange += OnStatusChanged;
            }

            base.Unbind();
        }

        public void OnStatusChanged()
        {
            //MainGameManager.Instance.interactSystem.UpdateInteractRangeObjs
        }
    }
}

