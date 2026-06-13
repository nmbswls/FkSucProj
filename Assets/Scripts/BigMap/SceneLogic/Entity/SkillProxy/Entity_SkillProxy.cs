using System;
using My;
using My.Map.Logic;
using UnityEngine;

namespace My.Map.Entity
{
    public class SkillProxyLogicEntity : LogicEntityBase
    {
        public SkillProxySpec Cfg { get; private set; }
        public long OwnerEntityId { get; private set; }
        public SkillProxyAbilityExecutor AbilityController { get; private set; }

        public event Action<int, int> EventOnAmmoResourceChanged;
        public event Action EventOnPeriodicCast;

        BaseUnitLogicEntity _owner;
        float _nextCastTime;
        float _orbitAngle;
        Vector2 _fixedWorldPos;
        bool _fixedWorldCaptured;

        public SkillProxyLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
            if (bindingRecord is LogicEntityRecord4SkillProxy spRec)
            {
                OwnerEntityId = spRec.OwnerEntityId;
            }
        }

        public override EEntityType Type => EEntityType.SkillProxy;

        public override void Initialize()
        {
            base.Initialize();

            Cfg = SkillProxySpecRuntimeMap.Get(CfgId);
            if (Cfg == null)
            {
                Debug.LogError($"SkillProxyLogicEntity spec missing: {CfgId}");
                return;
            }

            if (LifeTime <= 0f && Cfg.DefaultLifetime > 0f)
            {
                LifeTime = Cfg.DefaultLifetime;
            }

            _owner = LogicManager.GetLogicEntity(OwnerEntityId, false) as BaseUnitLogicEntity;
            if (_owner == null)
            {
                Debug.LogError($"SkillProxyLogicEntity owner missing: {OwnerEntityId}");
                return;
            }

            AbilityController = new SkillProxyAbilityExecutor(this, _owner);
            _orbitAngle = Cfg.OrbitInitialAngle * Mathf.Deg2Rad;
            _nextCastTime = LogicTime.time;

            InitAmmoResource();
            InitStartupBuffs();
        }

        void InitAmmoResource()
        {
            attributeStore.RegisterResource(
                Cfg.AmmoResourceId,
                maxAttrId: null,
                fixMaxValue: Cfg.MaxAmmo,
                initialCurrent: Cfg.MaxAmmo);

            attributeStore.EvOnResourceAttrChanged += OnAmmoResourceChanged;
            attributeStore.Commit();
        }

        void InitStartupBuffs()
        {
            if (!string.IsNullOrEmpty(Cfg.OrbRegenBuffId))
            {
                LogicManager.globalBuffManager.AddBuff(Id, Cfg.OrbRegenBuffId, casterId: OwnerEntityId);
            }

            if (!string.IsNullOrEmpty(Cfg.OwnerLinkMarkBuffId))
            {
                LogicManager.globalBuffManager.RequestAddBuff(
                    OwnerEntityId,
                    Cfg.OwnerLinkMarkBuffId,
                    casterId: Id);
            }
        }

        void OnAmmoResourceChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            if (attrId != Cfg.AmmoResourceId)
            {
                return;
            }

