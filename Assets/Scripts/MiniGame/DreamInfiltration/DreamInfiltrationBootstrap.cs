using My.UI;
using UnityEngine;

namespace My.MiniGame.Dream
{
    public static class DreamInfiltrationBootstrap
    {
        public static void OpenEntry()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[DreamInfiltration] UIManager not ready.");
                return;
            }

            UIManager.Instance.ShowPanel(DreamInfiltrationIds.EntryPanel, null, UILayer.Overlay);
        }
    }
}
