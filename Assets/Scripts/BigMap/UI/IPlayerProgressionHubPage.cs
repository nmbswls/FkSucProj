namespace My.UI
{
    // 仅由 PlayerProgressionHubPanel 实例化并托管的子页
    public interface IPlayerProgressionHubPage
    {
        bool IsHostedByHub { get; }

        void SetProgressionHubHost(IPlayerProgressionHubHost host);
    }
}