            EventOnAmmoResourceChanged?.Invoke((int)after, Cfg.MaxAmmo);
        }

        protected override void OnTick(float dt)
        {
            if (MarkDestroyed)
            {
                base.OnTick(dt);
                return;
            }

            if (!TickLifecycle())
            {
                base.OnTick(dt);
                return;
            }

            ApplyAnchor(dt);
            AbilityController?.Tick(dt);
            TickPeriodicCast();

            base.OnTick(dt);
        }

        bool TickLifecycle()
        {
            _owner = LogicManager.GetLogicEntity(OwnerEntityId, false) as BaseUnitLogicEntity;
            if (_owner == null || _owner.MarkDestroyed)
            {
                DoEntityDestroyed("owner_gone");
                return false;
            }

            if (!string.IsNullOrEmpty(Cfg.OwnerLinkMarkBuffId) && !_owner.CheckHasBuff(Cfg.OwnerLinkMarkBuffId))
            {
                DoEntityDestroyed("link_mark_lost");
                return false;
            }

            return true;
        }

        void ApplyAnchor(float dt)
        {
            if (_owner == null || Cfg == null)
            {
                return;
            }

            switch (Cfg.AnchorMode)
            {
                case ESkillProxyAnchorMode.FollowOwner:
                    SetPosition(_owner.Pos + Cfg.AnchorOffset);
                    break;
                case ESkillProxyAnchorMode.MirrorOwnerFacing:
                    SetPosition(_owner.Pos + Cfg.AnchorOffset);
                    break;
                case ESkillProxyAnchorMode.OrbitOwner:
                    {
                        _orbitAngle += Cfg.OrbitAngularSpeed * dt;
                        var offset = new Vector2(
                            Mathf.Cos(_orbitAngle) * Cfg.OrbitRadius,
                            Mathf.Sin(_orbitAngle) * Cfg.OrbitRadius);
                        SetPosition(_owner.Pos + offset + Cfg.AnchorOffset);
                    }
                    break;
                case ESkillProxyAnchorMode.FixedWorld:
                    {
                        if (!_fixedWorldCaptured)
                        {
                            _fixedWorldPos = Pos;
                            _fixedWorldCaptured = true;
                        }

                        SetPosition(_fixedWorldPos);
                    }
                    break;
            }
        }

        void TickPeriodicCast()
        {
            if (Cfg == null || AbilityController == null || string.IsNullOrEmpty(Cfg.PeriodicAbilityId))
            {
                return;
            }

            if (LogicTime.time < _nextCastTime)
            {
                return;
            }

            if (!AbilityController.IsActionable())
            {
                return;
            }

            var target = FindNearestEnemyInCastRange();
            if (target == null)
            {
                return;
            }

            var abilityCfg = AbilityLibrary.GetAbilityConfig(Cfg.PeriodicAbilityId);
            var castCosts = SkillCostUtil.ResolveAbilityCastCosts(abilityCfg);
            if (!SkillCostUtil.CanPay(this, castCosts))
            {
                return;
            }

            if (AbilityController.TryUseAbility(
                    Cfg.PeriodicAbilityId,
                    castVec: target.Pos,
                    target: target))
            {
                SkillCostUtil.Pay(this, OwnerEntityId, castCosts);
                _nextCastTime = LogicTime.time + Cfg.CastInterval;
                EventOnPeriodicCast?.Invoke();
            }
        }

        BaseUnitLogicEntity FindNearestEnemyInCastRange()
        {
            if (_owner == null || Cfg == null)
            {
                return null;
            }

            float radius = Cfg.CastAcquireRadius > 0.01f ? Cfg.CastAcquireRadius : 8f;
            BaseUnitLogicEntity best = null;
            float bestSqr = float.MaxValue;

            foreach (var one in _owner.FindEntityInRange(Pos, radius))
            {
                if (one is not BaseUnitLogicEntity unit)
                {
                    continue;
                }

                if (unit.Id == _owner.Id || unit.Id == Id)
                {
                    continue;
                }

                if (unit.FactionId == _owner.FactionId)
                {
                    continue;
                }

                if (unit.MarkDestroyed || unit.IsDead)
                {
                    continue;
                }

                float sqr = (unit.Pos - Pos).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = unit;
                }
            }

            return best;
        }

        public override void DoEntityDestroyed(string reason)
        {
            if (_owner != null && !string.IsNullOrEmpty(Cfg?.OwnerLinkMarkBuffId))
            {
                LogicManager.globalBuffManager.RemoveAllBuffById(
                    _owner.Id,
                    Cfg.OwnerLinkMarkBuffId,
                    casterId: Id);
            }

            base.DoEntityDestroyed(reason);
        }

        public override void OnDespawn(ref LogicEntityRecord? snapshot)
        {
            if (attributeStore != null)
            {
                attributeStore.EvOnResourceAttrChanged -= OnAmmoResourceChanged;
            }

            base.OnDespawn(ref snapshot);
        }
    }
}
