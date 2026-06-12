using System;
using My;
using My.Map;
using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    public class SkillProxyPresenter : ScenePresentationBase<SkillProxyLogicEntity>
    {
        [SerializeField] private Transform orbitRoot;
        [SerializeField] private SkillProxyOrbSlotView[] orbSlots;

        ISkillProxyViewComponent[] _viewComponents;

        protected override void Awake()
        {
            base.Awake();
            _viewComponents = GetComponentsInChildren<ISkillProxyViewComponent>(true);
            if (orbSlots == null || orbSlots.Length == 0)
            {
                orbSlots = GetComponentsInChildren<SkillProxyOrbSlotView>(true);
            }
        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);
            if (_logic == null)
            {
                return;
            }

            _logic.EventOnAmmoResourceChanged += OnAmmoChanged;
            _logic.EventOnPeriodicCast += OnPeriodicCast;

            foreach (var comp in _viewComponents)
            {
                comp?.OnSkillProxyBind(_logic, this);
            }

            RefreshOrbSlots(_logic.GetAttr(_logic.Cfg.AmmoResourceId), _logic.Cfg.MaxAmmo);
        }

        public override void Unbind()
        {
            if (_logic != null)
            {
                _logic.EventOnAmmoResourceChanged -= OnAmmoChanged;
                _logic.EventOnPeriodicCast -= OnPeriodicCast;
            }

            foreach (var comp in _viewComponents)
            {
                comp?.OnSkillProxyUnbind();
            }

            base.Unbind();
        }

        void OnAmmoChanged(int current, int max)
        {
            RefreshOrbSlots(current, max);
            foreach (var comp in _viewComponents)
            {
                comp?.OnOrbAmmoChanged(current, max);
            }
        }

        void OnPeriodicCast()
        {
            foreach (var comp in _viewComponents)
            {
                comp?.OnOrbFired(-1, 0);
            }
        }

        void RefreshOrbSlots(long current, int max)
        {
            if (orbSlots == null)
            {
                return;
            }

            for (int i = 0; i < orbSlots.Length; i++)
            {
                if (orbSlots[i] == null)
                {
                    continue;
                }

                orbSlots[i].SetActiveState(i < current);
            }
        }
    }

    public interface ISkillProxyViewComponent
    {
        void OnSkillProxyBind(SkillProxyLogicEntity logic, SkillProxyPresenter host);
        void OnOrbAmmoChanged(int current, int max);
        void OnOrbFired(int slotIndex, long targetId);
        void OnSkillProxyUnbind();
    }

    public class SkillProxyOrbSlotView : MonoBehaviour, ISkillProxyViewComponent
    {
        [SerializeField] private GameObject activeVisual;
        [SerializeField] private GameObject inactiveVisual;

        public void OnSkillProxyBind(SkillProxyLogicEntity logic, SkillProxyPresenter host) { }

        public void OnOrbAmmoChanged(int current, int max) { }

        public void OnOrbFired(int slotIndex, long targetId) { }

        public void OnSkillProxyUnbind() { }

        public void SetActiveState(bool active)
        {
            if (activeVisual != null)
            {
                activeVisual.SetActive(active);
            }

            if (inactiveVisual != null)
            {
                inactiveVisual.SetActive(!active);
            }
        }
    }
}
