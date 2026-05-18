using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map.Logic;
using UnityEngine;

namespace My.Map
{
    // 地图小剧情：触发器（Luban TbMapMicroPlotTrigger）+ 演出定义（TbMicroPlotDef）+ 时间轴行（TbMicroPlotTimelineEvent.plot_id）；NPC 使用 AIStateScriptedMicroPlot。
    public sealed class MapMicroPlotManager
    {
        // 与表内无关：玩家在收听点附近进入可触发（策划控制触发点数量即可）
        public const float TriggerListenRadius = 3f;

        readonly My.GameLogicManager _glm;

        sealed class TriggerEntry
        {
            public MapMicroPlotTrigger Cfg;
        }

        readonly List<TriggerEntry> _entries = new();
        MicroPlotRunner _runner;

        public MapMicroPlotManager(My.GameLogicManager glm)
        {
            _glm = glm;
        }

        public void RebuildForCurrentMap()
        {
            AbortForMapChange();
            _entries.Clear();
            var mapId = _glm.AreaManager?.MapName;
            if (string.IsNullOrEmpty(mapId) || CfgMgr.Cfgs == null)
            {
                return;
            }

            foreach (var row in CfgMgr.Cfgs.TbMapMicroPlotTrigger.DataList)
            {
                if (row == null || !row.Enabled || row.MapId != mapId)
                {
                    continue;
                }

                _entries.Add(new TriggerEntry { Cfg = row });
            }
        }

        public void AbortForMapChange()
        {
            _runner?.AbortForExternalShutdown();
            _runner = null;
        }

        private void ClearRunnerIf(MicroPlotRunner r)
        {
            if (_runner == r)
            {
                _runner = null;
            }
        }

        public void Tick(float dt)
        {
            if (_runner != null)
            {
                _runner.Tick(dt);
                return;
            }

            TryStartOne();
        }

