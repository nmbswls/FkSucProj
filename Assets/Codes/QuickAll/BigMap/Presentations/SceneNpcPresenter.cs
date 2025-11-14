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

        public bool CanInteractEnable()
        {
            if (NpcEntity.IsInBattle)
            {
                return false;
            }

            var diff = transform.position - MainGameManager.Instance.playerScenePresenter.transform.position;
            if (diff.magnitude > 2f)
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

            //if(NpcEntity.GetAttr(AttrIdConsts.UnitDizzy) > 0)
            //{
            //    return true;
            //}

            return true;
        }

        public void TriggerInteract(int selectionId)
        {
            if (NpcEntity.IsInBattle)
            {
                return;
            }

            if (selectionId == 1)
            {
                MainGameManager.Instance.playerScenePresenter.PlayerEntity.abilityController.TryUseAbility("deep_zhaqu", target: NpcEntity);
            }
            else if(selectionId == 2)
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

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position + new Vector3(0, 0.25f, 0);
        }


        /// <summary>
        /// 1 shendu
        /// 2 吸
        /// </summary>
        /// <returns></returns>
        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();


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

            return ret;
        }
    }
}

/// <summary>
/// 场景单位 基类
/// </summary>

