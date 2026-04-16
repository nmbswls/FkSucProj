using System.Threading.Tasks;
using My;
using My.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SavePointPanel : PanelBase
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;

        public void BeginFlow()
        {
            if (statusText != null)
            {
                statusText.text = "Saving...";
            }

            _ = RunSaveThenCloseAsync();
        }

        private async Task RunSaveThenCloseAsync()
        {
            try
            {
                if (MainGameManager.Instance != null)
                {
                    await MainGameManager.Instance.OnSaveClicked();
                }

                if (statusText != null)
                {
                    statusText.text = "Saved.";
                }
            }
            finally
            {
                await Task.Delay(400);
                UIManager.Instance.HidePanel("SavePointPanel");
                LogicTime.ReleasePause("SavePoint");
            }
        }

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    UIManager.Instance.HidePanel("SavePointPanel");
                    LogicTime.ReleasePause("SavePoint");
                });
            }
        }
    }
}
