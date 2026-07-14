using My.Player;

namespace My.UI.Talent
{
    public interface ITalentProgressionContext
    {
        int GetTalentNodeLevel(int nodeId);

        PlayerTalentManager.TalentNodeVisualState GetTalentNodeVisualState(int nodeId);

        bool TryUpgradeTalentNode(int nodeId, out string failReason);
    }
}
