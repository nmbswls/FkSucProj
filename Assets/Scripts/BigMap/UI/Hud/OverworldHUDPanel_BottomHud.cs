namespace My.UI
{
    public partial class OverworldHUDPanel
    {
        public void ShowBottomProgress(string hintText, float duration, float phaseStartLogicTime)
        {
            bottomProgressPanel.Setup(hintText, duration, phaseStartLogicTime);
        }

        public void TryCancelProgressComplete(float phaseStartLogicTime)
        {
            bottomProgressPanel.TryCancel(phaseStartLogicTime);
        }
    }
}
