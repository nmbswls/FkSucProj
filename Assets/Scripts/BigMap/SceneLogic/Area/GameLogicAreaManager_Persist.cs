using My.Map;
using My.Saving;

namespace My.Map.Logic
{
    public partial class GameLogicAreaManager
    {
        public void PrepareForPersistenceSnapshot()
        {
            if (Repo == null)
            {
                return;
            }

            while (despawnEntityQ.Count > 0)
            {
                var id = despawnEntityQ.Dequeue();
                DespawnEntity(id);
            }

            while (spawnEntityQ.Count > 0)
            {
                var id = spawnEntityQ.Dequeue();
                SpawnEntity(id);
            }

            while (sleepEntityQ.Count > 0)
            {
                var id = sleepEntityQ.Dequeue();
                var ent = Repo.GetLoaded(id);
                ent?.OnSleep();
            }

            while (wakeEntityQ.Count > 0)
            {
                var id = wakeEntityQ.Dequeue();
                var ent = Repo.GetLoaded(id);
                ent?.OnWake();
            }

            SyncLoadedCorpseRecordsForPersistence();
        }

        private void SyncLoadedCorpseRecordsForPersistence()
        {
            foreach (var kv in runtimeStates)
            {
                if (!kv.Value.IsMarkDestroy)
                {
                    continue;
                }

                if (!Repo.IsLoaded(kv.Key))
                {
                    continue;
                }

                if (Repo.GetLoaded(kv.Key) is LogicEntityBase leb)
                {
                    leb.SyncRecordForPersistence();
                }
            }
        }

        public MapRuntimePersistData BuildMapRuntimePersistData()
        {
            PrepareForPersistenceSnapshot();

            var d = new MapRuntimePersistData { AreaAlertValue = AreaAlertValue };

            foreach (var kv in RefreshInfoRuntimes)
            {
                var rt = kv.Value;
                EnsureLinkedRefreshOnRuntime(kv.Key, rt);
                var entityInstIdPersist = rt.EntityInstId;
                if (IsSavePointRefreshRuntime(kv.Key, rt))
                {
                    entityInstIdPersist = 0;
                }

                d.RefreshStates.Add(new RefreshRuntimePersist
                {
                    StaticId = kv.Key,
                    EntityInstId = entityInstIdPersist,
                    LastRespawnTime = rt.LastRespawnTime,
                    LastDestroyTime = rt.LastDestroyTime,
                    LastRemovalReason = (int)rt.LastRemovalReason,
                });
            }

            foreach (var kv in Record2RefreshInfo)
            {
                if (Repo.Records.TryGetValue(kv.Key, out var linkRec) &&
                    linkRec.EntityType == EEntityType.SavePoint)
                {
                    continue;
                }

                if (TryGetRefreshInfoByStaticId(kv.Value, out var refreshDef) &&
                    IsSavePointRefreshInfo(refreshDef))
                {
                    continue;
                }

                d.RecordToRefreshStaticId[kv.Key] = kv.Value;
            }

            if (Repo?.Records == null)
            {
                return d;
            }

            foreach (var kv in Repo.Records)
            {
                var rec = kv.Value;
                if (rec.EntityType == EEntityType.Player)
                {
                    continue;
                }

                if (rec.EntityType == EEntityType.SavePoint)
                {
                    continue;
                }

                if (rec.MarkDestroyed)
                {
                    continue;
                }

                d.EntityRecords.Add(rec);
            }

            return d;
        }

        public void ApplyMapRuntimePersistData(MapRuntimePersistData data)
        {
            if (data == null)
            {
                return;
            }

            AreaAlertValue = data.AreaAlertValue;

            RefreshInfoRuntimes.Clear();
            foreach (var r in data.RefreshStates)
            {
                var entityInstId = r.EntityInstId;
                if (TryGetRefreshInfoByStaticId(r.StaticId, out var refreshDef) &&
                    IsSavePointRefreshInfo(refreshDef))
                {
                    entityInstId = 0;
                }

                var rt = new SceneRefreshInfoRuntime
                {
                    EntityInstId = entityInstId,
                    LastRespawnTime = r.LastRespawnTime,
                    LastDestroyTime = r.LastDestroyTime,
                    LastRemovalReason = SanitizePersistedRemovalReason(r.LastRemovalReason),
                };
                EnsureLinkedRefreshOnRuntime(r.StaticId, rt);
                RefreshInfoRuntimes[r.StaticId] = rt;
            }

            Record2RefreshInfo.Clear();
            foreach (var kv in data.RecordToRefreshStaticId)
            {
                if (TryGetRefreshInfoByStaticId(kv.Value, out var refreshDef) &&
                    IsSavePointRefreshInfo(refreshDef))
                {
                    continue;
                }

                Record2RefreshInfo[kv.Key] = kv.Value;
            }

            if (data.EntityRecords != null && data.EntityRecords.Count > 0)
            {
                foreach (var rec in data.EntityRecords)
                {
                    if (rec == null)
                    {
                        continue;
                    }

                    if (rec.EntityType == EEntityType.Player)
                    {
                        continue;
                    }

                    if (rec.EntityType == EEntityType.SavePoint)
                    {
                        continue;
                    }

                    RegisterEntityRecord(rec, isCreate: false);
                }
            }

            BuildIndexFromRecords();
        }
    }
}
