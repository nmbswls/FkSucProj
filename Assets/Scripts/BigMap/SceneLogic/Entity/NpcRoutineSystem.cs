using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Map;
using UnityEngine;

namespace My.Map.Logic
{
    // 城镇 NPC 日程：按 Binding→Profile→Rule 派生 MacroMoveBehave
    public sealed class NpcRoutineSystem
    {
        const float DefaultReevaluateInterval = 0.5f;

        sealed class ResolvedRule
        {
            public string Id;
            public Vector2 Anchor;
            public bool HasAnchor;
            public ENpcRoutineActivityType Activity;
            public ENpcRoutineRelocatePolicy Relocate;
            public float WanderRadius;
            public float ReevaluateIntervalSec;
            public bool HasFaceDir;
            public Vector2 FaceDir;
            public List<Vector2> PathPoints;
        }

        readonly GameLogicAreaManager _area;
        readonly Dictionary<long, string> _activeRules = new();
        readonly Dictionary<long, string> _appliedRules = new();
        readonly Dictionary<long, float> _cooldowns = new();
        readonly Dictionary<string, List<NpcRoutineRule>> _rulesByProfile = new();

        public NpcRoutineSystem(GameLogicAreaManager area)
        {
            _area = area;
            _area.EventOnNpcRefreshRecordCreated += OnNpcRefreshRecordCreated;
            RebuildRuleIndex();
        }

        public void Dispose()
        {
            _area.EventOnNpcRefreshRecordCreated -= OnNpcRefreshRecordCreated;
            ClearRuntimeState();
        }

        public void Clear()
        {
            ClearRuntimeState();
            RebuildRuleIndex();
        }

        void ClearRuntimeState()
        {
            _activeRules.Clear();
            _appliedRules.Clear();
            _cooldowns.Clear();
        }

        void RebuildRuleIndex()
        {
            _rulesByProfile.Clear();
            var table = CfgMgr.Cfgs?.TbNpcRoutineRule?.DataList;
            if (table == null) return;

            foreach (var rule in table)
            {
                if (rule == null || string.IsNullOrEmpty(rule.ProfileId)) continue;
                if (!_rulesByProfile.TryGetValue(rule.ProfileId, out var list))
                {
                    list = new List<NpcRoutineRule>();
                    _rulesByProfile[rule.ProfileId] = list;
                }

                list.Add(rule);
            }
        }

        void OnNpcRefreshRecordCreated(GameLogicAreaManager area, LogicEntityRecord4Npc record)
        {
            if (area != _area) return;
            ApplyInitialPlacement(record);
        }

        public void ApplyInitialPlacement(LogicEntityRecord4Npc record)
        {
            var resolved = Resolve(record);
            if (resolved == null) return;

            // 只调整出生点/朝向；不改 Record.MoveBehave（日程写运行时 Override）
            if (resolved.HasAnchor && resolved.Activity != ENpcRoutineActivityType.StayCurrent)
            {
                record.Position = resolved.Anchor;
            }

            if (resolved.HasFaceDir)
            {
                record.FaceDir = resolved.FaceDir;
            }

            _activeRules[record.Id] = resolved.Id;
            _appliedRules.Remove(record.Id);
        }

        public void Tick(float dt)
        {
            if (_area?.Repo?.Records == null) return;

            foreach (var rec in _area.Repo.Records.Values)
            {
                if (rec is not LogicEntityRecord4Npc npc || !HasBinding(npc)) continue;

                _cooldowns.TryGetValue(npc.Id, out var cd);
                cd -= dt;
                if (cd > 0f)
                {
                    _cooldowns[npc.Id] = cd;
                    continue;
                }

                var entity = _area.GetLogicEntiy(npc.Id, false) as NpcUnitLogicEntity;
                if (entity?.AIBrain == null)
                {
                    _cooldowns[npc.Id] = DefaultReevaluateInterval;
                    continue;
                }

                var resolved = Resolve(npc);
                if (resolved == null)
                {
                    if (_appliedRules.ContainsKey(npc.Id) || _activeRules.ContainsKey(npc.Id))
                    {
                        ClearRuntimeRoutineState(entity, refreshIdlePolicy: true);
                        _activeRules.Remove(npc.Id);
                        _appliedRules.Remove(npc.Id);
                    }

                    _cooldowns[npc.Id] = DefaultReevaluateInterval;
                    continue;
                }

                _cooldowns[npc.Id] = resolved.ReevaluateIntervalSec;
                _activeRules[npc.Id] = resolved.Id;

                // 非 Idle 不改 MoveBehave；回 Idle 时由 SyncOnEnteredIdle 立刻应用
                if (!IsInIdle(entity))
                {
                    continue;
                }

                if (IsRoutineOverrideApplied(entity, resolved.Id))
                {
                    continue;
                }

                if (ApplyRuntimeRule(entity, resolved, refreshIdlePolicy: true))
                {
                    _appliedRules[npc.Id] = resolved.Id;
                }
            }
        }

