using System.Collections.Generic;
using My;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class GlobalWorldPanel : PanelBase, IInputConsumer
    {
        public const string Pid = "GlobalWorldPanel";

        Transform _root;
        IPlayerProgressionHubHost _progressionHubHost;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }

            layer = UILayer.Popup;
        }

        void BindRefs()
        {
            _root = transform.Find("BuiltRoot");
            if (_root == null)
            {
                return;
            }
        }

        public static GlobalWorldPanel Open()
        {
            PlayerProgressionHubPanel.OpenGear();
            var hubMono = UIManager.Instance.GetShowingPanel(PlayerProgressionHubPanel.Pid) as MonoBehaviour;
            return hubMono != null ? hubMono.GetComponentInChildren<GlobalWorldPanel>(true) : null;
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
        }

        public bool OnConfirm() => false;

        public bool OnCancel()
        {
            return true;
        }

        public bool OnNavigate(Vector2 dir) => false;

        public bool OnHotkey(string keyName) => false;

        public bool OnScroll(float deltaY) => false;

        public bool OnClick(int button, Vector2 mousePos) => false;

        public bool OnHoldStart(string holdKey) => false;

        public bool OnHoldUpdate(string holdKey) => false;

        public bool OnHoldingEnd(string holdKey) => false;
    }
}
