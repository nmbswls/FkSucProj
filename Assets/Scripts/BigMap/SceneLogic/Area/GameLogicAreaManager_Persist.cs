using My.Map;
using My.Saving;

namespace My.Map.Logic
{
    public partial class GameLogicAreaManager
    {
        public MapRuntimePersistData BuildMapRuntimePersistData()
        {
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

                if (rec.MarkDestroyed)
                {
                    continue;
                }

                if (runtimeStates.TryGetValue(rec.Id, out var st) && st != null && st.IsMarkDestroy)
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

                    if (rec.MarkDestroyed)
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
