using System;
using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Map.Unit;
using My.MapExport;
using UnityEngine;

namespace My.Map.Logic
{
    public enum EBossEncounterState
    {
        Dormant,
        Engaged,
        Returning,
        Defeated,
    }

    public sealed class BossEncounterAreaRuntime : IDisposable
    {
        sealed class Encounter
        {
            public BossEncounterExportInfo Def;
            public int StaticId;
            public long EntityId;
            public EBossEncounterState State;
            public int PhaseIndex;
            public float OutsideSince = -1f;
        }

        readonly GameLogicAreaManager _area;
        readonly List<Encounter> _encounters = new();
        readonly Dictionary<long, Encounter> _byEntity = new();

        public event Action EvChanged;

        public BossEncounterAreaRuntime(GameLogicAreaManager area, MapExportDatabase database)
        {
            _area = area;
            if (database?.BossEncounters == null)
            {
                return;
            }

            foreach (var def in database.BossEncounters)
            {
                if (def == null || string.IsNullOrWhiteSpace(def.EncounterId)
                    || string.IsNullOrWhiteSpace(def.BossUniqName))
                {
                    continue;
                }

                _encounters.Add(new Encounter
                {
                    Def = def,
                    StaticId = area.GetStaticIdByUniqName(def.BossUniqName),
                    State = EBossEncounterState.Dormant,
                });
            }
        }

        public void Dispose()
        {
            _encounters.Clear();
            _byEntity.Clear();
            EvChanged = null;
        }

        public void Tick(float dt)
        {
            foreach (var encounter in _encounters)
            {
                BindEntity(encounter);
                if (encounter.EntityId == 0)
                {
                    continue;
                }

                var boss = _area.GetLogicEntiy(encounter.EntityId, false) as NpcUnitLogicEntity;
                if (boss == null || boss.MarkDestroyed)
                {
                    continue;
                }

                if (boss.IsDead || boss.GetAttr(AttrIdConsts.HP) <= 0)
                {
                    _area.Summons.ClearForSourceDestroyed(encounter.EntityId);
                    SetState(encounter, EBossEncounterState.Defeated);
                    continue;
                }

                if (encounter.State == EBossEncounterState.Returning)
                {
                    if (boss.AIBrain?.CurrentState == boss.AIBrain.StateIdle
                        && boss.GetAttr(AttrIdConsts.HP) >= boss.GetResourceMax(AttrIdConsts.HP))
                    {
                        encounter.PhaseIndex = 0;
                        SetState(encounter, EBossEncounterState.Dormant);
                    }
                    continue;
                }

                bool engaged = boss.AggroSystem?.CombatEngaged == true
                    || boss.GetAttr(AttrIdConsts.HP) < boss.GetResourceMax(AttrIdConsts.HP);
                if (engaged)
                {
                    SetState(encounter, EBossEncounterState.Engaged);
                    UpdatePhase(encounter, boss);
                    CheckBoundary(encounter, boss);
                }
            }
        }

        void BindEntity(Encounter encounter)
        {
            if (encounter.StaticId == 0)
            {
                encounter.StaticId = _area.GetStaticIdByUniqName(encounter.Def.BossUniqName);
            }
            if (encounter.StaticId == 0
                || !_area.RefreshInfoRuntimes.TryGetValue(encounter.StaticId, out var refresh)
                || refresh.EntityInstId == 0
                || refresh.EntityInstId == encounter.EntityId)
            {
                return;
            }

            if (encounter.EntityId != 0)
            {
                _byEntity.Remove(encounter.EntityId);
            }
            encounter.EntityId = refresh.EntityInstId;
            encounter.State = EBossEncounterState.Dormant;
            encounter.PhaseIndex = 0;
            encounter.OutsideSince = -1f;
            _byEntity[encounter.EntityId] = encounter;
            EvChanged?.Invoke();
        }

