using Config.Map;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public class InteractPointPresenter : ScenePresentationBase<LogicEntityInteractPoint>, ISceneInteractable
    {
        [SerializeField] private GameObject highlightFx;
        public List<GameObject> Blocks;

        public event Action<bool> EventOnInteractStateChanged;

        public virtual string ShowName { 
            get 
            {
                return RealLogic.cacheCfg.ShowName;
            }
        }

        public LogicEntityInteractPoint RealLogic { get { return (LogicEntityInteractPoint)_logic; } }

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
            int enableOne = 0;
            var logicInts = RealLogic.InteractInfos;

            foreach (var i in logicInts)
            {
                bool canInt = RealLogic.CheckTriggerInteract(i.InteractId);
                if(canInt || !i.HideWhenFail)
                {
                    enableOne += 1;
                }
            }

            return enableOne > 0;
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

        public override void Tick(float dt)
        {
            base.Tick(dt);

        }

        protected override void RefreshFadeState()
        {
            base.RefreshFadeState();

            if(_mainSpriteArr != null)
            {
                foreach(var s in _mainSpriteArr)
                {
                    s.color = new Color(s.color.r, s.color.g, s.color.b, _currFadeAlpha);
                }
            }
        }
    }
}

