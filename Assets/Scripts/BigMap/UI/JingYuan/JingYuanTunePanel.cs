using My;

namespace My.UI
{
    public class JingYuanTunePanel : PanelBase, IPlayerProgressionHubPage
    {
        public const string Pid = "JingYuanTunePanel";

        IPlayerProgressionHubHost _progressionHubHost;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                panelId = Pid;
            }
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host)
        {
            _progressionHubHost = host;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
        }

        public override void Show()
        {
            base.Show();
        }
    }
}
