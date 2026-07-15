using System;
using System.Collections.Generic;
using System.Linq;
using My.Config;
using My.Home;
using My.UI.Home;
using My.Map.Logic;
using My.Map;
using My.Saving;
using UnityEngine;

namespace My.Home
{
    public sealed class FixedFacilityInfo
    {
        public long InstanceId;
        public string FacilityId;
        public bool Removed;
        public HomeFacilityLogicEntity Entity;
        public FacilityDefinition Definition;
    }

    public class HomeDataManager
    {
        public GameLogicManager LogicManager { get; private set; }
        private string _townId;
        private SaveData _saveDataContext;

        public string CurrentTownId => _townId;
        public int TownProsperity { get; private set; }
        public int TownCurrentPopulation { get; private set; }
        public int TownInfluence { get; private set; }
        public int TownStability { get; private set; }
        public event Action EvOnTownEcoChanged;
        public event Action EvOnFacilityUpdate;

        public List<FixedFacilityInfo> FixedFacilities { get; } = new();
        public IReadOnlyList<FixedFacilityInfo> FacilityInfos => FixedFacilities;
        public IReadOnlyList<TownFacilityPersist> FacilityDevelopment => LogicManager?.worldPersistState?.GetTownFacilities(CurrentTownId) ?? Array.Empty<TownFacilityPersist>();
        public List<string> RepairedFacilityList = new();
        public int DailyNormalHTimes;

        public HomeDataManager(GameLogicManager logicManager)
        {
            LogicManager = logicManager;
        }

        public void RefreshFixedFacilities()
        {
            FixedFacilities.Clear();
            var records = LogicManager?.AreaManager?.Repo?.Records;
            if (records == null) return;
            foreach (var pair in records)
            {
                if (pair.Value?.EntityType != EEntityType.HomeFacility) continue;
                var entity = LogicManager.AreaManager.GetLogicEntiy(pair.Key, false) as HomeFacilityLogicEntity;
                if (entity == null) continue;
                FixedFacilities.Add(new FixedFacilityInfo
                {
                    InstanceId = entity.Id,
                    FacilityId = entity.CfgId,
                    Entity = entity,
                    Definition = FacilityDefinitionCatalog.Get(entity.CfgId),
                });
            }
        }

        public FixedFacilityInfo FindFacilityById(long instanceId)
        {
            RefreshFixedFacilities();
            return FixedFacilities.Find(item => item.InstanceId == instanceId);
        }

        public string GetFacilityId(FixedFacilityInfo facility) => facility?.FacilityId;

        public void LoadHomeData(SaveData saveData)
        {
            _saveDataContext = saveData;
            var town = saveData?.TownDevelopmentById != null && !string.IsNullOrEmpty(CurrentTownId)
                && saveData.TownDevelopmentById.TryGetValue(CurrentTownId, out var persistedTown)
                ? persistedTown : null;
            TownProsperity = town?.Prosperity ?? 0;
            TownCurrentPopulation = town?.Population ?? 0;
            TownInfluence = town?.Influence ?? 0;
            TownStability = town?.Stability ?? 0;
            RefreshFixedFacilities();
        }

        public void SetTownContext(string townId)
        {
            if (string.IsNullOrEmpty(townId) || string.Equals(_townId, townId, StringComparison.Ordinal)) return;
            _townId = townId;
            if (_saveDataContext != null) LoadHomeData(_saveDataContext);
        }

        public void ApplyToSaveData(SaveData data)
        {
            if (data == null || string.IsNullOrEmpty(CurrentTownId)) return;
            data.TownDevelopmentById ??= new Dictionary<string, TownDevelopmentPersist>();
            if (!data.TownDevelopmentById.TryGetValue(CurrentTownId, out var town) || town == null)
            {
                town = new TownDevelopmentPersist();
                data.TownDevelopmentById[CurrentTownId] = town;
            }
            town.Prosperity = Mathf.Max(0, TownProsperity);
            town.Population = Mathf.Max(0, TownCurrentPopulation);
            town.Influence = Mathf.Max(0, TownInfluence);
            town.Stability = Mathf.Max(0, TownStability);
        }

