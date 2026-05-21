using My.MiniGame.Dream;
using My.UI;
using UnityEngine;

namespace My.SecretBase
{
    // 场景交互点：挂 Collider2D + 填 panelId；无碰撞体时用 SpriteRenderer.bounds。
    public class SecretBaseInteractable : SecretBaseClickTargetBase
    {
        // 与 demo_tbsecretbasefacility.json 中 dream 设施的 panel_id 一致，表示直达入梦而非打开 UI 面板
        public const string DreamFacilityPanelId = "UIBedroomDream";

        string _panelId;

        public void Setup(string panelId, int sortOrder)
        {
            _panelId = panelId;
            CacheRefs();
            ApplySortOrder(sortOrder);
        }

        void Awake()
        {
            CacheRefs();
        }

        public override void OnClick()
        {
            OpenPanel();
        }

        public void OpenPanel()
        {
            if (string.IsNullOrEmpty(_panelId))
            {
                return;
            }

            if (_panelId == DreamFacilityPanelId)
            {
                DreamInfiltrationBootstrap.OpenEntry();
                return;
            }

            UIManager.Instance.ShowPanel(_panelId);
        }
    }
}
