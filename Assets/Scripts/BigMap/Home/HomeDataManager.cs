using System;
using System.Collections.Generic;
using My.Config;
using My.Map;
using My.Map.Logic;
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

        public TownFacilityPersist GetTownFacility(string townId, long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(townId, instanceId, facilityId);
        public int GetFacilityDevelopmentLevel(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.DevelopmentLevel ?? 0;
        public bool IsFacilityConstructed(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.IsConstructed == true;
        public string GetFacilityOperationPlan(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.OperationPlanId;

        public bool TrySetFacilityOperationPlan(long instanceId, string facilityId, string operationPlanId, out string failReason)
        {
            failReason = null;
            var development = GetTownFacility(CurrentTownId, instanceId, facilityId);
            if (development == null || !development.IsConstructed)
            {
                failReason = "facility_not_constructed";
                return false;
            }
            SetFacilityDevelopment(instanceId, facilityId, development.DevelopmentLevel, operationPlanId, development.AssignedWorkforce);
            return true;
        }

        public int GetFacilityWorkforce(long instanceId, string facilityId) => LogicManager?.worldPersistState?.GetTownFacility(CurrentTownId, instanceId, facilityId)?.AssignedWorkforce ?? 0;

        public void SetFacilityDevelopment(long instanceId, string facilityId, int level, string operationPlanId, int assignedWorkforce)
        {
            if (string.IsNullOrEmpty(CurrentTownId) || string.IsNullOrEmpty(facilityId)) return;
            LogicManager?.worldPersistState?.SetTownFacilityDevelopment(CurrentTownId, instanceId, facilityId, level, operationPlanId, assignedWorkforce, level > 0);
            EvOnTownEcoChanged?.Invoke();
            EvOnFacilityUpdate?.Invoke();
        }

        public void SetTownProsperity(int value) { value = Mathf.Max(0, value); if (TownProsperity == value) return; TownProsperity = value; EvOnTownEcoChanged?.Invoke(); }
        public void SetTownCurrentPopulation(int value) { value = Mathf.Max(0, value); if (TownCurrentPopulation == value) return; TownCurrentPopulation = value; EvOnTownEcoChanged?.Invoke(); }

        public FacilityDefinition GetFacilityDefinition(FixedFacilityInfo facility) => facility?.Definition ?? FacilityDefinitionCatalog.Get(facility?.FacilityId);
        public bool SupportsFacilityWorkforce(FixedFacilityInfo facility) => GetFacilityDefinition(facility)?.Capabilities.HasFlag(FacilityCapability.Workforce) == true;
        public int GetFacilityWorkforceCapacity(FixedFacilityInfo facility) => Mathf.Max(0, GetFacilityDefinition(facility)?.MaxWorkforce ?? 0);

        public int ComputeAssignedWorkforceTotal()
        {
            RefreshFixedFacilities();
            int sum = 0;
            foreach (var facility in FixedFacilities)
            {
                if (facility.Removed || !SupportsFacilityWorkforce(facility)) continue;
                sum += GetFacilityWorkforce(facility.InstanceId, facility.FacilityId);
            }
            return sum;
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
            if (!SupportsFacilityWorkforce(facility))
            {
                failReason = "facility_has_no_workforce";
                return false;
            }
            if (!IsFacilityConstructed(facility.InstanceId, facility.FacilityId))
            {
                failReason = "facility_not_constructed";
                return false;
            }
            workers = Mathf.Clamp(workers, 0, GetFacilityWorkforceCapacity(facility));
            var development = GetTownFacility(CurrentTownId, facility.InstanceId, facility.FacilityId);
            if (development == null)
            {
                failReason = "facility_development_missing";
                return false;
            }
            SetFacilityDevelopment(facility.InstanceId, facility.FacilityId, development.DevelopmentLevel, development.OperationPlanId, workers);
            return true;
        }

        public void OnPlayerEnterHome() { }
        public void EnsureHomeFacilityRecordsRegistered() => RefreshFixedFacilities();
        public void DoRepairFacility(string facilityId, Vector2 repairPos) { }
        public bool CheckHasFacility(string facilityId) { RefreshFixedFacilities(); return FixedFacilities.Exists(item => item.FacilityId == facilityId && !item.Removed); }
        public void RefreshProduceValue() { }
        public void DoDayEndBalance() { DailyNormalHTimes = 0; }
    }
}
