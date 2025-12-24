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
        public string ShowName => gameObject.name;
        public NpcUnitLogicEntity NpcEntity
        {
            get
            {
                return (NpcUnitLogicEntity)_logic;
            }
        }


        protected override void Awake()
        {
            base.Awake();
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


        }

        public bool CanInteractEnable(float dist)
        {

            if (NpcEntity.IsAttaching) return false;

            if (NpcEntity.IsHMode)
            {
                return true;
            }

            if (MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
            {
                if (dist > 0.5f) return false;
                if(NpcEntity.cacheCfg.InteractList.Count > 0)
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
                    if(NpcEntity.IsHMode)
                    {

                        return true;
                    }

                    return false;
                }

                if (dist > 2f)
                {
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
                    var selection = NpcEntity.cacheCfg.InteractList[selectionId]; 
                    foreach(var output in selection.Outputs)
                    {
                        switch(output.OutputType)
                        {
                            case Config.LogicInteractOutput.EOutputType.OpenPanel:
                                {
                                    if(output.Param1 == 1)
                                    {
                                        // 
                                    }
                                }
                                break;
                        }
                    }
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


        /// <summary>
        /// 1 shendu
        /// 2 吸
        /// </summary>
        /// <returns></returns>
        public List<SceneInteractSelection> GetInteractSelections(float dist)
        {
            var ret = new List<SceneInteractSelection>();
            if (NpcEntity.IsAttaching) return ret;
            //if (NpcEntity.CombatState != NpcCombatStateComp.ECombatState.NotCombat)
            {
                if (NpcEntity.IsHMode)
                {
                    if (MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.IsSkillReady("h_mode_execute"))
                    {
                        ret.Add(new SceneInteractSelection()
                        {
                            SelectId = 99,
                            SelectContent = "榨取",
                            Selectable = true
                        }); ;
                    }
                    else
                    {
                        ret.Add(new SceneInteractSelection()
                        {
                            SelectId = 99,
                            SelectContent = "榨取(cd)",
                            Selectable = false
                        }); ;
                    }
                }
            }


            if (MainGameManager.Instance.gameLogicManager.PlayerPeaceMode)
            {
                for(int i=0;i< NpcEntity.cacheCfg.InteractList.Count;i++)
                {
                    ret.Add(new SceneInteractSelection()
                    {
                        SelectId = i,
                        SelectContent = NpcEntity.cacheCfg.InteractList[i].Label,
                    });
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

