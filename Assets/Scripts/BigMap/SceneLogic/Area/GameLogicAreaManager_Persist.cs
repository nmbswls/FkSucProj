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
                d.RefreshStates.Add(new RefreshRuntimePersist
                {
                    StaticId = kv.Key,
                    EntityInstId = rt.EntityInstId,
                    LastRespawnTime = rt.LastRespawnTime,
                    LastDestroyTime = rt.LastDestroyTime,
                });
            }

            foreach (var kv in Record2RefreshInfo)
            {
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
                RefreshInfoRuntimes[r.StaticId] = new SceneRefreshInfoRuntime
                {
                    EntityInstId = r.EntityInstId,
                    LastRespawnTime = r.LastRespawnTime,
                    LastDestroyTime = r.LastDestroyTime,
                };
            }

            Record2RefreshInfo.Clear();
            foreach (var kv in data.RecordToRefreshStaticId)
            {
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

                    RegisterEntityRecord(rec, isCreate: false);
                }
            }

            BuildIndexFromRecords();
        }
    }
}
