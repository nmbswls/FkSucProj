
using System.Collections.Generic;
using My.Config;
using My.Map.Entity;
using My.UI;
using UnityEngine;
using static My.GameLogicManager;
using static UnityEditor.Rendering.CameraUI;

namespace My.Map
{

    public interface IWithInteract
    {

        List<MapInteractInfo> InteractInfos { get; }

        bool TryTriggerInteract(int interactId);

        bool CheckTriggerInteract(int interactId);

        bool IsInteracting { get; }
    }


    public interface IEntityInteractable
    {
        string GetRuntimeVariable(string paramName);

        GameLogicManager LogicManager { get; }

        Vector2 Pos { get; }

        long Id { get; }

        bool CheckLocalSwitch(string switchName);

        void SetLocalSwitch(string switchName, bool isOn);

        void DoAnimation(string animName);
    }


    public class EntityInteractComp : IWithInteract
    {
        public IEntityInteractable Owner;

        private List<MapInteractInfo> interactInfos = new();
        public List<MapInteractInfo> InteractInfos { get { return interactInfos; } }

        private bool _isInteracting = false;
        public bool IsInteracting { get { return _isInteracting; } }

        private int _currOutputIdx = 0;
        private MapInteractInfo? _currInteract = null;
        private float _pendingParam1 = 0;

        public EntityInteractComp(IEntityInteractable owner)
        {
            this.Owner = owner;
        }

        public void RefreshInteractInfo(List<MapInteractInfo> interactInfos)
        {
            this.interactInfos.Clear();
            this.interactInfos.AddRange(interactInfos);

            if(_isInteracting)
            {
                Debug.LogError("RefreshInteractInfo when interacting.");
                DoInteractEnd();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void TickInteract(float dt)
        {
            if(_isInteracting)
            {
                if(_currOutputIdx >= _currInteract.Outputs.Count)
                {
                    DoInteractEnd();
                    return;
                }



                bool pendingFinish = false;
                switch (_currInteract.Outputs[_currOutputIdx].OutputType)
                {
                    case LogicInteractOutput.EOutputType.Wait:
                        {
                            _pendingParam1 += dt;

                            if (_pendingParam1 > _currInteract.Outputs[_currOutputIdx].Param1 * 0.001f)
                            {
                                pendingFinish = true;
                            }
                        }
                        break;
                    case LogicInteractOutput.EOutputType.SelfAnim:
                        {
                            _pendingParam1 += dt;

                            if (_pendingParam1 > _currInteract.Outputs[_currOutputIdx].Param1 * 0.001f)
                            {
                                pendingFinish = true;
                            }
                        }
                        break;
                    default:
                        {
                            pendingFinish = true;
                            break;
                        }
                }

                if (pendingFinish)
                {
                    _currOutputIdx += 1;
                    _pendingParam1 = 0;

                    HandleInteractOutputs();
                }
            }
            // 处理

            if(!_isInteracting)
            {

            }
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
                    case InteractCheckCond.ECheckType.PlayerNotRetreating:
                        {
                            if (Owner.LogicManager.playerLogicEntity.IsRetreating)
                            {
                                passed = false;
                            }
                        }
                        break;
                        
                }
            }


            return passed;
        }

