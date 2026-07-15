using System.Collections.Generic;
using My.Dialog;
using My.Map;
using My.Map.Logic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 大地图 NPC 和平交互入口：选项由 Catalog 提供，不再走 Dialog 动态拼菜单
    public sealed class NpcInteractHubPanel : PanelWithInput
    {
        public const string Pid = "NpcInteractHubPanel";

        public sealed class Payload
        {
            public long NpcEntityId;
        }

        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text titleText;
        [SerializeField] RectTransform optionRoot;
        [SerializeField] Button optionTemplate;

        long _npcEntityId;
        readonly List<Button> _spawned = new();

        public static void Open(long npcEntityId)
        {
            if (UIManager.Instance == null || npcEntityId == 0)
            {
                return;
            }

            UIManager.Instance.ShowPanel(Pid, new Payload { NpcEntityId = npcEntityId });
        }

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
            closeButton?.onClick.AddListener(CloseSelf);
            if (optionTemplate != null)
            {
                optionTemplate.gameObject.SetActive(false);
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (data is Payload payload)
            {
                _npcEntityId = payload.NpcEntityId;
            }
        }

        public override void Show()
        {
            base.Show();
            Refresh();
        }

        public override bool OnCancel()
        {
            CloseSelf();
            return true;
        }

        void CloseSelf()
        {
            UIManager.Instance.HidePanel(Pid);
        }

        void Refresh()
        {
            ClearSpawned();
            var npc = ResolveNpc();
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (npc == null || glm == null || optionRoot == null || optionTemplate == null)
            {
                if (titleText != null)
                {
                    titleText.text = "交互";
                }

                return;
            }

            if (titleText != null)
            {
                var name = !string.IsNullOrEmpty(npc.WorldMapLandmarkLabel)
                    ? npc.WorldMapLandmarkLabel
                    : "交互";
                titleText.text = name;
            }

            var options = NpcInteractHubCatalog.Build(npc, glm);
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                var btn = Instantiate(optionTemplate, optionRoot);
                btn.gameObject.SetActive(true);
                btn.name = "Opt_" + i;
                var label = btn.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = opt.DisplayText ?? string.Empty;
                }

                var captured = opt;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnClickOption(captured));
                _spawned.Add(btn);
            }
        }

        void OnClickOption(NpcInteractHubOption option)
        {
            if (option == null)
            {
                return;
            }

            var npcId = _npcEntityId;
            switch (option.Kind)
            {
                case NpcInteractHubOptionKind.TalkPeace:
                case NpcInteractHubOptionKind.Seduce:
                    CloseSelf();
                    if (!string.IsNullOrEmpty(option.TargetDialogId))
                    {
                        MainGameManager.Instance?.PlayDialog(option.TargetDialogId, npcId);
                    }
                    break;

                case NpcInteractHubOptionKind.Quest:
                    CloseSelf();
                    if (option.QuestSession != null)
                    {
                        QuestDialogFlowRunner.Start(option.QuestSession, npcId);
                    }
                    break;

                case NpcInteractHubOptionKind.Shop:
                {
                    var npc = ResolveNpc();
                    var shop = npc != null
                        ? MainGameManager.Instance?.gameLogicManager?.shopDataManager?.GetShopProviderByNpcId(npc.CfgId)
                        : null;
                    if (shop != null)
                    {
                        CloseSelf();
                        UIOrchestrator.Instance?.ShowShop(shop);
                    }
                    break;
                }
            }
        }

        NpcUnitLogicEntity ResolveNpc()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || _npcEntityId == 0)
            {
                return null;
            }

            return glm.GetLogicEntity(_npcEntityId, false) as NpcUnitLogicEntity;
        }

        void ClearSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Destroy(_spawned[i].gameObject);
                }
            }

            _spawned.Clear();
        }
    }
}