        void TryStartOne()
        {
            var player = _glm.playerLogicEntity;
            if (player == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            var mapId = _glm.AreaManager.MapName;
            var ws = _glm.worldPersistState;
            if (ws == null || string.IsNullOrEmpty(mapId))
            {
                return;
            }

            var px = player.Pos.x;
            var py = player.Pos.y;
            TriggerEntry best = null;
            var bestPri = int.MinValue;

            foreach (var e in _entries)
            {
                var t = e.Cfg;
                if (ws.IsMicroPlotTriggerConsumed(mapId, t.Id))
                {
                    continue;
                }

                var dx = px - t.CenterX;
                var dy = py - t.CenterY;
                var r = TriggerListenRadius;
                if (dx * dx + dy * dy > r * r)
                {
                    continue;
                }

                if (t.Priority > bestPri)
                {
                    bestPri = t.Priority;
                    best = e;
                }
            }

            if (best == null)
            {
                return;
            }

            var def = CfgMgr.Cfgs.TbMicroPlotDef.GetOrDefault(best.Cfg.MicroPlotId);
            if (def == null)
            {
                Debug.LogError($"[MicroPlot] TbMicroPlotDef not found: {best.Cfg.MicroPlotId}");
                return;
            }

            var runner = new MicroPlotRunner(_glm, this, best.Cfg, def);
            if (!runner.TryStart())
            {
                return;
            }

            _runner = runner;
        }

        // 时间轴已拆到 TbMicroPlotTimelineEvent；按 plot_id 对齐 MicroPlotDef.id，忽略 def 内嵌 timeline 字段。
        static List<MicroPlotTimelineEvent> BuildTimelineForPlot(MicroPlotDef def)
        {
            var list = new List<MicroPlotTimelineEvent>();
            if (def == null || string.IsNullOrEmpty(def.Id))
            {
                return list;
            }

            var table = CfgMgr.Cfgs?.TbMicroPlotTimelineEvent;
            if (table?.DataList == null)
            {
                return list;
            }

            foreach (var row in table.DataList)
            {
                if (row == null || row.PlotId != def.Id)
                {
                    continue;
                }

                list.Add(row);
            }

            list.Sort(static (a, b) =>
            {
                int c = a.TSec.CompareTo(b.TSec);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            });
            return list;
        }

        internal static bool TryResolveNpc(
            GameLogicAreaManager area,
            My.GameLogicManager glm,
            string uniq,
            out NpcUnitLogicEntity npc)
        {
            npc = null;
            if (string.IsNullOrEmpty(uniq))
            {
                return false;
            }

            var sid = area.GetStaticIdByUniqName(uniq);
            if (sid == 0)
            {
                return false;
            }

            if (!area.RefreshInfoRuntimes.TryGetValue(sid, out var rt))
            {
                return false;
            }

            var e = glm.GetLogicEntity(rt.EntityInstId, false);
            npc = e as NpcUnitLogicEntity;
            return npc != null && !npc.MarkDestroyed && npc.AIBrain != null;
        }

        sealed class MicroPlotRunner
        {
            readonly My.GameLogicManager _glm;
            readonly MapMicroPlotManager _owner;
            readonly MapMicroPlotTrigger _trigger;
            readonly MicroPlotDef _def;
            readonly List<MicroPlotTimelineEvent> _timeline;
            readonly List<NpcUnitLogicEntity> _actors = new();

            float _elapsed;
            int _nextEventIdx;
            bool _ended;

            public MicroPlotRunner(
                My.GameLogicManager glm,
                MapMicroPlotManager owner,
                MapMicroPlotTrigger trigger,
                MicroPlotDef def)
            {
                _glm = glm;
                _owner = owner;
                _trigger = trigger;
                _def = def;
                _timeline = BuildTimelineForPlot(def);
            }

            public bool TryStart()
            {
                _actors.Clear();
                if (_def.ManagedUniqnames == null || _def.ManagedUniqnames.Count == 0)
                {
                    Debug.LogWarning($"[MicroPlot] {_def.Id}: managed_uniqnames empty");
                    return false;
                }

                var area = _glm.AreaManager;
                foreach (var uniq in _def.ManagedUniqnames)
                {
                    if (!TryResolveNpc(area, _glm, uniq, out var npc))
                    {
                        Debug.LogWarning($"[MicroPlot] {_def.Id}: npc not ready for uniq '{uniq}'");
                        return false;
                    }

                    _actors.Add(npc);
                }

                foreach (var npc in _actors)
                {
                    npc.AIBrain.ChangeState(npc.AIBrain.StateScriptedMicroPlot);
                }

                _elapsed = 0f;
                _nextEventIdx = 0;
                _ended = false;
                return true;
            }

            public void Tick(float dt)
            {
                if (_ended)
                {
                    return;
                }

                if (!CheckAllStillScripted())
                {
                    Finish(aborted: true);
                    return;
                }

                _elapsed += dt;
                if (_def.MaxDurationSec > 0f && _elapsed >= _def.MaxDurationSec)
                {
                    Finish(aborted: true);
                    return;
                }

                var timeline = _timeline;
                while (_nextEventIdx < timeline.Count && timeline[_nextEventIdx].TSec <= _elapsed)
                {
                    FireEvent(timeline[_nextEventIdx]);
                    _nextEventIdx++;
                }

                if (timeline.Count == 0 || _nextEventIdx >= timeline.Count)
                {
                    Finish(aborted: false);
                }
            }

            bool CheckAllStillScripted()
            {
                foreach (var npc in _actors)
                {
                    if (npc == null || npc.MarkDestroyed || npc.AIBrain == null)
                    {
                        return false;
                    }

                    if (npc.AIBrain.CurrentState != npc.AIBrain.StateScriptedMicroPlot)
                    {
                        return false;
                    }
                }

                return true;
            }

            void FireEvent(MicroPlotTimelineEvent ev)
            {
                switch (ev.Kind)
                {
                    case EMicroPlotEventKind.Log:
                        Debug.Log($"[MicroPlot] {_def.Id} t={ev.TSec} {ev.Text}");
                        break;
                    case EMicroPlotEventKind.NpcSpeak:
                        var npc = ResolveActor(ev.ActorIndex);
                        if (npc != null)
                        {
                            _glm.viewer?.ShowMapSpeachBubble(
                                npc.Id,
                                string.IsNullOrEmpty(ev.Text) ? "..." : ev.Text,
                                2f);
                        }

                        break;
                }
            }

            NpcUnitLogicEntity ResolveActor(int idx)
            {
                if (idx < 0 || idx >= _actors.Count)
                {
                    return null;
                }

                return _actors[idx];
            }

            public void AbortForExternalShutdown()
            {
                if (_ended)
                {
                    return;
                }

                ReleaseActorsToIdle();
                _ended = true;
                _owner.ClearRunnerIf(this);
            }

            void Finish(bool aborted)
            {
                if (_ended)
                {
                    return;
                }

                _ended = true;
                ReleaseActorsToIdle();

                var mapId = _glm.AreaManager.MapName;
                var ws = _glm.worldPersistState;
                if (ws != null && !string.IsNullOrEmpty(mapId))
                {
                    var consume =
                        _def.ConsumeOn == EMicroPlotConsumeOn.SuccessOrAbort
                        || (_def.ConsumeOn == EMicroPlotConsumeOn.SuccessOnly && !aborted);
                    if (consume)
                    {
                        ws.MarkMicroPlotConsumed(mapId, _trigger.Id);
                    }
                }

                if(!aborted)
                {
                    foreach(var kv in _def.RewardItems)
                    _glm.playerDataManager.GiveItemToPlayer(kv.Key, kv.Value);
                }
                _owner.ClearRunnerIf(this);
            }

            void ReleaseActorsToIdle()
            {
                foreach (var npc in _actors)
                {
                    if (npc == null || npc.MarkDestroyed || npc.AIBrain == null)
                    {
                        continue;
                    }

                    if (npc.AIBrain.CurrentState == npc.AIBrain.StateScriptedMicroPlot)
                    {
                        npc.AIBrain.ChangeState(npc.AIBrain.StateIdle);
                    }
                }

                _actors.Clear();
            }
        }
    }
}