        void CheckBoundary(Encounter encounter, NpcUnitLogicEntity boss)
        {
            if (encounter.Def.ArenaBounds.Contains(boss.Pos))
            {
                encounter.OutsideSince = -1f;
                return;
            }

            if (encounter.OutsideSince < 0f)
            {
                encounter.OutsideSince = LogicTime.time;
                return;
            }

            if (LogicTime.time - encounter.OutsideSince < encounter.Def.OutsideGraceTime)
            {
                return;
            }

            if (boss.AIBrain != null && boss.AIBrain.TryDisengageFromCombat(
                new CombatDisengageRequest(
                    encounter.Def.EncounterId,
                    EUnitReturnReason.EncounterBoundary,
                    encounter.Def.ResetPosition,
                    encounter.Def.ReturnMoveSpeedRate,
                    encounter.Def.RecoverDuration,
                    encounter.Def.ReengageDelay,
                    encounter.Def.InvulnerableWhileReturning)))
            {
                _area.Summons.ClearForCombatReset(encounter.EntityId);
                encounter.OutsideSince = -1f;
                encounter.PhaseIndex = 0;
                SetState(encounter, EBossEncounterState.Returning);
            }
        }

        void UpdatePhase(Encounter encounter, NpcUnitLogicEntity boss)
        {
            var phases = encounter.Def.Phases;
            if (phases == null || phases.Count == 0)
            {
                return;
            }

            var max = boss.GetResourceMax(AttrIdConsts.HP);
            if (max <= 0)
            {
                return;
            }

            var ratio = Mathf.Clamp01((float)boss.GetAttr(AttrIdConsts.HP) / max);
            int next = 0;
            for (int i = 0; i < phases.Count; i++)
            {
                if (ratio <= phases[i].EnterHpRatio)
                {
                    next = i;
                }
            }
            if (next != encounter.PhaseIndex)
            {
                encounter.PhaseIndex = next;
                EvChanged?.Invoke();
            }
        }

        void SetState(Encounter encounter, EBossEncounterState state)
        {
            if (encounter.State == state)
            {
                return;
            }
            encounter.State = state;
            EvChanged?.Invoke();
        }

        public void FilterReadySkills(NpcUnitLogicEntity unit, List<SkillRuntime> skills)
        {
            if (unit == null || skills == null || !_byEntity.TryGetValue(unit.Id, out var encounter))
            {
                return;
            }
            var phases = encounter.Def.Phases;
            if (phases == null || phases.Count == 0)
            {
                return;
            }
            int phase = Mathf.Clamp(encounter.PhaseIndex, 0, phases.Count - 1);
            var allowed = phases[phase].SelectableSkills;
            if (allowed == null || allowed.Count == 0)
            {
                return;
            }
            skills.RemoveAll(skill => !allowed.Contains(skill.SkillName));
        }

        public bool IsSkillSelectable(NpcUnitLogicEntity unit, string skillId)
        {
            if (unit == null || !_byEntity.TryGetValue(unit.Id, out var encounter))
            {
                return true;
            }
            var phases = encounter.Def.Phases;
            if (phases == null || phases.Count == 0)
            {
                return true;
            }
            int phase = Mathf.Clamp(encounter.PhaseIndex, 0, phases.Count - 1);
            var allowed = phases[phase].SelectableSkills;
            return allowed == null || allowed.Count == 0 || allowed.Contains(skillId);
        }

        public bool TryGetActiveBoss(out long entityId, out string displayName,
            out EBossEncounterState state, out int phaseIndex)
        {
            foreach (var encounter in _encounters)
            {
                if (encounter.State != EBossEncounterState.Engaged
                    && encounter.State != EBossEncounterState.Returning)
                {
                    continue;
                }
                entityId = encounter.EntityId;
                displayName = encounter.Def.DisplayName;
                state = encounter.State;
                phaseIndex = encounter.PhaseIndex;
                return entityId != 0;
            }
            entityId = 0;
            displayName = string.Empty;
            state = EBossEncounterState.Dormant;
            phaseIndex = 0;
            return false;
        }

        public bool IsBossEntity(long entityId) => _byEntity.ContainsKey(entityId);
    }
}