        // Idle 进入时立刻按当前条件写好 MoveBehave Override；IdlePolicy 由 OnEnter 随后构建
        public void SyncOnEnteredIdle(NpcUnitLogicEntity npc)
        {
            if (npc?.AIBrain == null) return;
            if (npc.BindingRecord is not LogicEntityRecord4Npc record || !HasBinding(record)) return;

            var resolved = Resolve(record);
            if (resolved == null)
            {
                if (_appliedRules.ContainsKey(npc.Id) || _activeRules.ContainsKey(npc.Id))
                {
                    ClearRuntimeRoutineState(npc, refreshIdlePolicy: false);
                    _activeRules.Remove(npc.Id);
                    _appliedRules.Remove(npc.Id);
                }

                return;
            }

            _activeRules[npc.Id] = resolved.Id;
            if (IsRoutineOverrideApplied(npc, resolved.Id))
            {
                return;
            }

            if (ApplyRuntimeRule(npc, resolved, refreshIdlePolicy: false))
            {
                _appliedRules[npc.Id] = resolved.Id;
            }

            _cooldowns[npc.Id] = resolved.ReevaluateIntervalSec;
        }

        bool IsRoutineOverrideApplied(NpcUnitLogicEntity npc, string ruleId)
        {
            return _appliedRules.TryGetValue(npc.Id, out var appliedId)
                && appliedId == ruleId
                && npc.MacroMoveBehaveAuthority == BaseUnitLogicEntity.EMacroBehaveAuthority.Routine;
        }

        static bool IsInIdle(NpcUnitLogicEntity npc)
        {
            var brain = npc.AIBrain;
            return brain?.CurrentState != null && brain.CurrentState == brain.StateIdle;
        }

        bool ApplyRuntimeRule(NpcUnitLogicEntity npc, ResolvedRule rule, bool refreshIdlePolicy)
        {
            var move = npc.TryAllocateMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Routine);
            if (move == null)
            {
                return false;
            }

            WriteMoveBehave(move, rule, npc.Pos);

            if (rule.Relocate == ENpcRoutineRelocatePolicy.Snap && rule.HasAnchor
                && rule.Activity != ENpcRoutineActivityType.StayCurrent)
            {
                npc.StopMove();
                npc.SetPosition(rule.Anchor);
            }

            if (rule.HasFaceDir
                && (rule.Relocate == ENpcRoutineRelocatePolicy.Snap
                    || rule.Activity == ENpcRoutineActivityType.StayCurrent))
            {
                npc.ForceSetFaceTarget(rule.FaceDir, true);
            }

            if (refreshIdlePolicy)
            {
                npc.AIBrain.RefreshIdlePolicy();
            }

            return true;
        }

        void ClearRuntimeRoutineState(NpcUnitLogicEntity npc, bool refreshIdlePolicy)
        {
            if (npc == null) return;
            npc.ClearMacroMoveBehave(BaseUnitLogicEntity.EMacroBehaveAuthority.Routine);

            if (refreshIdlePolicy && npc.AIBrain != null)
            {
                npc.AIBrain.RefreshIdlePolicy();
            }
        }

        static void WriteMoveBehave(UnitMoveBehaveInfo move, ResolvedRule rule, Vector2 currentPos)
        {
            move.HasFaceDir = rule.HasFaceDir;
            move.FaceDir = rule.FaceDir;
            move.WanderRadius = Mathf.Max(0f, rule.WanderRadius);
            move.PathLoopPoints ??= new List<Vector2>();
            move.PathLoopPoints.Clear();

            switch (rule.Activity)
            {
                case ENpcRoutineActivityType.StayCurrent:
                    move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.NoMove;
                    move.MoveToTarget = rule.HasAnchor ? rule.Anchor : currentPos;
                    break;
                case ENpcRoutineActivityType.WanderAroundPoint:
                    move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.WanderAroundPoint;
                    move.MoveToTarget = rule.Anchor;
                    if (move.WanderRadius <= 0.01f) move.WanderRadius = 1f;
                    break;
                case ENpcRoutineActivityType.PatrolPath:
                    move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.PathLoop;
                    if (rule.PathPoints != null)
                    {
                        move.PathLoopPoints.AddRange(rule.PathPoints);
                    }

                    move.MoveToTarget = move.PathLoopPoints.Count > 0 ? move.PathLoopPoints[0] : rule.Anchor;
                    break;
                default:
                    move.MoveBehaveMode = UnitMoveBehaveInfo.EMoveBehaveType.MoveToPoint;
                    move.MoveToTarget = rule.Anchor;
                    break;
            }
        }

