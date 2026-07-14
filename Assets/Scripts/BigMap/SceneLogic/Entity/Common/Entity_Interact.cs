
using System.Collections.Generic;
using My.Config;
using My.Map.Entity;
using My.Map.Scene;
using My.Player;
using My.Quest;
using My.UI;
using UnityEngine;
using static My.GameLogicManager;

namespace My.Map
{

    public interface IWithInteract
    {

        List<MapInteractInfo> InteractInfos { get; }

        bool TryTriggerInteract(int interactId, int playerId);

        bool CheckTriggerInteract(int interactId, int playerId);

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

        int GetLocalIntValue(string key);

        void SetLocalIntValue(string key, int value);

        //void DoAnimation(string animName);
    }

    public interface IWithTimedRefreshState
    {
        bool TryGetLastRefreshSettlementDay(out int settlementDayIndex);
        void RecordRefreshSettlementDay(int settlementDayIndex);
    }


    public class EntityInteractComp : IWithInteract
    {
        private enum InteractPendingMode
        {
            None,
            Timer,
            ExternalNotify
        }

        private const float ExternalNotifyFallbackMarginSec = 0.5f;

        public IEntityInteractable Owner;
        LogicEntityBase OwnerEntity => Owner as LogicEntityBase;

        private List<MapInteractInfo> interactInfos = new();
        public List<MapInteractInfo> InteractInfos { get { return interactInfos; } }

        private bool _isInteracting = false;
        public bool IsInteracting { get { return _isInteracting; } }

        private int _currOutputIdx = 0;
        private MapInteractInfo? _currInteract = null;
        private int _interactingPlayerId = GamePlayerIds.Local;

        private InteractPendingMode _pendingMode = InteractPendingMode.None;
        private float _pendingElapsed;
        private float _pendingTimeoutSec;
        private bool _pendingNotified;
        private bool _interactFailed;

        public EntityInteractComp(IEntityInteractable owner)
        {
            this.Owner = owner;
        }

