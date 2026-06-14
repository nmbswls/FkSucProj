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

        public event Action<string, int, int> EventOnResourceChanged;
        public event Action EventOnPeriodicCast;

        BaseUnitLogicEntity _owner;
        float _nextCastTime;
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

        protected override bool ShouldTearDownBuffsOnDestroy => true;

        protected override void LoadCfg()
        {
            Cfg = SkillProxySpecRuntimeMap.Get(CfgId);
        }

        public override void Initialize()
        {
            base.Initialize();

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
            _nextCastTime = LogicTime.time;

            InitStartupBuffs();
        }

        protected override void InitAttribute()
        {
            RegisterSpecAttrs();
            attributeStore.Commit();
        }

        void RegisterSpecAttrs()
        {
            if (Cfg?.InitialAttrs == null || Cfg.InitialAttrs.Count == 0)
            {
                return;
            }

            foreach (var kv in Cfg.InitialAttrs)
            {
                if (AttrUtils.GetAttrType(kv.Key) != EAttrType.Num)
                {
                    continue;
                }

                attributeStore.RegisterNumeric(kv.Key, initialBase: kv.Value);
            }

            foreach (var kv in Cfg.InitialAttrs)
            {
                if (AttrUtils.GetAttrType(kv.Key) != EAttrType.Resource)
                {
                    continue;
                }

                attributeStore.RegisterResource(
                    kv.Key,
                    ResolveResourceMaxAttrId(kv.Key),
                    fixMaxValue: null,
                    initialCurrent: kv.Value,
                    policy: MaxChangePolicy.ClampOnly);
            }
        }

        static string ResolveResourceMaxAttrId(string resourceId)
        {
            if (resourceId == AttrIdConsts.Ammo)
            {
                return AttrIdConsts.AmmoMax;
            }

            return null;
        }

        public override void OnResourceAttriChanged(string attrId, long before, long after, ResourceDeltaIntent intent)
        {
            base.OnResourceAttriChanged(attrId, before, after, intent);

            if (attrId != AttrIdConsts.Ammo)
            {
                return;
            }

            int max = (int)GetResourceMax(AttrIdConsts.Ammo);
            EventOnResourceChanged?.Invoke(attrId, (int)after, max);
        }

        void InitStartupBuffs()
        {
            if (Cfg.SelfBuffIds != null)
            {
                foreach (var buffId in Cfg.SelfBuffIds)
                {
                    if (string.IsNullOrEmpty(buffId))
                    {
                        continue;
                    }

                    LogicManager.globalBuffManager.AddBuff(Id, buffId, casterId: OwnerEntityId);
                }
            }

            if (!string.IsNullOrEmpty(Cfg.OwnerLinkBuffId))
            {
                LogicManager.globalBuffManager.RequestAddBuff(
                    OwnerEntityId,
                    Cfg.OwnerLinkBuffId,
                    casterId: Id);
            }
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
            TickPeriodicCast();
            AbilityController?.Tick(dt);

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

            if (!string.IsNullOrEmpty(Cfg.OwnerLinkBuffId) && !_owner.CheckHasBuff(Cfg.OwnerLinkBuffId))
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
                case ESkillProxyAnchorMode.MirrorOwnerFacing:
                    SetPosition(_owner.Pos);
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
            if (castCosts == null || castCosts.Count == 0)
            {
                return;
            }

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

            // 以 proxy 自身位置为圆心搜索，owner 只用于提供阵营参考
            return EntityAbilityHelper.FindNearestEnemyInRadius(
                LogicManager,
                _owner,
                Pos,
                Cfg.CastAcquireRadius,
                _owner.Id,
                Id);
        }

        protected override void OnBeforeEntityDestroyed(string reason)
        {
            // 先停掉周期施法/被动 tick，再反注册 Buff，避免销毁过程中 Buff 仍触发
            AbilityController?.Cancel();
            base.OnBeforeEntityDestroyed(reason);
        }
    }
}
