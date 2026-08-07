using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using My.Map.Entity;
using My.Map.Logic;
using My.Player;
using My.Player.Bag;
using My.UI;
using UnityEngine;

namespace My.Home
{
    public sealed class TransportLootSystem
    {
        public const string UnlockSwitchId = "transport_marker_unlocked";
        public const string UnlockGrantEventId = "transport_camp_marker_unlock";

        readonly GameLogicManager _logic;

        public TransportLootSystem(GameLogicManager logic)
        {
            _logic = logic;
        }

        public bool IsMarkerUnlocked()
        {
            return _logic?.playerDataManager?.CheckHasParam(UnlockSwitchId) == true;
        }

        public void UnlockMarkerPlacement()
        {
            if (_logic?.playerDataManager == null)
            {
                return;
            }

            if (IsMarkerUnlocked())
            {
                return;
            }

            _logic.playerDataManager.SetVariable(UnlockSwitchId);
            Debug.Log("[TransportLoot] transport marker placement unlocked.");
        }

        public bool CanPlaceMarkerOnCurrentMap(out string failReason)
        {
            failReason = null;
            if (_logic == null || !_logic.IsInfiltrationRun)
            {
                failReason = "not_infiltration";
                return false;
            }

            if (!IsMarkerUnlocked())
            {
                failReason = "not_unlocked";
                return false;
            }

            var overlay = _logic.AreaManager?.cacheMapOverlayCfg;
            if (overlay == null || (!overlay.IsDangerArea && !overlay.IsMagicSensitiveArea))
            {
                failReason = "not_scavenge_map";
                return false;
            }

            return true;
        }

        public bool HasActiveMarkerOnCurrentMap()
        {
            var overlayId = _logic?.AreaManager?.AreaOverlayId;
            if (string.IsNullOrEmpty(overlayId))
            {
                return false;
            }

            if (_logic.GameSession.TransportMarkerEntityId > 0
                && overlayId == _logic.GameSession.TransportMarkerOverlayId)
            {
                var entity = _logic.GetLogicEntity(_logic.GameSession.TransportMarkerEntityId, false);
                if (entity is TransportLootPointLogicEntity
                    && _logic.AreaManager.Repo.Records.TryGetValue(_logic.GameSession.TransportMarkerEntityId, out var rec)
                    && !rec.MarkDestroyed)
                {
                    return true;
                }
            }

            return FindMarkerRecordOnCurrentMap() != null;
        }

        LogicEntityRecord4LootPoint FindMarkerRecordOnCurrentMap()
        {
            var area = _logic?.AreaManager;
            if (area == null)
            {
                return null;
            }

            foreach (var rec in area.Repo.Records.Values)
            {
                if (rec is LogicEntityRecord4LootPoint lootRec
                    && lootRec.IsTransportMarker
                    && !lootRec.MarkDestroyed)
                {
                    return lootRec;
                }
            }

            return null;
        }

        public bool TryPlaceMarkerAtPlayerFeet(out string failReason)
        {
            failReason = null;
            if (!CanPlaceMarkerOnCurrentMap(out failReason))
            {
                return false;
            }

            var player = _logic.playerLogicEntity;
            if (player == null)
            {
                failReason = "no_player";
                return false;
            }

            DestroyExistingMarkerAndDropContents();

            var record = new LogicEntityRecord4LootPoint
            {
                Id = GameLogicManager.LogicEntityIdInst++,
                EntityType = EEntityType.LootPoint,
                CfgId = TransportLootPointLogicEntity.MarkerCfgId,
                Position = player.Pos,
                IsTransportMarker = true,
                ItemInitialized = true,
                InnerItems = new List<ItemStack>(),
                SrcUniqName = "transport_marker_session",
            };

            _logic.AddNewEntityRecord(record, isCreate: true);
            _logic.GameSession.TransportMarkerEntityId = record.Id;
            _logic.GameSession.TransportMarkerOverlayId = _logic.AreaManager.AreaOverlayId ?? string.Empty;
            _logic.viewer?.ShowFakeFxEffect("已放置运输标记", player.Pos);
            return true;
        }

        void DestroyExistingMarkerAndDropContents()
        {
            var marker = GetActiveMarkerEntity();
            if (marker != null)
            {
                marker.SyncRecordForPersistence();
                DropMarkerContentsAt(marker.LootItems, marker.Pos);
                CloseLootUiIfTargeting(marker);
                DestroyMarkerRecord(marker.Id);
                return;
            }

            var rec = FindMarkerRecordOnCurrentMap();
            if (rec == null)
            {
                return;
            }

            DropMarkerContentsAt(rec.InnerItems, rec.Position);
            DestroyMarkerRecord(rec.Id);
        }

        void DestroyMarkerRecord(long markerId)
        {
            if (markerId <= 0)
            {
                return;
            }

            _logic.AreaManager?.ForceDestroyEntityNow(markerId, "transport_marker_replace");
            if (_logic.GameSession.TransportMarkerEntityId == markerId)
            {
                _logic.GameSession.TransportMarkerEntityId = 0;
                _logic.GameSession.TransportMarkerOverlayId = string.Empty;
            }
        }

