using Config.Map;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Config.Map.MapInteractPointConfig;

namespace My.Map.Scene
{
    public class InteractPointPresenter : ScenePresentationBase<LogicEntityInteractPoint>, ISceneInteractable
    {
        [SerializeField] private GameObject highlightFx;

        public GameObject MainBlock;
        public List<GameObject> Blocks;

        public Transform[] StatusViews;

        public Transform InteractPivot;

        public Vector2 Pos => transform.position;

        public event Action<bool> EventOnInteractStateChanged;

        private bool IsSwitching = false;  // ≤ª∫œ¿Ì
        private float switchingTimer = 0;
        public virtual string ShowName { 
            get 
            {
                return RealLogic.cacheCfg.ShowName;
            }
        }

        public Collider2D AutoTriggerArea;

        public LogicEntityInteractPoint RealLogic { get { return (LogicEntityInteractPoint)_logic; } }

        public Vector3 GetHintAnchorPosition()
        {
            if(InteractPivot != null)
            {
                return InteractPivot.transform.position;
            }
            return GetWorldPosition();
        }

        public float GetHintOffsetInfos()
        {
            return RealLogic.cacheCfg.NameOffset;
        }


        public bool TriggerInteract(int selectionId)
        {
            return RealLogic.TryTriggerInteract(selectionId);
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            if (IsSwitching) return ret;

            if(RealLogic.IsInteracting)
            {
                return ret;
            }

            var logicInts = RealLogic.InteractInfos;

            foreach (var i in logicInts)
            {
                if (i.Passive)
                {
                    continue;
                }
                bool canInt = RealLogic.CheckTriggerInteract(i.InteractId);
                
                if(canInt)
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = i.InteractId,
                        SelectContent = canInt ? i.Label : i.UnLabel,
                        Selectable = canInt,
                    });
                }
            }
            
            return ret;
        }

        public bool CanInteractEnable()
        {
            if (IsSwitching) return false;

            if (RealLogic.IsInteracting)
            {
                return false;
            }

            int enableOne = 0;
            var logicInts = RealLogic.InteractInfos;

            foreach (var i in logicInts)
            {

                if (i.Passive)
                {
                    continue;
                }

                bool canInt = RealLogic.CheckTriggerInteract(i.InteractId);
                if(canInt)
                {
                    enableOne += 1;
                }
            }

            return enableOne > 0;
        }

        public bool IsAutoInteract()
        {
            var statInfo = RealLogic.GetCurrentStatusInfo();
            if(statInfo == null)
            {
                return false;
            }
            return statInfo.AutoTrigger;
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);

            RealLogic.EventOnStatusChange += OnStatusChanged;

            RealLogic.EventOnAnimLayerUpdate += OnEventAnimLayerUpdate;
        }

        public override void Unbind()
        {
            if(RealLogic != null)
            {
                RealLogic.EventOnStatusChange -= OnStatusChanged;
                RealLogic.EventOnAnimLayerUpdate -= OnEventAnimLayerUpdate;
            }

            IsSwitching = false;
            base.Unbind();
        }

        public void OnEventAnimLayerUpdate()
        {
            
        }

        public void OnStatusChanged(StateChangeView changeView)
        {
            //MainGameManager.Instance.interactSystem.UpdateInteractRangeObjs
            var status = RealLogic.GetCurrentStatusInfo();
            if(MainBlock != null)
            {
                if (status.HasBlock)
                {
                    MainBlock?.SetActive(true);
                }
                else
                {
                    MainBlock?.SetActive(false);
                }
            }
            

            if(changeView != null && changeView.ChangingDuration > 0)
            {
                IsSwitching = true;
                switchingTimer = changeView.ChangingDuration;
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            if(IsSwitching)
            {
                switchingTimer -= dt;

                if(switchingTimer <= 0)
                {
                    IsSwitching = false;
                }

                MainGameManager.Instance.ShowFakeFxEffect("switching", transform.position);
            }
        }

        protected override void OnFadeStateUpdate()
        {
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

