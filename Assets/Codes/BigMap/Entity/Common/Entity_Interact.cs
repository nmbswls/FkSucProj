
using System.Collections.Generic;
using My.Config;
using My.Map.Entity;
using UnityEngine;
using static My.GameLogicManager;

namespace My.Map
{

    public interface IWithInteract
    {

        List<MapInteractInfo> InteractInfos { get; }

        bool TryTriggerInteract(int interactId);

        bool CheckTriggerInteract(int interactId);
    }


    public class EntityInteractComp : IWithInteract
    {
        public IEntityInteractable Owner;

        private List<MapInteractInfo> interactInfos = new();
        public List<MapInteractInfo> InteractInfos { get { return interactInfos; } }

        public EntityInteractComp(IEntityInteractable owner)
        {
            this.Owner = owner;
        }

        public void RegisterInteractInfo(List<MapInteractInfo> interactInfos)
        {
            this.interactInfos.Clear();
            this.interactInfos.AddRange(interactInfos);

        }


        public bool CheckTriggerInteract(int interactId)
        {
            var interactItem = interactInfos.Find((item) => item.InteractId == interactId);
            if (interactItem == null)
            {
                return false;
            }

            var passed = true;
            foreach(var oneCond in interactItem.CheckCond)
            {
                if (!Owner.LogicManager.CheckCommonCond(oneCond))
                {
                    passed = false;
                    break;
                }
            }


            return passed;
        }

        public bool TryTriggerInteract(int interactId)
        {
            if(!CheckTriggerInteract(interactId))
            {
                return false;
            }

            var interactItem = interactInfos.Find((item) => item.InteractId == interactId);
            if (interactItem == null)
            {
                return false;
            }


            foreach (var output in interactItem.Outputs)
            {
                switch (output.OutputType)
                {
                    case Config.LogicInteractOutput.EOutputType.ChangeSelfStatus:
                        {
                            if(Owner is not InteractPointLogic intPoint)
                            {
                                Debug.Log("TryTriggerInteract not valid change self status");
                                return false;
                            }
                            intPoint.ChangeSelfStatus((int)output.Param1);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.Teleport:
                        {
                            Owner.LogicManager.PlayerSwitchArea(output.Param3);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.ActivateEventGroup:
                        {
                            if( Owner is not EventGroupLogicEntity egPoint)
                            {
                                Debug.Log("TryTriggerInteract ActivateEventGroup valid egpoint");
                                return false;
                            }

                            egPoint.ActivateSleepyMembers();
                        }
                        break;
                    case LogicInteractOutput.EOutputType.SpecialMoveTo:
                        {
                            var player = Owner.LogicManager.playerLogicEntity;
                            
                            // 尝试获取覆盖的值
                            var pName = Owner.GetRuntimeVariable(output.Param3);
                            if(string.IsNullOrEmpty(pName))
                            {
                                pName = output.Param3;
                            }

                            var p = Owner.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(pName);
                            if(p == null)
                            {
                                Debug.Log("special move no point found");
                                return false;
                            } 

                            float delay = output.Param1 * 0.001f;
                            LogicFightEffectContext ctx = new LogicFightEffectContext(Owner.LogicManager, new EffectSourceInfo()
                            {
                                SrcType = ESourceType.Mechanism
                            });
                            ctx.TargetId = player.Id;
                            ctx.CastVec1 = p.Value.Position;


                            //Debug.Log("special move yo");
                            Owner.LogicManager.globalBuffManager.AddBuff(player.Id, "lock_move", overrideDuration: delay);
                            Owner.LogicManager.viewer.DoPlayerSpecialMove(p.Value.Position, player.Pos, delay, () =>
                            {

                            });
                            Owner.LogicManager.HandleLogicFightEffect(new MapAbilityEffectTeleportToCfg() { PendingTime = delay}, ctx);

                        }
                        break;
                }
            }
            return true;
        }
    }

}