        /// <summary>
        /// 获取交互作用目标
        /// 仅针对部分output有效果
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        private long GetInteractTarget(LogicInteractOutput output)
        {
            switch(output.TargetType)
            {
                case LogicInteractOutput.ETargetType.DynamicEntity:
                    {
                        var vName = output.DynamicEntityVariable;
                        var idStr = Owner.GetRuntimeVariable(vName);
                        long.TryParse(idStr, out var targetId);
                        return targetId;
                    }
                    break;
                case LogicInteractOutput.ETargetType.StaticName:
                    {
                        var staticName = output.StaticName;
                        var staticid = Owner.LogicManager.AreaManager.GetStaticIdByUniqName(staticName);

                        Owner.LogicManager.AreaManager.RefreshInfoRuntimes.TryGetValue(staticid, out var refreshRuntime);
                        if(refreshRuntime == null)
                        {
                            return 0;
                        }

                        return refreshRuntime.EntityInstId;
                    }
                    break;
                case LogicInteractOutput.ETargetType.GroupMember:
                    {
                        var ownerEG = Owner as EventGroupLogicEntity;
                        if (ownerEG == null)
                        {
                            Debug.LogError("EGMemberChangeState owner not event group ");
                            return 0;
                        }

                        int memberId = output.MemberId;

                        ownerEG.MemberId2EntityMap.TryGetValue(memberId, out var entityId);
                        return entityId;
                    }
                case LogicInteractOutput.ETargetType.Default:
                    {
                        return Owner.Id;
                    }
            }
            return 0;
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

            _isInteracting = true;
            _currOutputIdx = 0;
            _currInteract = interactItem;

            HandleInteractOutputs();

            return true;
        }