        public void RefreshInteractInfo(List<MapInteractInfo> interactInfos)
        {
            this.interactInfos.Clear();
            this.interactInfos.AddRange(interactInfos);

            // 交互 Output 链内的 ChangeSelfStatus 会同步刷新选项；勿在此 DoInteractEnd，由 HandleInteractOutputs 正常收尾。
            if (_isInteracting)
            {
                return;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dt"></param>
        public void TickInteract(float dt)
        {
            if (!_isInteracting)
            {
                return;
            }

            if (_currOutputIdx >= _currInteract.Outputs.Count)
            {
                DoInteractEnd();
                return;
            }

            if (_pendingMode == InteractPendingMode.None)
            {
                return;
            }

            if (TickInteractPending(dt))
            {
                _currOutputIdx += 1;
                HandleInteractOutputs();
            }
        }

        private void BeginInteractPending(InteractPendingMode mode, float timeoutSec)
        {
            _pendingMode = mode;
            _pendingElapsed = 0f;
            _pendingTimeoutSec = timeoutSec;
            _pendingNotified = false;
        }

        private void NotifyInteractPendingComplete()
        {
            if (_pendingMode == InteractPendingMode.ExternalNotify)
            {
                _pendingNotified = true;
            }
        }

        private bool TickInteractPending(float dt)
        {
            if (_pendingMode == InteractPendingMode.None)
            {
                return true;
            }

            _pendingElapsed += dt;
            bool finished = _pendingMode switch
            {
                InteractPendingMode.Timer => _pendingElapsed >= _pendingTimeoutSec,
                InteractPendingMode.ExternalNotify =>
                    _pendingNotified || _pendingElapsed >= _pendingTimeoutSec,
                _ => true
            };

            if (finished)
            {
                ResetInteractPending();
            }

            return finished;
        }

        private void ResetInteractPending()
        {
            _pendingMode = InteractPendingMode.None;
            _pendingElapsed = 0f;
            _pendingTimeoutSec = 0f;
            _pendingNotified = false;
        }


        
        public bool CheckTriggerInteract(int interactId, int playerId)
        {
            var interactItem = interactInfos.Find((item) => item.InteractId == interactId);
            if (interactItem == null)
            {
                return false;
            }

            var logicManager = Owner.LogicManager;
            var playerSystem = logicManager?.GetPlayerSystem(playerId);
            var playerEntity = logicManager?.GetPlayerEntity(playerId);
            if (logicManager == null || playerSystem == null || playerEntity == null)
            {
                return false;
            }

            var passed = true;
            foreach(var oneCond in interactItem.CheckCommonCond)
            {
                if (!logicManager.CheckCommonCond(oneCond, playerId))
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
                            if (playerEntity.IsInStealth())
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.HasLocalSwitch:
                        {
                            // Param3：开关名；Param4 可选：具名角色 CharacterKey（与 Owner 无关时查 Registry）
                            string switchName = oneCond.Param3;
                            string characterKey = oneCond.Param4;
                            bool has;
                            if (!string.IsNullOrEmpty(characterKey))
                            {
                                has = playerSystem.NamedNpcHasLocalSwitch(characterKey, switchName);
                            }
                            else
                            {
                                has = Owner.CheckLocalSwitch(switchName);
                            }

                            if (!has)
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.NoLocalSwitch:
                        {
                            // Param3：开关名；Param4 可选：具名角色 CharacterKey
                            string switchName = oneCond.Param3;
                            string characterKey = oneCond.Param4;
                            bool has;
                            if (!string.IsNullOrEmpty(characterKey))
                            {
                                has = playerSystem.NamedNpcHasLocalSwitch(characterKey, switchName);
                            }
                            else
                            {
                                has = Owner.CheckLocalSwitch(switchName);
                            }

                            if (has)
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.PlayerNotRetreating:
                        {
                            if (playerEntity.IsRetreating)
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.PlayerNotHoldTempSkill:
                        {
                            if (playerSystem.IsTempSkill(oneCond.Param3))
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.PlayerAttachCountAtLeast:
                        {
                            string attachKey = oneCond.Param3;
                            int needCount = oneCond.Param1 > 0 ? (int)oneCond.Param1 : 0;
                            int count = oneCond.Param2 == 1
                                ? playerEntity.CountAttachByType(attachKey)
                                : playerEntity.CountAttachById(attachKey);
                            if (count < needCount)
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.PlayerHasBuff:
                        {
                            if (string.IsNullOrEmpty(oneCond.Param3)
                                || !logicManager.globalBuffManager.CheckHasBuff(playerEntity.Id, oneCond.Param3))
                            {
                                passed = false;
                            }
                        }
                        break;
                    case InteractCheckCond.ECheckType.PlayerNoBuff:
                        {
                            if (!string.IsNullOrEmpty(oneCond.Param3)
                                && logicManager.globalBuffManager.CheckHasBuff(playerEntity.Id, oneCond.Param3))
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

        bool TryResolveUnitByStaticName(string staticName, out BaseUnitLogicEntity unit)
        {
            unit = null;
            if (string.IsNullOrEmpty(staticName))
            {
                return false;
            }

            var staticId = Owner.LogicManager.AreaManager.GetStaticIdByUniqName(staticName);
            Owner.LogicManager.AreaManager.RefreshInfoRuntimes.TryGetValue(staticId, out var refreshRuntime);
            if (refreshRuntime == null || refreshRuntime.EntityInstId == 0)
            {
                return false;
            }

            unit = Owner.LogicManager.GetLogicEntity(refreshRuntime.EntityInstId, false) as BaseUnitLogicEntity;
            return unit != null && unit is not PlayerLogicEntity;
        }

        bool TryResolveNamedPoint(string paramName, out Vector2 logicPos)
        {
            logicPos = default;
            var pName = Owner.GetRuntimeVariable(paramName);
            if (string.IsNullOrEmpty(pName))
            {
                pName = paramName;
            }

            if (string.IsNullOrEmpty(pName))
            {
                return false;
            }

            var p = Owner.LogicManager.AreaManager.cacheDatabase.FindNamedPointByName(pName);
            if (p == null)
            {
                return false;
            }

            logicPos = p.Value.Position;
            return true;
        }

        bool TryResolveVineEndLogicPos(string param3, out Vector2 logicPos)
        {
            logicPos = default;
            if (!string.IsNullOrEmpty(param3) && TryResolveNamedPoint(param3, out logicPos))
            {
                return true;
            }

            var pres = SceneAOIManager.Instance?.GetActivePresentation(Owner.Id);
            if (pres is VineGrowthPresenter vinePresenter
                && vinePresenter.TryGetApexWorldPosition(out var worldPos))
            {
                logicPos = MapLogicPosition.WorldToLogicPos(worldPos);
                return true;
            }

            Debug.Log("[EntityInteract] RelocateVineClimb failed to resolve vine end point.");
            return false;
        }

        void ApplyPendingTeleportTo(long playerId, Vector2 targetPos, float delaySec)
        {
            LogicFightEffectContext ctx = new LogicFightEffectContext(Owner.LogicManager, EFightCtxType.None, new EffectSourceInfo()
            {
                SrcType = ESourceType.Mechanism
            });
            ctx.TargetId = playerId;
            ctx.CastVec1 = targetPos;
            Owner.LogicManager.HandleLogicFightEffect(new MapAbilityEffectTeleportToCfg() { PendingTime = delaySec }, ctx);
        }

        void RunPlayerRelocate(PlayerLogicEntity player, PlayerRelocateSpec spec, out bool pending)
        {
            pending = true;
            float totalSec = PlayerRelocateTimings.GetTotalDuration(spec);
            BeginInteractPending(
                InteractPendingMode.ExternalNotify,
                totalSec + ExternalNotifyFallbackMarginSec);

            MainGameManager.Instance?.interactSystem?.SetInteractPause(totalSec);
            Owner.LogicManager.globalBuffManager.RequestAddBuff(player.Id, "lock_move", overrideDuration: totalSec);
            Owner.LogicManager.globalBuffManager.RequestAddBuff(player.Id, "as_presentation", overrideDuration: totalSec);
            Owner.LogicManager.viewer.DoPlayerRelocate(spec, () =>
            {
                ApplyPendingTeleportTo(player.Id, spec.FinalLogicPos, 0f);
                NotifyInteractPendingComplete();
            });
        }

        public bool TryTriggerInteract(int interactId, int playerId)
        {
            if(!CheckTriggerInteract(interactId, playerId))
            {
                return false;
            }

            var interactItem = interactInfos.Find((item) => item.InteractId == interactId);
            if (interactItem == null)
            {
                return false;
            }

            _interactingPlayerId = playerId;
            _isInteracting = true;
            _currOutputIdx = 0;
            _currInteract = interactItem;
            _interactFailed = false;

            HandleInteractOutputs();

            return true;
        }

        PlayerSystemManager GetInteractingPlayerSystem() =>
            Owner.LogicManager?.GetPlayerSystem(_interactingPlayerId);

        PlayerLogicEntity GetInteractingPlayerEntity() =>
            Owner.LogicManager?.GetPlayerEntity(_interactingPlayerId);

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
                            if (string.IsNullOrEmpty(mapName) || mapName == Owner.LogicManager.AreaManager.AreaOverlayId)
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
                    case LogicInteractOutput.EOutputType.RelocateGhostOrb:
                        {
                            var player = GetInteractingPlayerEntity();
                            if (player == null)
                            {
                                errOccur = true;
                                break;
                            }

                            if (!TryResolveNamedPoint(output.Param3, out var targetPos))
                            {
                                Debug.Log("relocate ghost orb no point found");
                                errOccur = true;
                                break;
                            }

                            RunPlayerRelocate(player, new PlayerRelocateSpec
                            {
                                TransitStyle = PlayerRelocateTransitStyle.GhostOrb,
                                FinalLogicPos = targetPos,
                            }, out pending);
                        }
                        break;

                    case LogicInteractOutput.EOutputType.RelocateWaitOnly:
                        {
                            var player = GetInteractingPlayerEntity();
                            if (player == null)
                            {
                                errOccur = true;
                                break;
                            }

                            if (!TryResolveNamedPoint(output.Param3, out var targetPos))
                            {
                                Debug.Log("relocate wait only no point found");
                                errOccur = true;
                                break;
                            }

                            RunPlayerRelocate(player, new PlayerRelocateSpec
                            {
                                TransitStyle = PlayerRelocateTransitStyle.WaitOnly,
                                FinalLogicPos = targetPos,
                            }, out pending);
                        }
                        break;

                    case LogicInteractOutput.EOutputType.SetGlobalSwitch:
                        {
                            string switchName = output.Param3;
                            if (string.IsNullOrEmpty(switchName))
                            {
                                errOccur = true;
                                break;
                            }

                            GetInteractingPlayerSystem()?.SetVariable(switchName);
                        }
                        break;

                    case LogicInteractOutput.EOutputType.StartStealth:
                        {
                            var player = GetInteractingPlayerEntity();
                            if (player == null)
                            {
                                errOccur = true;
                                break;
                            }

                            player.StartStealth(Owner.Id, Owner.Pos);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.CostItems:
                        {
                            string itemId = output.Param3;
                            long count = output.Param1;
                            GetInteractingPlayerSystem()?.CostItem(itemId, count);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.GiveItems:
                        {
                            string itemId = output.Param3;
                            long count = output.Param1;
                            GetInteractingPlayerSystem()?.GiveItemToPlayer(itemId, count);
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.SetLocalSwitch:
                        {
                            // Param3：开关名；Param4 可选：具名角色 CharacterKey（写 WorldNpcCharacterPersistRegistry，场内同 Key 的 NPC 立即可查）
                            string switchName = output.Param3;
                            string characterKey = output.Param4;
                            if (!string.IsNullOrEmpty(characterKey))
                            {
                                GetInteractingPlayerSystem()?.SetNamedNpcLocalSwitch(characterKey, switchName, true);
                            }
                            else
                            {
                                Owner.SetLocalSwitch(switchName, true);
                            }
                        }
                        break;
                    case Config.LogicInteractOutput.EOutputType.UnsetLocalSwitch:
                        {
                            string switchName = output.Param3;
                            string characterKey = output.Param4;
                            if (!string.IsNullOrEmpty(characterKey))
                            {
                                GetInteractingPlayerSystem()?.SetNamedNpcLocalSwitch(characterKey, switchName, false);
                            }
                            else
                            {
                                Owner.SetLocalSwitch(switchName, false);
                            }
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
                            GetInteractingPlayerEntity()?.TryStartRetreating();
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
                            Owner.LogicManager.PendingCostDayPeriod();
                        }
                        break;

                    case LogicInteractOutput.EOutputType.GrantRune:
                        {
                            if (string.IsNullOrEmpty(output.Param3))
                            {
                                Debug.LogWarning("GrantRune: Param3 rune_id is empty");
                                errOccur = true;
                                break;
                            }

                            var playerSystem = GetInteractingPlayerSystem();
                            if (playerSystem == null || !playerSystem.TryGrantRune(output.Param3))
                            {
                                Debug.LogWarning($"GrantRune failed: {output.Param3}");
                                errOccur = true;
                            }
                        }
                        break;

                    case LogicInteractOutput.EOutputType.GrantTempSkill:
                        {
                            if (string.IsNullOrEmpty(output.Param3))
                            {
                                Debug.LogWarning("GrantTempSkill: Param3 skillId is empty");
                                break;
                            }

                            GetInteractingPlayerSystem()?.GrantTempSkill(output.Param3, output.Param1);
                            My.UI.OverworldHUDPanel.Instance?.MainBottomBar?.Refresh();
                            My.UI.PlayerHumanItemBarPanel.RefreshFromGame();
                        }
                        break;

                    case LogicInteractOutput.EOutputType.ChangeUnitFaction:
                        {
                            if (output.TargetType != LogicInteractOutput.ETargetType.StaticName)
                            {
                                Debug.LogWarning("ChangeUnitFaction: TargetType must be StaticName");
                                errOccur = true;
                                break;
                            }

                            if (!TryResolveUnitByStaticName(output.StaticName, out var unit))
                            {
                                Debug.LogWarning($"ChangeUnitFaction: no unit at StaticName '{output.StaticName}'");
                                errOccur = true;
                                break;
                            }

                            var newFaction = (EFactionId)output.Param1;
                            if (newFaction == EFactionId.None)
                            {
                                Debug.LogWarning("ChangeUnitFaction: Param1 faction is None");
                                errOccur = true;
                                break;
                            }

                            // Param2==1 时不强制脱战；默认脱战并清仇恨
                            bool leaveCombat = output.Param2 != 1;
                            unit.ApplyRuntimeFactionChange(newFaction, leaveCombat);
                            Debug.Log($"ChangeUnitFaction: entity {unit.Id} faction -> {newFaction}");
                        }
                        break;

                    case LogicInteractOutput.EOutputType.MarkCharacterValue:
                        {
                            string charKey = output.Param3;
                            //Owner.LogicManager.worldPersistState?.NpcCharacters?.SetDesireCrystalTaken(charKey, true);
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
                            BeginInteractPending(InteractPendingMode.Timer, output.Param1 * 0.001f);
                            pending = true;
                        }
                        break;
                    case LogicInteractOutput.EOutputType.SelfAnim:
                        {
                            var durationSec = output.Param1 * 0.001f;
                            BeginInteractPending(InteractPendingMode.Timer, durationSec);
                            pending = true;

                            var animName = output.Param3;
                            (Owner as LogicEntityInteractPoint)?.NotifySelfAnim(animName, durationSec);
                        }
                        break;
                    case LogicInteractOutput.EOutputType.RelocateVineClimb:
                        {
                            var player = GetInteractingPlayerEntity();
                            if (player == null)
                            {
                                errOccur = true;
                                break;
                            }

                            Vector2 entryPos = Owner.Pos;

                            if (!TryResolveVineEndLogicPos(output.Param3, out var endPos))
                            {
                                errOccur = true;
                                break;
                            }

                            if (!TryResolveNamedPoint(output.Param4, out var landPos))
                            {
                                Debug.Log($"vine climb land point not found: {output.Param4}");
                                errOccur = true;
                                break;
                            }

                            RunPlayerRelocate(player, new PlayerRelocateSpec
                            {
                                TransitStyle = PlayerRelocateTransitStyle.VineClimb,
                                EntryLogicPos = entryPos,
                                MidLogicPos = endPos,
                                FinalLogicPos = landPos,
                            }, out pending);
                        }
                        break;

                    case LogicInteractOutput.EOutputType.RelocateFakeJump2D:
                        {
                            var player = GetInteractingPlayerEntity();
                            if (player == null)
                            {
                                errOccur = true;
                                break;
                            }

                            Vector2 entryPos = Owner.Pos;

                            if (!TryResolveNamedPoint(output.Param3, out var takeoffPos))
                            {
                                Debug.Log($"fake jump takeoff point not found: {output.Param3}");
                                errOccur = true;
                                break;
                            }

                            if (!TryResolveNamedPoint(output.Param4, out var landPos))
                            {
                                Debug.Log($"fake jump land point not found: {output.Param4}");
                                errOccur = true;
                                break;
                            }

                            RunPlayerRelocate(player, new PlayerRelocateSpec
                            {
                                TransitStyle = PlayerRelocateTransitStyle.FakeJump2D,
                                EntryLogicPos = entryPos,
                                MidLogicPos = takeoffPos,
                                FinalLogicPos = landPos,
                            }, out pending);
                        }
                        break;

                    case LogicInteractOutput.EOutputType.ShowCameraOverride:
                        {
                            Vector2 focusPos;
                            if (!TryResolveNamedPoint(output.Param3, out focusPos))
                            {
                                var focusTargetId = GetInteractTarget(output);
                                if (focusTargetId != 0)
                                {
                                    var focusEntity = Owner.LogicManager.GetLogicEntity(focusTargetId, false);
                                    if (focusEntity == null)
                                    {
                                        Debug.LogWarning("ShowCameraOverride: focus target not found");
                                        errOccur = true;
                                        break;
                                    }

                                    focusPos = focusEntity.Pos;
                                }
                                else
                                {
                                    focusPos = Owner.Pos;
                                }
                            }

                            long pinId = 0;
                            if (output.TargetType == LogicInteractOutput.ETargetType.StaticName)
                            {
                                pinId = GetInteractTarget(output);
                            }

                            float durationSec = output.Param1 * 0.001f;
                            if (durationSec <= 0f)
                            {
                                durationSec = MainGameManager.DefaultCameraOverrideDuration;
                            }

                            float visualRadius = output.Param2 > 0
                                ? output.Param2
                                : MainGameManager.DefaultCameraOverrideVisualRadius;

                            MainGameManager.Instance?.ShowCameraOverrideFix(
                                focusPos,
                                durationSec,
                                pinId,
                                visualRadius,
                                blockInput: true);

                            BeginInteractPending(InteractPendingMode.Timer, durationSec);
                            pending = true;
                        }
                        break;

                    case LogicInteractOutput.EOutputType.ModifyLocalIntValue:
                        {
                            string key = output.Param3;
                            if (string.IsNullOrEmpty(key))
                            {
                                errOccur = true;
                                break;
                            }

                            var writeMode = (LogicInteractOutput.ELocalIntWriteMode)output.Param2;
                            int operand = (int)output.Param1;
                            long targetEntityId = GetInteractTarget(output);
                            var target = targetEntityId == Owner.Id
                                ? Owner
                                : Owner.LogicManager.GetLogicEntity(targetEntityId, false) as IEntityInteractable;

                            int current;
                            if (target != null)
                            {
                                current = target.GetLocalIntValue(key);
                                target.SetLocalIntValue(key, ResolveLocalIntWriteValue(writeMode, current, operand));
                            }
                            else if (output.TargetType == LogicInteractOutput.ETargetType.StaticName
                                     && !string.IsNullOrEmpty(output.StaticName))
                            {
                                var registry = Owner.LogicManager.worldPersistState?.MapInteractPoints;
                                if (registry == null)
                                {
                                    errOccur = true;
                                    break;
                                }

                                current = registry.GetLocalIntValue(output.StaticName, key);
                                registry.SetLocalIntValue(
                                    output.StaticName,
                                    key,
                                    ResolveLocalIntWriteValue(writeMode, current, operand));
                            }
                            else
                            {
                                errOccur = true;
                            }
                        }
                        break;

                    case LogicInteractOutput.EOutputType.RecordRefreshSettlementDay:
                        {
                            long targetEntityId = GetInteractTarget(output);
                            var target = targetEntityId == Owner.Id
                                ? Owner as IWithTimedRefreshState
                                : Owner.LogicManager.GetLogicEntity(targetEntityId, false) as IWithTimedRefreshState;
                            int settlementDayIndex = Owner.LogicManager.SettlementDayIndex;

                            if (target != null)
                            {
                                target.RecordRefreshSettlementDay(settlementDayIndex);
                            }
                            else if (output.TargetType == LogicInteractOutput.ETargetType.StaticName
                                     && !string.IsNullOrEmpty(output.StaticName))
                            {
                                var registry = Owner.LogicManager.worldPersistState?.MapInteractPoints;
                                if (registry == null)
                                {
                                    errOccur = true;
                                    break;
                                }

                                registry.RecordRefreshSettlementDay(output.StaticName, settlementDayIndex);
                            }
                            else
                            {
                                errOccur = true;
                            }
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
                _interactFailed |= errOccur;
                DoInteractEnd();
            }
        }

        public void HandleOneDirectOutput()
        {

        }

        int ResolveLocalIntWriteValue(
            LogicInteractOutput.ELocalIntWriteMode mode,
            int current,
            int operand)
        {
            return mode switch
            {
                LogicInteractOutput.ELocalIntWriteMode.Add => current + operand,
                LogicInteractOutput.ELocalIntWriteMode.Set => operand,
                _ => current,
            };
        }

        private void DoInteractEnd()
        {
            if (_currInteract == null) return;
            Debug.Log($"DoInteractEnd. {_currInteract?.InteractId}");

            if (!_interactFailed)
            {
                PlayerEventBus.Publish(new PlayerEntityInteractionCompletedEvent
                {
                    CfgId = OwnerEntity?.CfgId,
                    UniqName = OwnerEntity?.SrcUniqName,
                    InteractId = _currInteract.InteractId,
                });
            }

            _isInteracting = false;
            _currOutputIdx = 0;
            _currInteract = null;
            _interactFailed = false;

            ResetInteractPending();
        }
    }

}

