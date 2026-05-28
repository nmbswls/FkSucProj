using System.Threading.Tasks;
using My;
using My.Map;
using My.Map.Entity;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class SavePointPanel : PanelWithInput
    {
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text statusText;

        [Header("Form Switch")]
        [SerializeField] Button switchButton;
        [SerializeField] TMP_Text switchStateText;

        [Header("Vault")]
        [SerializeField] GameObject vaultSectionRoot;
        [SerializeField] TMP_Text carriedCountText;
        [SerializeField] TMP_Text quotaText;
        [SerializeField] Button depositButton;
        [SerializeField] TMP_Text depositFeedbackText;

        LogicEntitySavePoint _bound;
        bool _listenersBound;

        public void BeginFlow(LogicEntitySavePoint entity)
        {
            _bound = entity;
            if (_bound == null || !_bound.IsActivated)
            {
                Debug.LogWarning("[SavePointPanel] Save point is not activated.");
                ClosePanel();
                return;
            }

            BindListeners();
            RefreshSwitchUi();
            RefreshVaultUi();
            if (statusText != null)
            {
                statusText.text = "Saving...";
            }

            _ = RunAutoSaveAsync();
        }

        async Task RunAutoSaveAsync()
        {
            try
            {
                if (MainGameManager.Instance != null)
                {
                    await MainGameManager.Instance.OnSaveClicked();
                }

                if (statusText != null)
                {
                    statusText.text = "已存档。";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SavePointPanel] Auto save failed: " + ex.Message);
                if (statusText != null)
                {
                    statusText.text = "存档失败。";
                }
            }
        }

        void BindListeners()
        {
            if (_listenersBound)
            {
                return;
            }

            _listenersBound = true;
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (switchButton != null)
            {
                switchButton.onClick.AddListener(OnSwitchClick);
            }

            if (depositButton != null)
            {
                depositButton.onClick.AddListener(OnDepositClick);
            }
        }

        void RefreshSwitchUi()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            bool showSwitch = glm != null && glm.IsSavePointFormSwitchVisible();
            if (switchButton != null)
            {
                switchButton.gameObject.SetActive(showSwitch);
            }

            if (!showSwitch)
            {
                return;
            }

            if (switchStateText != null)
            {
                switchStateText.text = glm.PlayerHumanMode ? "人类" : "真身";
            }

            if (switchButton != null)
            {
                switchButton.interactable = glm.CanToggleSavePointForm(out _);
            }
        }

        void OnSwitchClick()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (!glm.TryToggleSavePointForm(out var failReason))
            {
                if (statusText != null)
                {
                    statusText.text = failReason switch
                    {
                        "not_civil" => "当前区域无法切换为人类形态。",
                        "not_danger" => "当前区域无法切换为真身形态。",
                        "area_not_supported" => "当前区域不支持形态切换。",
                        _ => "无法切换形态。",
                    };
                }

                RefreshSwitchUi();
                return;
            }

            RefreshSwitchUi();
        }

        void RefreshVaultUi()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            bool showVault = glm != null && glm.IsSavePointVaultAvailable;
            if (vaultSectionRoot != null)
            {
                vaultSectionRoot.SetActive(showVault);
            }

            if (!showVault)
            {
                return;
            }

            if (glm == null)
            {
                if (depositButton != null)
                {
                    depositButton.interactable = false;
                }

                return;
            }

            var carried = glm.GetSavePointVaultCarriedDesireShard();
            var deposited = glm.SavePointVaultDepositedThisRun;
            var quota = glm.GetSavePointVaultRemainingQuota();
            var depositable = glm.GetSavePointVaultDepositableAmount();

            if (carriedCountText != null)
            {
                carriedCountText.text = $"携带：{carried}";
            }

            if (quotaText != null)
            {
                quotaText.text = $"本次额度：{deposited} / {GameLogicManager.SavePointDesireShardRunCap}（剩余 {quota}）";
            }

            if (depositButton != null)
            {
                depositButton.interactable = depositable > 0;
            }
        }

        void OnDepositClick()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null)
            {
                return;
            }

            if (!glm.TryDepositSavePointVaultAllAvailable(out var deposited, out var failReason))
            {
                if (depositFeedbackText != null)
                {
                    depositFeedbackText.text = failReason switch
                    {
                        "nothing_to_deposit" => "没有可存入的数量。",
                        "warehouse_full" => "仓库空间不足。",
                        _ => "存入失败。",
                    };
                }

                Debug.LogWarning("[SavePointPanel] Vault deposit failed: " + failReason);
                RefreshVaultUi();
                return;
            }

            if (depositFeedbackText != null)
            {
                depositFeedbackText.text = $"已存入 {deposited} 个欲望碎片。";
            }

            RefreshVaultUi();
        }

        void ClosePanel()
        {
            UIManager.Instance.HidePanel("SavePointPanel");
            LogicTime.ReleasePause("SavePoint");
            _bound = null;
        }

        public override bool OnCancel()
        {
            ClosePanel();
            return true;
        }

        void Awake()
        {
            BindListeners();
        }
    }
}
