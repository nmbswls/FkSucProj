using System;
using My.Map.Hunting;
using UnityEngine;

namespace My.UI
{
    // 狩猎模式专用 HUD：悬浮 NPC 详情、行动轮盘、欲望结晶标记等。
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

        // 猎杀模式切换时广播，供结晶标记等订阅。
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
                actionRadial?.Close();
            }

            HunterModeChanged?.Invoke(on);
        }

        public override void Show()
        {
            base.Show();
            HunterModeChanged?.Invoke(IsHunterMode);
        }

        public override void Hide()
        {
            actionRadial?.Close();
            HuntingModeManager.Instance?.Exit();
            base.Hide();
        }
    }
}
