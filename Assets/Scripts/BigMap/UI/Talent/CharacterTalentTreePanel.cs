using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Talent
{
    public sealed class CharacterTalentTreePanel : PanelWithInput
    {
        public const string Pid = "CharacterTalentTreePanel";

        public sealed class Payload
        {
            public string TreeId;
            public string CharacterKey;
        }

        [SerializeField] TalentTreeView treeView;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] Button closeButton;
        [SerializeField] Button blockerButton;

        bool _listenersBound;

        public static CharacterTalentTreePanel Open(string treeId, string characterKey)
        {
            return UIManager.Instance.ShowPanel(Pid, new Payload
            {
                TreeId = treeId,
                CharacterKey = characterKey,
            }) as CharacterTalentTreePanel;
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            Layer = UILayer.Popup;
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            BindListeners();
        }

        public override void Setup(object data = null)
        {
            var payload = data as Payload;
            var tree = !string.IsNullOrEmpty(payload?.TreeId)
                ? CfgMgr.Cfgs?.TbTalentTree?.GetOrDefault(payload.TreeId)
                : null;
            if (tree == null)
            {
                Debug.LogError($"[CharacterTalentTreePanel] Unknown tree: {payload?.TreeId}");
                CloseSelf();
                return;
            }

            if (!string.IsNullOrEmpty(tree.OwnerCharacterKey)
                && tree.OwnerCharacterKey != payload.CharacterKey)
            {
                Debug.LogError($"[CharacterTalentTreePanel] Owner mismatch: {payload.CharacterKey} -> {tree.TreeId}");
                CloseSelf();
                return;
            }

            if (titleText != null)
            {
                titleText.text = tree.DisplayName;
            }

            treeView?.Bind(tree.TreeId, ResolveProgression());
        }

        public override void Show()
        {
            base.Show();
            treeView?.Refresh();
        }

        public override bool OnCancel()
        {
            CloseSelf();
            return true;
        }

        void BindListeners()
        {
            if (_listenersBound)
            {
                return;
            }

            _listenersBound = true;
            closeButton?.onClick.AddListener(CloseSelf);
            blockerButton?.onClick.AddListener(CloseSelf);
        }

        void CloseSelf()
        {
            if (UIManager.Instance != null && UIManager.Instance.IsPanelVisible(Pid))
            {
                UIManager.Instance.HidePanel(Pid);
            }
        }

        static PlayerProgressionSystem ResolveProgression()
        {
            return MainGameManager.Instance?.gameLogicManager?.playerDataManager?.ProgressionSystem;
        }
    }
}