        public TownFacilityPersist GetTownFacilityBySite(string townId, int siteId) => LogicManager?.worldPersistState?.GetTownFacilityBySite(townId, siteId, false);
        public TownFacilityPersist GetTownFacility(string townId, long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(townId, instanceId, facilityId);
        public int GetFacilityDevelopmentLevel(int siteId) => LogicManager?.worldPersistState?.GetSiteDevelopmentLevel(CurrentTownId, siteId) ?? 0;
        public int GetFacilityDevelopmentLevel(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.DevelopmentLevel ?? 0;
        public bool IsFacilityConstructed(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.IsConstructed == true;
        public string GetFacilityRenovation(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.RenovationId;

        public bool TryLearnFacilityRenovation(int siteId, string renovationId, out string failReason)
        {
            failReason = null;
            var site = TownFacilitySiteCatalog.Get(siteId);
            if (site == null)
            {
                failReason = "invalid_site";
                return false;
            }

            return TryLearnFacilityRenovation(0, site.FacilityCfgId, renovationId, out failReason, siteId);
        }

        public bool TryLearnFacilityRenovation(long instanceId, string facilityId, string renovationId, out string failReason, int siteId = 0)
        {
            failReason = null;
            TownFacilityPersist development = siteId > 0
                ? GetTownFacilityBySite(CurrentTownId, siteId)
                : GetTownFacility(CurrentTownId, instanceId, facilityId);
            if (development == null || !development.IsConstructed)
            {
                failReason = "facility_not_constructed";
                return false;
            }

            if (string.IsNullOrEmpty(renovationId))
            {
                SetFacilityDevelopment(instanceId, facilityId, development.DevelopmentLevel, null, development.AssignedWorkforce, siteId);
                return true;
            }

            var renovation = FacilityRenovationCatalog.Get(facilityId, renovationId);
            if (renovation == null)
            {
                failReason = "invalid_renovation";
                return false;
            }

            if (!FacilityRenovationCatalog.CanLearn(renovation, development.DevelopmentLevel, LogicManager, out failReason))
            {
                return false;
            }

            if (!PayRenovationLearnCosts(renovation, out failReason))
            {
                return false;
            }

            SetFacilityDevelopment(instanceId, facilityId, development.DevelopmentLevel, renovationId, development.AssignedWorkforce, siteId);
            return true;
        }

        bool PayRenovationLearnCosts(FacilityRenovationDefinition renovation, out string failReason)
        {
            failReason = null;
            var pdm = LogicManager?.playerDataManager;
            if (pdm == null || renovation?.LearnCosts == null || renovation.LearnCosts.Count == 0)
            {
                return true;
            }

            foreach (var cost in renovation.LearnCosts)
            {
                if (cost == null || string.IsNullOrEmpty(cost.ItemId) || cost.Count <= 0)
                {
                    continue;
                }

                if (pdm.CostItem(cost.ItemId, cost.Count) < cost.Count)
                {
                    failReason = "not_enough_item";
                    return false;
                }
            }

            return true;
        }

        public TownFacilityPersist EnsureFacilityPersistRecord(FixedFacilityInfo facility, int defaultLevel = 1)
        {
            if (facility == null || string.IsNullOrEmpty(facility.FacilityId) || string.IsNullOrEmpty(CurrentTownId))
            {
                return null;
            }

            var existing = GetTownFacility(CurrentTownId, facility.InstanceId, facility.FacilityId);
            if (existing != null)
            {
                return existing;
            }

            int level = Mathf.Max(0, defaultLevel);
            SetFacilityDevelopment(facility.InstanceId, facility.FacilityId, level, null, 0);
            return GetTownFacility(CurrentTownId, facility.InstanceId, facility.FacilityId);
        }

        public int GetHelperWorkforce(int siteId, long instanceId, string facilityId)
        {
            if (siteId > 0)
            {
                return GetTownFacilityBySite(CurrentTownId, siteId)?.AssignedWorkforce ?? 0;
            }

            return GetFacilityWorkforce(instanceId, facilityId);
        }

        public int GetFacilityWorkforce(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.AssignedWorkforce ?? 0;

        public string GetFacilitySupervisor(int siteId, long instanceId, string facilityId, int slotIndex)
        {
            return LogicManager?.worldPersistState?.GetFacilitySupervisor(
                CurrentTownId, siteId, instanceId, facilityId, slotIndex);
        }

        public int GetMaxSupervisorSlots(FacilityDefinition definition) => Mathf.Max(0, definition?.MaxSupervisorSlots ?? 0);

        public int GetHelperWorkforceCapacity(FacilityDefinition definition) => Mathf.Max(0, definition?.MaxWorkforce ?? 0);

        public bool SupportsHelperWorkforce(FacilityDefinition definition)
        {
            return definition != null
                   && definition.MaxWorkforce > 0
                   && definition.Capabilities.HasFlag(FacilityCapability.Workforce);
        }

        public bool SupportsSupervisors(FacilityDefinition definition) => GetMaxSupervisorSlots(definition) > 0;

        public List<FacilitySupervisorCandidate> GetAssignableSupervisors(string logicAreaId)
        {
            return TownFacilityTownCharacterCatalog.GetAssignableSupervisors(LogicManager, logicAreaId);
        }

        public bool TrySetFacilitySupervisor(
            int siteId,
            long instanceId,
            string facilityId,
            int slotIndex,
            string characterKey,
            out string failReason)
        {
            failReason = null;
            if (string.IsNullOrEmpty(CurrentTownId) || slotIndex < 0)
            {
                failReason = "invalid_args";
                return false;
            }

            var definition = FacilityDefinitionCatalog.Get(facilityId);
            if (!SupportsSupervisors(definition) || slotIndex >= GetMaxSupervisorSlots(definition))
            {
                failReason = "invalid_supervisor_slot";
                return false;
            }

            var persist = siteId > 0
                ? GetTownFacilityBySite(CurrentTownId, siteId)
                : GetTownFacility(CurrentTownId, instanceId, facilityId);
            if (persist == null || !persist.IsConstructed)
            {
                failReason = "facility_not_constructed";
                return false;
            }

            if (!string.IsNullOrEmpty(characterKey))
            {
                if (!TownFacilitySupervisorCatalog.CanAssign(characterKey))
                {
                    failReason = "character_not_assignable";
                    return false;
                }

                var inTown = TownFacilityTownCharacterCatalog.GetCharacterKeysInTown(LogicManager, CurrentTownId);
                if (!inTown.Contains(characterKey))
                {
                    failReason = "character_not_in_town";
                    return false;
                }

                if (IsSupervisorAssignedElsewhere(characterKey, siteId, instanceId, facilityId))
                {
                    failReason = "supervisor_already_assigned";
                    return false;
                }
            }

            LogicManager?.worldPersistState?.SetFacilitySupervisor(
                CurrentTownId, siteId, instanceId, facilityId, slotIndex, characterKey);
            EvOnFacilityUpdate?.Invoke();
            return true;
        }

        bool IsSupervisorAssignedElsewhere(string characterKey, int siteId, long instanceId, string facilityId)
        {
            var facilities = LogicManager?.worldPersistState?.GetTownFacilities(CurrentTownId);
            if (facilities == null)
            {
                return false;
            }

            foreach (var facility in facilities)
            {
                if (facility?.SupervisorSlots == null)
                {
                    continue;
                }

                bool sameFacility = facility.SiteId > 0
                    ? facility.SiteId == siteId
                    : facility.InstanceId == instanceId && facility.FacilityId == facilityId;
                if (sameFacility)
                {
                    continue;
                }

                foreach (var slot in facility.SupervisorSlots)
                {
                    if (slot != null
                        && slot.CharacterKey == characterKey
                        && !string.IsNullOrEmpty(slot.CharacterKey))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SetFacilityDevelopment(long instanceId, string facilityId, int level, string renovationId, int assignedWorkforce, int siteId = 0)
        {
            if (string.IsNullOrEmpty(CurrentTownId) || string.IsNullOrEmpty(facilityId))
            {
                return;
            }

            LogicManager?.worldPersistState?.SetTownFacilityDevelopment(
                CurrentTownId, instanceId, facilityId, level, renovationId, assignedWorkforce, level > 0, siteId);
            EvOnTownEcoChanged?.Invoke();
            EvOnFacilityUpdate?.Invoke();
        }

        public void SetTownProsperity(int value) { value = Mathf.Max(0, value); if (TownProsperity == value) return; TownProsperity = value; EvOnTownEcoChanged?.Invoke(); }
        public void SetTownCurrentPopulation(int value) { value = Mathf.Max(0, value); if (TownCurrentPopulation == value) return; TownCurrentPopulation = value; EvOnTownEcoChanged?.Invoke(); }

        public FacilityDefinition GetFacilityDefinition(FixedFacilityInfo facility) => facility?.Definition ?? FacilityDefinitionCatalog.Get(facility?.FacilityId);
        public bool SupportsFacilityWorkforce(FixedFacilityInfo facility) => SupportsHelperWorkforce(GetFacilityDefinition(facility));
        public int GetFacilityWorkforceCapacity(FixedFacilityInfo facility) => GetHelperWorkforceCapacity(GetFacilityDefinition(facility));

        public int ComputeAssignedWorkforceTotal()
        {
            int sum = 0;
            var facilities = LogicManager?.worldPersistState?.GetTownFacilities(CurrentTownId);
            if (facilities == null)
            {
                return sum;
            }

            foreach (var facility in facilities)
            {
                if (facility == null || facility.DevelopmentLevel <= 0)
                {
                    continue;
                }

                var definition = FacilityDefinitionCatalog.Get(facility.FacilityId);
                if (!SupportsHelperWorkforce(definition))
                {
                    continue;
                }

                sum += Mathf.Max(0, facility.AssignedWorkforce);
            }

            return sum;
        }

        public bool TrySetHelperWorkforce(int siteId, long instanceId, string facilityId, int workers, out string failReason)
        {
            failReason = null;
            var definition = FacilityDefinitionCatalog.Get(facilityId);
            if (!SupportsHelperWorkforce(definition))
            {
                failReason = "facility_has_no_workforce";
                return false;
            }

            TownFacilityPersist development = siteId > 0
                ? GetTownFacilityBySite(CurrentTownId, siteId)
                : GetTownFacility(CurrentTownId, instanceId, facilityId);
            if (development == null || !development.IsConstructed)
            {
                failReason = "facility_not_constructed";
                return false;
            }

            workers = Mathf.Clamp(workers, 0, GetHelperWorkforceCapacity(definition));
            SetFacilityDevelopment(instanceId, facilityId, development.DevelopmentLevel, development.RenovationId, workers, siteId);
            return true;
        }

        public bool TrySetFacilityWorkforce(long instanceId, int workers, out string failReason)
        {
            failReason = null;
            var facility = FindFacilityById(instanceId);
            if (facility == null || facility.Removed)
            {
                failReason = "facility_not_found";
                return false;
            }

            return TrySetHelperWorkforce(0, instanceId, facility.FacilityId, workers, out failReason);
        }

        public void OnPlayerEnterHome() { }
        public void EnsureHomeFacilityRecordsRegistered() => RefreshFixedFacilities();
        public void DoRepairFacility(string ruinCfgId, Vector2 repairPos)
        {
            var targetFacilityId = TownFacilityInteractUtil.ResolveFacilityIdFromRuin(ruinCfgId);
            if (string.IsNullOrEmpty(targetFacilityId) || LogicManager == null)
            {
                return;
            }

            var logicAreaId = TownFacilityUtil.ResolveCurrentLogicAreaId(LogicManager.AreaManager);
            if (string.IsNullOrEmpty(logicAreaId))
            {
                return;
            }

            var site = TownFacilitySiteCatalog.FindByMapAndFacility(logicAreaId, targetFacilityId);
            if (site != null)
            {
                if (LogicManager.townFacilityDevelopmentSystem?.TryUpgradeFacility(logicAreaId, site.Id, out _) == true)
                {
                    EvOnFacilityUpdate?.Invoke();
                }

                return;
            }

            if (LogicManager.townFacilityDevelopmentSystem?.TryUpgradeFacility(logicAreaId, 0, targetFacilityId, out _) == true)
            {
                EvOnFacilityUpdate?.Invoke();
            }
        }
        public bool CheckHasFacility(string facilityId) { RefreshFixedFacilities(); return FixedFacilities.Exists(item => item.FacilityId == facilityId && !item.Removed); }
        public void RefreshProduceValue() { }
        public void DoDayEndBalance() { DailyNormalHTimes = 0; }
    }
}
