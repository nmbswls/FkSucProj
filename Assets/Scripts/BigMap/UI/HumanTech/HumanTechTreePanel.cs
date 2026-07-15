using My.Player;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.HumanTech
{
    public sealed class HumanTechTreePanel : PanelBase
    {
        public const string Pid = "HumanTechTreePanel";

        public static HumanTechTreePanel Open()
        {
            return UIManager.Instance.ShowPanel(Pid) as HumanTechTreePanel;
        }

        [SerializeField] Button closeButton;
        [SerializeField] HumanTechTreeView treeView;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            if (treeView == null)
            {
                treeView = GetComponentInChildren<HumanTechTreeView>(true);
            }

            EnsureCloseButton();
        }

        void EnsureCloseButton()
        {
            if (closeButton == null)
            {
                var existing = transform.Find("CloseButton");
                if (existing != null) closeButton = existing.GetComponent<Button>();
            }

            if (closeButton == null)
            {
                Debug.LogError("HumanTechTreePanel requires CloseButton in its prefab.");
                return;
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Hide);
        }
        public override void Setup(object data = null)
        {
            base.Setup(data);
            treeView?.Bind(ResolveHumanCivilization());
        }

        public override void Show()
        {
            base.Show();
            treeView?.Refresh();
        }

        static HumanCivilizationSystem ResolveHumanCivilization()
        {
            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            return glm?.playerDataManager?.ProgressionSystem?.HumanCivilization;
        }
    }
}