        ResolvedRule Resolve(LogicEntityRecord4Npc record)
        {
            if (!TryGetProfile(record, out var profileId)) return null;
            var profile = CfgMgr.Cfgs.TbNpcRoutineProfile.GetOrDefault(profileId);
            if (profile == null) return null;

            NpcRoutineRule selected = null;
            if (_rulesByProfile.TryGetValue(profile.Id, out var rules))
            {
                foreach (var rule in rules)
                {
                    if (!PeriodMatches(_area.LogicManager, rule.DayPeriod)
                        || !_area.LogicManager.CheckCommonCondsAll(rule.WorldConds))
                    {
                        continue;
                    }

                    if (selected == null || rule.Priority > selected.Priority)
                    {
                        selected = rule;
                    }
                }
            }

            selected ??= CfgMgr.Cfgs.TbNpcRoutineRule.GetOrDefault(profile.FallbackRuleId);
            if (selected == null) return null;

            var activity = selected.ActivityType;
            var relocate = selected.RelocatePolicy;
            float interval = profile.ReevaluateIntervalSec > 0.05f
                ? profile.ReevaluateIntervalSec
                : DefaultReevaluateInterval;

            bool hasFace = selected.FaceDir.X * selected.FaceDir.X + selected.FaceDir.Y * selected.FaceDir.Y > 1e-8f;
            Vector2 faceDir = hasFace
                ? new Vector2(selected.FaceDir.X, selected.FaceDir.Y).normalized
                : default;

            List<Vector2> pathPoints = null;
            Vector2 anchor = default;
            bool hasAnchor = false;

            if (activity == ENpcRoutineActivityType.PatrolPath)
            {
                if (!TryResolveNamedPath(selected.TargetNamedPath, out pathPoints))
                {
                    Debug.LogWarning(
                        $"[NpcRoutine] Missing named path '{selected.TargetNamedPath}' for rule {selected.Id}.");
                    return null;
                }

                hasAnchor = true;
                anchor = pathPoints[0];
                if (!string.IsNullOrEmpty(selected.AnchorNamedPoint)
                    && TryResolveNamedPoint(selected.AnchorNamedPoint, out var pathAnchor))
                {
                    anchor = pathAnchor;
                }
            }
            else if (activity == ENpcRoutineActivityType.StayCurrent)
            {
                if (!string.IsNullOrEmpty(selected.AnchorNamedPoint)
                    && TryResolveNamedPoint(selected.AnchorNamedPoint, out anchor))
                {
                    hasAnchor = true;
                }
                else
                {
                    hasAnchor = true;
                    anchor = record.Position;
                }
            }
            else
            {
                if (!TryResolveNamedPoint(selected.AnchorNamedPoint, out anchor))
                {
                    Debug.LogWarning(
                        $"[NpcRoutine] Missing named point '{selected.AnchorNamedPoint}' for rule {selected.Id}.");
                    return null;
                }

                hasAnchor = true;
            }

            return new ResolvedRule
            {
                Id = selected.Id,
                Anchor = anchor,
                HasAnchor = hasAnchor,
                Activity = activity,
                Relocate = relocate,
                WanderRadius = selected.WanderRadius,
                ReevaluateIntervalSec = interval,
                HasFaceDir = hasFace,
                FaceDir = faceDir,
                PathPoints = pathPoints,
            };
        }

        bool TryResolveNamedPoint(string name, out Vector2 pos)
        {
            pos = default;
            if (string.IsNullOrEmpty(name)) return false;
            var named = _area.cacheDatabase?.FindNamedPointByName(name);
            if (!named.HasValue) return false;
            pos = new Vector2(named.Value.Position.x, named.Value.Position.y);
            return true;
        }

        bool TryResolveNamedPath(string pathName, out List<Vector2> points)
        {
            points = null;
            if (string.IsNullOrEmpty(pathName)) return false;
            var namedPath = _area.cacheDatabase?.FindNamedPathByName(pathName);
            if (!namedPath.HasValue || namedPath.Value.Points == null || namedPath.Value.Points.Count < 2)
            {
                return false;
            }

            points = new List<Vector2>(namedPath.Value.Points.Count);
            foreach (var pointName in namedPath.Value.Points)
            {
                if (!TryResolveNamedPoint(pointName, out var p))
                {
                    points = null;
                    return false;
                }

                points.Add(p);
            }

            return points.Count >= 2;
        }

        bool HasBinding(LogicEntityRecord4Npc record)
        {
            return TryGetProfile(record, out _);
        }

        bool TryGetProfile(LogicEntityRecord4Npc record, out string profileId)
        {
            profileId = string.Empty;
            if (record == null || string.IsNullOrEmpty(record.CharacterKey)) return false;
            var binding = CfgMgr.Cfgs.TbNpcRoutineBinding.Get(
                _area.MapChunkSceneKey,
                _area.AreaOverlayId,
                record.CharacterKey);
            if (binding == null || string.IsNullOrEmpty(binding.RoutineProfileId)) return false;
            profileId = binding.RoutineProfileId;
            return true;
        }

        static bool PeriodMatches(GameLogicManager glm, ENpcRoutineDayPeriod period)
        {
            return period == ENpcRoutineDayPeriod.Any
                || period == ENpcRoutineDayPeriod.Day && glm.DayPeriod == GameLogicManager.EDayPeriod.Day
                || period == ENpcRoutineDayPeriod.Night && glm.DayPeriod == GameLogicManager.EDayPeriod.Night;
        }
    }
}
