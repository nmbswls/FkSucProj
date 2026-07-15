using My.Player;
using My.UI;
using UnityEngine;

namespace My.UI.CultTech
{
    public sealed class AncientSeatTreePanel : PanelBase
    {
        public const string Pid = "AncientSeatTreePanel";
        [SerializeField] AncientSeatTreeView treeView;

        public static AncientSeatTreePanel Open() => UIManager.Instance.ShowPanel(Pid) as AncientSeatTreePanel;

        void Awake()
        {
            panelId = Pid;
            treeView ??= GetComponentInChildren<AncientSeatTreeView>(true);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            treeView?.Bind(MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem?.DemonCult);
        }

        public override void Show()
        {
            base.Show();
            treeView?.Refresh();
        }
    }
}
