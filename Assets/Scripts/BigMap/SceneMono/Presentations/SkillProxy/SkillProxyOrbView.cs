using My.Map.Entity;
using UnityEngine;

namespace My.Map.Scene
{
    // orb 专属表现组件，与 SkillProxyPresenter 平级挂在同一根 GameObject。
    // 订阅 Presenter 暴露的事件，自行管理子节点的弹药槽显示，不影响 Presenter 逻辑。
    [RequireComponent(typeof(SkillProxyPresenter))]
    public class SkillProxyOrbView : MonoBehaviour
    {
        [SerializeField] private SkillProxyOrbSlotView[] orbSlots;

        SkillProxyPresenter _presenter;

        void Awake()
        {
            _presenter = GetComponent<SkillProxyPresenter>();

            if (orbSlots == null || orbSlots.Length == 0)
            {
                orbSlots = GetComponentsInChildren<SkillProxyOrbSlotView>(true);
            }
        }

        void OnEnable()
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.EvBound += OnBound;
            _presenter.EvUnbound += OnUnbound;
            _presenter.EvAmmoChanged += OnAmmoChanged;
            _presenter.EvPeriodicCast += OnPeriodicCast;
        }

        void OnDisable()
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.EvBound -= OnBound;
            _presenter.EvUnbound -= OnUnbound;
            _presenter.EvAmmoChanged -= OnAmmoChanged;
            _presenter.EvPeriodicCast -= OnPeriodicCast;
        }

        void OnBound(SkillProxyLogicEntity logic)
        {
            int current = (int)logic.GetAttr(logic.Cfg.AmmoResourceId);
            RefreshSlots(current);
        }

        void OnUnbound()
        {
            RefreshSlots(0);
        }

        void OnAmmoChanged(int current, int max)
        {
            RefreshSlots(current);
        }

        void OnPeriodicCast() { }

        void RefreshSlots(int current)
        {
            if (orbSlots == null)
            {
                return;
            }

            for (int i = 0; i < orbSlots.Length; i++)
            {
                orbSlots[i]?.SetActive(i < current);
            }
        }
    }
}
