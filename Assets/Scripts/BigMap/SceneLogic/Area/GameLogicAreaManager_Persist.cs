using My.Map;
using My.Saving;
using UnityEngine;

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

            SyncCorpsePendingIntoRecords();
        }

        private void SyncCorpsePendingIntoRecords()
        {
            foreach (var kv in runtimeStates)
            {
                var st = kv.Value;
                if (!st.IsMarkDestroy)
                {
                    continue;
                }

                if (!Repo.Records.TryGetValue(st.Id, out var rec))
                {
                    continue;
                }

                if (Repo.IsLoaded(st.Id) && Repo.GetLoaded(st.Id) is LogicEntityBase leb)
                {
                    leb.SyncRecordForPersistence();
                }
                else
                {
                    ApplyCorpseRemainFromRuntime(st.Id, rec);
                }
            }
        }

        public void ApplyCorpseRemainFromRuntime(long id, LogicEntityRecord rec)
        {
            if (rec == null)
            {
                return;
            }

            if (runtimeStates.TryGetValue(id, out var st) && st.IsMarkDestroy)
            {
                rec.CorpseCleanupRemainTime = Mathf.Max(0f, st.DeathRemainTimer);
            }
            else
            {
                rec.CorpseCleanupRemainTime = 0f;
            }
        }

        private void RestoreCorpseRuntimeFromRecord(LogicEntityRecord rec)
        {
            if (rec == null || rec.CorpseCleanupRemainTime <= 0f)
            {
                return;
            }

            if (!Repo.HasRecord(rec.Id))
            {
                return;
            }

            if (!runtimeStates.TryGetValue(rec.Id, out var st))
            {
                st = new OneEntityRuntimeState { Id = rec.Id, State = LogicLifeState.NotLoaded };
            }

            st.IsMarkDestroy = true;
            st.DeathRemainTimer = rec.CorpseCleanupRemainTime;
            runtimeStates[rec.Id] = st;

            corpseCleanupQ.Enqueue((rec.Id, "persist_restore"));
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

                if (rec.MarkDestroyed && rec.CorpseCleanupRemainTime <= 0f)
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

                    if (rec.MarkDestroyed && rec.CorpseCleanupRemainTime <= 0f)
                    {
                        continue;
                    }

                    RegisterEntityRecord(rec, isCreate: false);
                }

                foreach (var rec in data.EntityRecords)
                {
                    if (rec == null || rec.EntityType == EEntityType.Player)
                    {
                        continue;
                    }

                    if (rec.CorpseCleanupRemainTime > 0f)
                    {
                        RestoreCorpseRuntimeFromRecord(rec);
                    }
                }
            }

            BuildIndexFromRecords();
        }
    }
}
