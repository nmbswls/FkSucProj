using UnityEngine;

namespace My.Map.Scene
{
    public abstract partial class SceneUnitPresenter
    {
        [SerializeField] bool autoBindViewHierarchy = true;

        void TryAutoBindViewHierarchy()
        {
            if (!autoBindViewHierarchy)
            {
                return;
            }

            if (ViewRoot == null)
            {
                ViewRoot = transform.Find(UnitPresentationPaths.View);
                if (ViewRoot == null)
                {
                    ViewRoot = transform.Find(UnitPresentationPaths.ViewLegacy);
                }
            }

            SyncMainViewWithViewRoot();

            if (ViewRoot == null)
            {
                return;
            }

            if (AgentView == null)
            {
                AgentView = ViewRoot.Find(UnitPresentationPaths.Agent);
            }

            // 扁平 prefab：sprite 直接挂在 view 上且无 agent 子节点
            if (AgentView == null && ViewRoot != null && ViewRoot.Find(UnitPresentationPaths.Agent) == null)
            {
                if (ViewRoot.GetComponentInChildren<SpriteRenderer>(true) != null)
                {
                    AgentView = ViewRoot;
                }
            }

            if (ShadowView == null)
            {
                ShadowView = ViewRoot.Find(UnitPresentationPaths.Shadow);
            }

            if (BindEffectRoot == null)
            {
                var bindFx = ViewRoot.Find(UnitPresentationPaths.BindEffectRoot);
                if (bindFx != null)
                {
                    BindEffectRoot = bindFx;
                }
            }

            if (WeaponCtrl == null)
            {
                var weaponRoot = ViewRoot.Find(UnitPresentationPaths.WeaponRoot);
                if (weaponRoot == null)
                {
                    weaponRoot = transform.Find(UnitPresentationPaths.WeaponRoot);
                }

                if (weaponRoot != null)
                {
                    WeaponCtrl = weaponRoot.GetComponent<MapUnitWeaponCtrl>();
                }
            }

            SyncShadowRendererForFade();
            TryAutoCollectMainSpritesIfEmpty();
        }

        void SyncMainViewWithViewRoot()
        {
            if (ViewRoot != null && MainViewRt == null)
            {
                MainViewRt = ViewRoot;
            }
            else if (MainViewRt != null && ViewRoot == null)
            {
                ViewRoot = MainViewRt;
            }
        }

        void SyncShadowRendererForFade()
        {
            if (ShadowView == null)
            {
                return;
            }

            AssignShadowViewRenderer(ShadowView.GetComponent<SpriteRenderer>());
        }

        void TryAutoCollectMainSpritesIfEmpty()
        {
            if (MainViewRt == null || (_mainSpriteArr != null && _mainSpriteArr.Length > 0))
            {
                return;
            }

            CollectMainSpritesFromView();
        }

        protected void CollectMainSpritesFromView()
        {
            if (MainViewRt == null)
            {
                return;
            }

            _mainSpriteArr = MainViewRt.GetComponentsInChildren<SpriteRenderer>(true);
        }

        protected void AssignShadowViewRenderer(SpriteRenderer shadowRenderer)
        {
            _shadowView = shadowRenderer;
        }

        static Transform FindChildByPath(Transform root, string path)
        {
            if (root == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            var current = root;
            var segments = path.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (current == null)
                {
                    return null;
                }

                current = current.Find(segments[i]);
            }

            return current;
        }
    }
}
