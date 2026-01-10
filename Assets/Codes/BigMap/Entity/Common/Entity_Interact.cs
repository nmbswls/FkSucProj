
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


    public interface IEntityInteractable
    {
        string GetRuntimeVariable(string paramName);

        GameLogicManager LogicManager { get; }

        Vector2 Pos { get; }

        long Id { get; }

        bool CheckLocalSwitch(string switchName);

        void SetLocalSwitch(string switchName, bool isOn);
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
            foreach(var oneCond in interactItem.CheckCommonCond)
            {
                if (!Owner.LogicManager.CheckCommonCond(oneCond))
                {
                    passed = false;
                    break;
                }
            }

            if(!passed)return false;

            foreach (var oneCond in interactItem.CheckInteractCond)
            {
                switch (oneCond.CheckType)
                {
                    case InteractCheckCond.ECheckType.NotHide:
                        {
                            if (Owner.LogicManager.playerLogicEntity.IsInStealth())
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.HasLocalSwitch:
                        {
                            string switchName = oneCond.Param3;
                            if(!Owner.CheckLocalSwitch(switchName))
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.NoLocalSwitch:
                        {
                            string switchName = oneCond.Param3;
                            if (Owner.CheckLocalSwitch(switchName))
                            {
                                passed = false;
                            }
                        }
                        break;
                }
            }


            return passed;
        }

        public bool TryTriggerInteract(int interactId)
        {
            if(!CheckTriggerInteract(interactId))
            {
                //return false;
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
                            if(Owner is not LogicEntityInteractPoint intPoint)
                            {
                                Debug.Log("TryTriggerInteract not valid change self status");
                                return false;
                            }
                            intPoint.ChangeSelfStatus((int)output.Param1);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.Teleport:
                        {
                            string areaName = output.Param3;
                            string namedP = output.Param4;

                            // 原地传送
                            if(areaName == Owner.LogicManager.AreaManager.AreaId)
                            {
                                if(string.IsNullOrEmpty(namedP))
                                {
                                    var p = Owner.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(namedP);
                                    if(p == null)
                                    {
                                        break;
                                    }
                                    Owner.LogicManager.playerLogicEntity.TeleportTo(p.Value.Position);
                                }
                            }
                            else
                            {
                                Owner.LogicManager.PlayerSwitchArea(output.Param3, false, namedP);
                            }
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

                    case LogicInteractOutput.EOutputType.StartStealth:
                        {
                            var player = Owner.LogicManager.playerLogicEntity;
                            player.StartStealth(Owner.Id, Owner.Pos);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.CostItems:
                        {
                            string itemId = output.Param3;
                            long count = output.Param1;
                            Owner.LogicManager.playerDataManager.CostItem(itemId, count);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.GiveItems:
                        {
                            string itemId = output.Param3;
                            long count = output.Param1;
                            Owner.LogicManager.playerDataManager.TryGiveItem(itemId, count, 0);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.SetLocalSwitch:
                        {
                            string switchName = output.Param3;
                            Owner.SetLocalSwitch(switchName, true);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.UnsetLocalSwitch:
                        {
                            string switchName = output.Param3;
                            Owner.SetLocalSwitch(switchName, false);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.OpenDialog:
                        {
                            string dialogId = output.Param3;
                            Owner.LogicManager.viewer.PlayDialog(dialogId);
                        }
                        break;

                }
            }
            return true;
        }
    }

}