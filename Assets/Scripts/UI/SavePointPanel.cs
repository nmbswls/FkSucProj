using System.Threading.Tasks;
using My;
using My.Map.Entity;
using My.Map.SavePoint;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SavePointPanel : PanelBase
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button primaryButton;
        [SerializeField] TMP_Text statusText;

        LogicEntitySavePoint _bound;

        public void BeginFlow(LogicEntitySavePoint entity)
        {
            _bound = entity;
            RefreshUi();

            if (statusText != null && _bound != null && _bound.IsFormallyUnlocked)
            {
                statusText.text = "Saving...";
                _ = RunSaveThenCloseAsync();
            }
        }

        void RefreshUi()
        {
            if (_bound == null)
            {
                if (statusText != null)
                {
                    statusText.text = "No save point.";
                }

                return;
            }

            var cfg = _bound.Cfg;
            var glm = _bound.LogicManager;
            var persist = SavePointUnlockHelper.GetPersist(glm, _bound.SavePointId);

            if (_bound.IsFormallyUnlocked)
            {
                if (statusText != null)
                {
                    statusText.text = "Saving...";
                }

                if (primaryButton != null)
                {
                    primaryButton.gameObject.SetActive(false);
                }

                return;
            }

            if (primaryButton != null)
            {
                primaryButton.gameObject.SetActive(true);
                primaryButton.onClick.RemoveAllListeners();
            }

            if (_bound.NeedsTribute)
            {
                if (statusText != null)
                {
                    statusText.text = "Tribute: " + SavePointUnlockHelper.BuildTributeProgressText(cfg, persist);
                }

                if (primaryButton != null)
                {
                    var label = primaryButton.GetComponentInChildren<TMP_Text>();
                    if (label != null)
                    {
                        label.text = "Submit tribute";
                    }

                    primaryButton.onClick.AddListener(OnSubmitTributeClicked);
                }
            }
            else
            {
                if (statusText != null)
                {
                    statusText.text = "Activate this save point?";
                }

                if (primaryButton != null)
                {
                    var label = primaryButton.GetComponentInChildren<TMP_Text>();
                    if (label != null)
                    {
                        label.text = "Activate";
                    }

                    primaryButton.onClick.AddListener(OnActivateClicked);
                }
            }
        }

        void OnActivateClicked()
        {
            if (_bound == null)
            {
                return;
            }

            if (!SavePointUnlockHelper.TryUnlockOnInteract(_bound.LogicManager, _bound.SavePointId, out var reason))
            {
                if (statusText != null)
                {
                    statusText.text = "Activate failed: " + reason;
                }

                return;
            }

            BeginFlow(_bound);
        }

        void OnSubmitTributeClicked()
        {
            if (_bound == null)
            {
                return;
            }

            if (!SavePointUnlockHelper.TrySubmitTribute(_bound.LogicManager, _bound.SavePointId, out var reason))
            {
                if (statusText != null)
                {
                    statusText.text = "Submit failed: " + reason;
                }

                return;
            }

            if (_bound.IsFormallyUnlocked)
            {
                if (statusText != null)
                {
                    statusText.text = "Unlocked. Saving...";
                }

                _ = RunSaveThenCloseAsync();
                return;
            }

            RefreshUi();
        }

        async Task RunSaveThenCloseAsync()
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
                ClosePanel();
            }
        }

        void ClosePanel()
        {
            UIManager.Instance.HidePanel("SavePointPanel");
            LogicTime.ReleasePause("SavePoint");
            _bound = null;
        }

        void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }
        }
    }
}
