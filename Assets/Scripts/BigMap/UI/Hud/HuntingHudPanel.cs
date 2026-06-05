using System;
using My.Map.Hunting;
using UnityEngine;

namespace My.UI
{    /// <summary>
    /// 狩猎模式专用 HUD：悬浮 NPC 详情、欲望结晶标记等。
    /// </summary>
    public class HuntingHudPanel : PanelBase
    {
        public const string PanelIdConst = "HuntingHudPanel";

        public static HuntingHudPanel Instance
        {
            get
            {
                var panel = UIManager.Instance?.GetShowingPanel(PanelIdConst);
                return panel as HuntingHudPanel;
            }
        }

        public bool IsHunterMode { get; private set; }

        /// <summary>
        /// 猎杀模式切换时广播，供结晶标记等订阅。
        /// </summary>
        public static event Action<bool> HunterModeChanged;

        [SerializeField]
        HuntingNpcDetailView npcDetail;

        [SerializeField]
        HuntingNpcActionRadialMenu actionRadial;

        [SerializeField]
        DesireCrystalHuntingHudMarkers crystalMarkers;

        public HuntingNpcDetailView NpcDetail => npcDetail;

        public HuntingNpcActionRadialMenu ActionRadial => actionRadial;

        public void SetHunterModeState(bool on)
        {
            if (IsHunterMode == on)
            {
                return;
            }

            IsHunterMode = on;
            if (on)
            {
                npcDetail?.Clear();
            }

            HunterModeChanged?.Invoke(on);
        }

        public override void Show()
        {
            EnsureActionRadial();
            base.Show();
            HunterModeChanged?.Invoke(IsHunterMode);
        }

        void EnsureActionRadial()
        {
            if (actionRadial != null)
            {
                return;
            }

            var radialGo = new GameObject("HuntingNpcActionRadial", typeof(RectTransform));
            radialGo.transform.SetParent(transform, false);
            var rt = radialGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260f, 260f);
            rt.anchoredPosition = Vector2.zero;

            actionRadial = radialGo.AddComponent<HuntingNpcActionRadialMenu>();
            actionRadial.MenuRoot = rt;
            actionRadial.SectorContainer = rt;
            radialGo.SetActive(false);
        }

        public override void Hide()
        {
            HuntingModeManager.Instance?.Exit();
            base.Hide();
        }
    }
}