        static void CloseLootUiIfTargeting(ILootableObj marker)
        {
            if (marker == null || LootPointUIPanel.Instance?.Loot != marker)
            {
                return;
            }

            UIOrchestrator.Instance?.TryQuitLootDetailMode();
        }

        void DropMarkerContentsAt(List<ItemStack> stacks, Vector2 center)
        {
            if (stacks == null || _logic?.globalDropCollection == null)
            {
                return;
            }

            foreach (var stack in stacks)
            {
                if (stack == null || stack.Count <= 0 || string.IsNullOrEmpty(stack.ItemID))
                {
                    continue;
                }

                var dropPos = center + UnityEngine.Random.insideUnitCircle * 0.35f;
                var essence = stack.InstanceInfo?.Get<ItemInstance4PremiumEssence>();
                if (essence != null)
                {
                    _logic.globalDropCollection.CreateDrop(
                        new DropUtils.DropReward
                        {
                            ItemId = stack.ItemID,
                            Amount = (int)Math.Min(int.MaxValue, stack.Count),
                            PremiumEssence = essence,
                        },
                        dropPos,
                        false,
                        center);
                }
                else
                {
                    _logic.globalDropCollection.CreateDrop(
                        stack.ItemID,
                        stack.Count,
                        dropPos,
                        false,
                        center);
                }
            }
        }

        public TransportLootPointLogicEntity GetActiveMarkerEntity()
        {
            if (_logic == null)
            {
                return null;
            }

            var area = _logic.AreaManager;
            if (area == null)
            {
                return null;
            }

            if (_logic.GameSession.TransportMarkerEntityId > 0)
            {
                var entity = _logic.GetLogicEntity(_logic.GameSession.TransportMarkerEntityId, false);
                if (entity is TransportLootPointLogicEntity marker
                    && area.Repo.Records.TryGetValue(marker.Id, out var boundRec)
                    && !boundRec.MarkDestroyed)
                {
                    return marker;
                }
            }

            var rec = FindMarkerRecordOnCurrentMap();
            if (rec == null)
            {
                return null;
            }

            var spawned = _logic.GetLogicEntity(rec.Id, false);
            return spawned is TransportLootPointLogicEntity transport
                && area.Repo.Records.TryGetValue(transport.Id, out var spawnedRec)
                && !spawnedRec.MarkDestroyed
                ? transport
                : null;
        }

        public void BindSessionMarkerIfNeeded()
        {
            if (_logic == null || !_logic.IsInfiltrationRun)
            {
                return;
            }

            var rec = FindMarkerRecordOnCurrentMap();
            if (rec == null)
            {
                return;
            }

            _logic.GameSession.TransportMarkerEntityId = rec.Id;
            _logic.GameSession.TransportMarkerOverlayId = _logic.AreaManager?.AreaOverlayId ?? string.Empty;
        }

        public void DepositMarkerContentsToPending()
        {
            var marker = GetActiveMarkerEntity();
            if (marker == null || !marker.HasAnyItem())
            {
                return;
            }

            var overlay = _logic.AreaManager?.cacheMapOverlayCfg;
            var regionKey = GameRegionUtil.ResolveRegionKey(overlay);
            var homeLogicAreaId = GameRegionUtil.ResolveHomeLogicAreaForOverlay(overlay);
            var overlayId = _logic.AreaManager?.AreaOverlayId ?? string.Empty;
            var civilization = _logic.playerDataManager?.ProgressionSystem?.HumanCivilization;
            civilization?.AddTransportPendingItems(
                regionKey,
                homeLogicAreaId,
                overlayId,
                marker.LootItems);

            ClearMarkerContainer(marker);
            Debug.Log("[TransportLoot] marker contents deposited to pending transport storage.");
        }

        static void ClearMarkerContainer(TransportLootPointLogicEntity marker)
        {
            var container = marker.GetLootItemContainer();
            for (int i = 0; i < marker.LootItems.Count; i++)
            {
                container.SetItemData(i, null);
            }
        }

        public void GetTransportPickupBounds(string homeLogicAreaId, out int minPickup, out int maxPickup)
        {
            minPickup = 0;
            maxPickup = 0;
            if (_logic?.townFacilityDevelopmentSystem == null
                || string.IsNullOrEmpty(homeLogicAreaId))
            {
                return;
            }

            if (_logic.townFacilityDevelopmentSystem.GetFacilityDevelopmentLevel(
                    homeLogicAreaId, "transport_camp") <= 0)
            {
                return;
            }

            minPickup = (int)_logic.townFacilityDevelopmentSystem.GetBuildingAttribute(
                homeLogicAreaId, "transport_camp", EBuildingAttribute.TransportMinPickupCount);
            maxPickup = (int)_logic.townFacilityDevelopmentSystem.GetBuildingAttribute(
                homeLogicAreaId, "transport_camp", EBuildingAttribute.TransportMaxPickupCount);
        }
    }
}
