using Map.Entity;
using Map.Logic.Events;
using My.Map.Entity;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.RuleTile.TilingRuleOutput;


namespace My.Map.Scene
{
    public class SceneNpcPresenter : SceneUnitPresenter, ISceneInteractable
    {
        public Vector2 Pos => transform.position;
        public string ShowName {
            get
            {
                return NpcEntity.cacheCfg.ShowName;
            } 
        }


        public NpcUnitLogicEntity NpcEntity
        {
            get
            {
                return (NpcUnitLogicEntity)_logic;
            }
        }

        public List<AnimationClip> SpecialAnimClips;
        private Dictionary<string, AnimationClip> _animCacheDict = new();

        protected override void Awake()
        {
            base.Awake();

            if (SpecialAnimClips != null)
            {
                foreach(var clip in SpecialAnimClips)
                {
                    _animCacheDict[clip.name] = clip;
                }
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            UnitEntity.EventOnAnimLayerUpdate += OnEventAnimLayerUpdate;

            //UnitEntity.onNewKnockBackIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.knockDir.normalized * intent.knockDuration;
            //};



            //UnitEntity.onNewDashIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.dashDir.normalized * intent.dashSpeed;
            //};

            //UnitEntity.onNewKnockBackIntent += (intent) =>
            //{
            //    UnitEntity.externalVel = intent.knockDir.normalized * intent.knockDuration;
            //};
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            UnitEntity.EventOnAnimLayerUpdate -= OnEventAnimLayerUpdate;
        }


        public void OnEventAnimLayerUpdate()
        {
            if (UnitEntity.AnimLayers.Count > 0)
            {
                var firstAnim = UnitEntity.AnimLayers[0];

                if (string.IsNullOrEmpty(firstAnim.Name) && _Animancer != null)
                {
                    _animCacheDict.TryGetValue(firstAnim.Name, out var clip);
                    if(clip != null)
                    {
                        _Animancer.Play(clip);
                    }
                }
            }
        }

        public bool CanInteractEnable()
        {

            if (NpcEntity.IsAttaching) return false;

            

            if (MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
            {
                if (NpcEntity.InteractComp.IsInteracting)
                {
                    return false;
                }

                var logicInts = NpcEntity.InteractComp.InteractInfos;
                int enableOne = 0;
                foreach (var i in logicInts)
                {
                    if(i.Passive)
                    {
                        continue;
                    }
                    bool canInt = NpcEntity.InteractComp.CheckTriggerInteract(i.InteractId);
                    if (canInt || !i.HideWhenFail)
                    {
                        enableOne += 1;
                    }
                }

                if (enableOne > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {

                if (NpcEntity.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
                {
                    if(NpcEntity.IsInHMode())
                    {
                        return true;
                    }

                    return false;
                }

                if (UnitEntity.CheckHasBuff("unsensored"))
                {
                    if (UnitEntity.GetAttr(AttrIdConsts.DeepZhaChance) == 0)
                    {
                        return false;
                    }
                }
                else
                {
                    if (MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, MainGameManager.Instance.playerScenePresenter.transform.position, NpcEntity.FaceDir, 1.0f, 60f))
                    {
                        return false;
                    }
                }

                return true;
            }

            //if(NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) > 0)
            //{
            //    return true;
            //}

            return false;
        }

        public void TriggerInteract(int selectionId)
        {

            if(selectionId == 99)
            {
                MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.UseSkill("h_mode_execute", target:NpcEntity);
                return;
            }

            if (MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
            {
                if (selectionId < NpcEntity.cacheCfg.InteractList.Count)
                {

                    NpcEntity.InteractComp.TryTriggerInteract(selectionId);
                }
            }
            else
            {
                if (selectionId == 1)
                {
                    MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("deep_zhaqu", target: NpcEntity);
                }
                else if (selectionId == 2)
                {
                    if (MainGameManager.Instance.VisionSenser2D.CanSee(transform.position, MainGameManager.Instance.playerScenePresenter.transform.position, NpcEntity.FaceDir, 1.0f, 60f))
                    {
                        return;
                    }

                    if (NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) == 0)
                    {
                        //return;
                    }

                    // 显示层事件
                    MainGameManager.Instance.gameLogicManager.LogicEventBus.Publish(new MLECommonGameEvent()
                    {
                        Name = "AbsorbDizzy",
                        Param3 = this.Id,
                    });

                    MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("zhaqu", target: NpcEntity);
                }
            }

        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position + new Vector3(0, 0.25f, 0);
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }

        /// <summary>
        /// 1 shendu
        /// 2 吸
        /// </summary>
        /// <returns></returns>
        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();
            if (NpcEntity.IsAttaching) return ret;
            //if (NpcEntity.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
            {
                //if (NpcEntity.IsHMode)
                {
                    
                }
            }


            if (MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
            {
                var logicInts = NpcEntity.InteractComp.InteractInfos;
                foreach (var i in logicInts)
                {

                    if (i.Passive)
                    {
                        continue;
                    }

                    bool canInt = NpcEntity.InteractComp.CheckTriggerInteract(i.InteractId);
                    if (canInt || !i.HideWhenFail)
                    {
                        ret.Add(new SceneInteractSelection()
                        {
                            SelectId = i.InteractId,
                            SelectContent = i.Label,
                        });
                    }
                }
            }
            else
            {
                if (UnitEntity.CheckHasBuff("unsensored"))
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = 1,
                        SelectContent = "Int",

                    });
                }
                else
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = 2,
                        SelectContent = "Int",

                    });
                }
            }

            return ret;
        }

        
    }
}

/// <summary>
/// 场景单位 基类
/// </summary>

