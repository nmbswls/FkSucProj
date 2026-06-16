using System.Collections.Generic;
using My;
using My.Home;
using My.Map;
using My.Map.Entity;

namespace My.Map.Fight
{
    // 和平战斗限制 buff 的统一挂卸：全图 mark、掌控城镇、Zone 脚底扫描
    public static class PeaceCombatBuffRefresh
    {
        public const string MapPeaceBuffId = "map_peace_mark";
        public const string ControlledTownBuffId = ControlledTownPeaceRules.BuffId;
        public const string ZonePeaceBuffId = "zone_peace";

        const float ZoneExitGraceSec = 0.4f;

        static bool _eventsBound;
        static readonly Dictionary<long, float> _pendingZoneExitAt = new();
        static readonly HashSet<long> _activeZoneKeys = new();

        public static void BindRefreshEvents(GameLogicManager glm)
        {
            if (_eventsBound || glm?.worldPersistState == null)
            {
                return;
            }

            _eventsBound = true;
            glm.worldPersistState.EvOnLogicAreaHomesteadChanged += (_, __) => Refresh(glm);
        }

        public static void Refresh(GameLogicManager glm)
        {
            ClearZonePeaceState(glm);
            SyncBuff(glm, MapPeaceBuffId, ShouldHaveMapPeaceMark(glm));
            SyncBuff(glm, ControlledTownBuffId, ControlledTownPeaceRules.ShouldApply(glm));
        }

        public static void Tick(GameLogicManager glm, float dt)
        {
            if (_pendingZoneExitAt.Count == 0)
            {
                return;
            }

            var now = LogicTime.time;
            _zoneExitScratch.Clear();
            foreach (var pair in _pendingZoneExitAt)
            {
                if (now >= pair.Value)
                {
                    _zoneExitScratch.Add(pair.Key);
                }
            }

            for (var i = 0; i < _zoneExitScratch.Count; i++)
            {
                ApplyZonePeaceRemove(glm, _zoneExitScratch[i]);
                _pendingZoneExitAt.Remove(_zoneExitScratch[i]);
            }
        }

        static readonly List<long> _zoneExitScratch = new();

        public static void NotifyZonePeaceEnter(GameLogicManager glm, long playerId, long zoneSourceId)
        {
            if (glm?.globalBuffManager == null || playerId == 0 || zoneSourceId == 0)
            {
                return;
            }

            long key = MakeZoneKey(playerId, zoneSourceId);
            _pendingZoneExitAt.Remove(key);
            if (!_activeZoneKeys.Add(key))
            {
                return;
            }

            glm.globalBuffManager.RequestAddBuff(playerId, ZonePeaceBuffId, casterId: zoneSourceId);
        }

        public static void NotifyZonePeaceExit(GameLogicManager glm, long playerId, long zoneSourceId)
        {
            if (playerId == 0 || zoneSourceId == 0)
            {
                return;
            }

            long key = MakeZoneKey(playerId, zoneSourceId);
            if (!_activeZoneKeys.Contains(key))
            {
                return;
            }

            _pendingZoneExitAt[key] = LogicTime.time + ZoneExitGraceSec;
        }

        static void ApplyZonePeaceRemove(GameLogicManager glm, long key)
        {
            if (glm?.globalBuffManager == null)
            {
                return;
            }

            UnpackZoneKey(key, out var playerId, out var zoneSourceId);
            _activeZoneKeys.Remove(key);
            glm.globalBuffManager.RemoveAllBuffById(playerId, ZonePeaceBuffId, casterId: zoneSourceId);
        }

        static void ClearZonePeaceState(GameLogicManager glm)
        {
            _pendingZoneExitAt.Clear();
            _activeZoneKeys.Clear();

            var player = glm?.playerLogicEntity;
            if (player == null || glm.globalBuffManager == null)
            {
                return;
            }

            glm.globalBuffManager.RemoveAllBuffById(player.Id, ZonePeaceBuffId);
        }

        static bool ShouldHaveMapPeaceMark(GameLogicManager glm)
        {
            return glm?.AreaManager?.cacheMapOverlayCfg?.PeaceZoneMark == true;
        }

        static void SyncBuff(GameLogicManager glm, string buffId, bool shouldHave)
        {
            var player = glm?.playerLogicEntity;
            if (player == null || glm.globalBuffManager == null || string.IsNullOrEmpty(buffId))
            {
                return;
            }

            bool has = glm.globalBuffManager.CheckHasBuff(player.Id, buffId);
            if (shouldHave && !has)
            {
                glm.globalBuffManager.RequestAddBuff(player.Id, buffId);
            }
            else if (!shouldHave && has)
            {
                glm.globalBuffManager.RemoveAllBuffById(player.Id, buffId);
            }
        }

        static long MakeZoneKey(long playerId, long zoneSourceId)
        {
            unchecked
            {
                return (playerId << 32) | (zoneSourceId & 0xFFFFFFFFL);
            }
        }

        static void UnpackZoneKey(long key, out long playerId, out long zoneSourceId)
        {
            unchecked
            {
                playerId = key >> 32;
                zoneSourceId = key & 0xFFFFFFFFL;
            }
        }
    }
}
