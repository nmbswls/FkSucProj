using UnityEngine;

namespace My
{
    // 订阅 MapSceneEffectCtrl progress 事件的基类
    public abstract class SceneEffectProgressConsumerBase : MonoBehaviour
    {
        protected MapSceneEffectCtrl EffectCtrl { get; private set; }

        protected virtual void Awake()
        {
            EffectCtrl = GetComponent<MapSceneEffectCtrl>();
            if (EffectCtrl == null)
            {
                EffectCtrl = GetComponentInParent<MapSceneEffectCtrl>();
            }
        }

        protected virtual void OnEnable()
        {
            if (EffectCtrl == null)
            {
                return;
            }

            EffectCtrl.OnShown += HandleShown;
            EffectCtrl.OnProgressChanged += HandleProgressChanged;
        }

        protected virtual void OnDisable()
        {
            if (EffectCtrl == null)
            {
                return;
            }

            EffectCtrl.OnShown -= HandleShown;
            EffectCtrl.OnProgressChanged -= HandleProgressChanged;
        }

        void HandleShown()
        {
            OnEffectShown();
        }

        void HandleProgressChanged(float progress01)
        {
            OnProgressChanged(progress01);
        }

        protected abstract void OnEffectShown();
        protected abstract void OnProgressChanged(float progress01);
    }
}
