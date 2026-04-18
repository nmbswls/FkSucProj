using My.UI;
using UnityEngine;

namespace My.MiniGame.Dream
{
    public static class DreamInfiltrationIds
    {
        public const string EntryPanel = "DreamInfiltrationEntry";
        public const string GameplayPanel = "DreamInfiltrationGameplay";
        public const string SettlementPanel = "DreamInfiltrationSettlement";
    }

    public static class DreamInfiltrationBootstrap
    {
        public static void OpenEntry()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[DreamInfiltration] UIManager not ready.");
                return;
            }

            DreamInfiltrationLogicPause.EnterMiniGame();
            UIManager.Instance.ShowPanel(DreamInfiltrationIds.EntryPanel, null, UILayer.Overlay);
        }
    }
}