        private void HandleInteractOutputs()
        {
            if(_currInteract == null)
            {
                DoInteractEnd();
                return;
            }

            bool errOccur = false;

            while(_currInteract != null && _currOutputIdx < _currInteract.Outputs.Count)
            {
                bool pending = false;
                var output = _currInteract.Outputs[_currOutputIdx];
                switch (output.OutputType)
                {
                    case LogicInteractOutput.EOutputType.ChangeSelfStatus:
                        {

                            var targetEntityId = GetInteractTarget(output);
                            if(targetEntityId == 0)
                            {
                                Debug.Log("TryTriggerInteract ChangeSelfStatus not valid target");
                                errOccur = true;
                                break;
                            }

                            LogicEntityInteractPoint toChangeEntity = null;
                            if (targetEntityId == Owner.Id)
                            {
                                toChangeEntity = Owner as LogicEntityInteractPoint;
                            }
                            else
                            {
                                toChangeEntity = Owner.LogicManager.GetLogicEntity(targetEntityId, false) as LogicEntityInteractPoint;
                            }

                            if (toChangeEntity == null)
                            {
                                Debug.Log("TryTriggerInteract target not interact point");
                                errOccur = true;
                                break;
                            }
                            toChangeEntity.ChangeSelfStatus((int)output.Param1);
                        }
                        break;
                    case LogicInteractOutput.EOutputType.Teleport:
                        {
                            if (Owner.LogicManager.IsLocalRoomTeleportLocked)
                                break;

                            string mapName = output.Param3;
                            string namedP = output.Param4;

                            // 原地传送
                            if (string.IsNullOrEmpty(mapName) || mapName == Owner.LogicManager.AreaManager.MapName)
                            {
                                if (!string.IsNullOrEmpty(namedP))
                                {
                                    var p = Owner.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(namedP);
                                    if (p == null)
                                    {
                                        break;
                                    }
                                    Owner.LogicManager.RequestLocalRoomTeleport(p.Value.Position);
                                }
                            }
                            else
                            {
                                Owner.LogicManager.PreparePlayerSwitchArea(mapName, false, namedP);
                            }
                        }
                        break;
                    case LogicInteractOutput.EOutputType.ActivateEventGroup:
                        {
                            if (Owner is not EventGroupLogicEntity egPoint)
                            {
                                Debug.Log("TryTriggerInteract ActivateEventGroup valid egpoint");
                                errOccur = true;
                                break;
                            }

                            egPoint.ActivateSleepyMembers();
                        }
                        break;
                    case LogicInteractOutput.EOutputType.SpecialMoveTo:
                        {
                            var player = Owner.LogicManager.playerLogicEntity;

                            // 尝试获取覆盖的值
                            var pName = Owner.GetRuntimeVariable(output.Param3);
                            if (string.IsNullOrEmpty(pName))
                            {
                                pName = output.Param3;
                            }

                            var p = Owner.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(pName);
                            if (p == null)
                            {
                                Debug.Log("special move no point found");
                                errOccur = true;
                                break;
                            }

                            float delay = output.Param1 * 0.001f;
                            LogicFightEffectContext ctx = new LogicFightEffectContext(Owner.LogicManager, EFightCtxType.None, new EffectSourceInfo()
                            {
                                SrcType = ESourceType.Mechanism
                            });
                            ctx.TargetId = player.Id;
                            ctx.CastVec1 = p.Value.Position;


                            //Debug.Log("special move yo");
                            Owner.LogicManager.globalBuffManager.RequestAddBuff(player.Id, "lock_move", overrideDuration: delay);
                            Owner.LogicManager.viewer.DoPlayerSpecialMove(p.Value.Position, player.Pos, delay, () =>
                            {

                            });
                            Owner.LogicManager.HandleLogicFightEffect(new MapAbilityEffectTeleportToCfg() { PendingTime = delay }, ctx);

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
                            Owner.LogicManager.viewer.PlayDialog(dialogId, Owner.Id);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.StartRetreat:
                        {
                            Owner.LogicManager.playerLogicEntity.TryStartRetreating();
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.TriggerSpawner:
                        {
                            //Owner.GetRuntimeVariable();
                            int staticId = (int)output.Param1;

                            Owner.LogicManager.AreaManager.RefreshInfoRuntimes.TryGetValue(staticId, out var info);
                            if(info == null)
                            {
                                break;
                            }
                            var spanwer = Owner.LogicManager.GetLogicEntity(info.EntityInstId);
                            if(spanwer == null || spanwer is not DynamicSpawnerLogicEntity spawner)
                            {
                                break;
                            }
                            spawner.RefreshSpawner();
                        }
                        break;
                    case LogicInteractOutput.EOutputType.OpenPanel:
                        {
                            string panelId = output.Param3;
                            UIManager.Instance.ShowPanel(panelId);
                        }
                        break;

                    case LogicInteractOutput.EOutputType.NextDayPeriod:
                        {
                            Owner.LogicManager.GoToNextPeriod();
                        }
                        break;

                    #region group相关

                    case Config.LogicInteractOutput.EOutputType.EGMemberActivate:
                        {

                            var ownerEG = Owner as EventGroupLogicEntity;
                            if (ownerEG == null)
                            {
                                Debug.LogError("EGMemberChangeState owner not event group ");
                                break;
                            }

                            ownerEG.ActivateSleepyMembers();
                            //int memberId = (int)output.Param1;
                            //int status = (int)output.Param2;

                            //ownerEG.MemberId2EntityMap.TryGetValue(memberId, out var entityId);
                            //if (entityId == 0)
                            //{
                            //    Debug.Log("HandleOutput EGMemberChangeState UpdateInteractStatus fail e");
                            //    continue;
                            //}

                            //var unit = Owner.LogicManager.GetLogicEntity(entityId) as BaseUnitLogicEntity;
                            //if (unit == null)
                            //{
                            //    Debug.Log("HandleOutput UpdateInteractStatus no entity e");
                            //    continue;
                            //}

                            //unit.IsActive = true;
                        }
                        break;

                    #endregion


                    #region pending 持续性的

                    case LogicInteractOutput.EOutputType.Wait:
                        {
                            pending = true;
                            _pendingParam1 = 0;
                        }
                        break;
                    case LogicInteractOutput.EOutputType.SelfAnim:
                        {
                            pending = true;
                            _pendingParam1 = 0;

                            var animName = output.Param3;
                            Owner.DoAnimation(animName);
                        }
                        break;

                    #endregion


                }

                if (pending)
                {
                    break;
                }
                _currOutputIdx += 1;
            }

            if (_currInteract == null || errOccur || _currOutputIdx >= _currInteract.Outputs.Count)
            {
                DoInteractEnd();
            }
        }

        public void HandleOneDirectOutput()
        {

        }

        private void DoInteractEnd()
        {
            if (_currInteract == null) return;
            Debug.Log($"DoInteractEnd. {_currInteract?.InteractId}");

            _isInteracting = false;
            _currOutputIdx = 0;
            _currInteract = null;

            _pendingParam1 = 0;
        }
    }

}