using System.Collections.Generic;
using System.Text;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class GlobalWorldPanel : PanelBase, IInputConsumer, IPlayerProgressionHubPage
    {
        public const string Pid = "GlobalWorldPanel";

        const string SummaryPath = "Window/WorldSummary";

        [SerializeField]
        TMP_Text summaryText;

        Transform _root;
        IPlayerProgressionHubHost _progressionHubHost;

        public bool IsHostedByHub => _progressionHubHost != null;

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
            ApplyHostedChromeIfNeeded();
        }

        void ApplyHostedChromeIfNeeded()
        {
            if (_progressionHubHost == null || _root == null)
            {
                return;
            }

            var blocker = _root.Find("BlockerButton");
            if (blocker != null)
            {
                blocker.gameObject.SetActive(false);
            }
        }

        void BindRefs()
        {
            _root = transform.Find("BuiltRoot");
            if (summaryText == null && _root != null)
            {
                summaryText = _root.Find(SummaryPath)?.GetComponent<TMP_Text>();
            }
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (!IsHostedByHub)
            {
                Debug.LogError("[GlobalWorldPanel] Setup without hub host.");
                return;
            }

            BindRefs();
            ApplyHostedChromeIfNeeded();
            RefreshSummary();
        }

        public override void Show()
        {
            if (!IsHostedByHub)
            {
                Debug.LogError("[GlobalWorldPanel] Show without hub host.");
                return;
            }

            base.Show();
            RefreshSummary();
        }

        void RefreshSummary()
        {
            BindRefs();
            if (summaryText == null)
            {
                Debug.LogWarning("[GlobalWorldPanel] summaryText missing; assign in prefab or place BuiltRoot/" + SummaryPath);
                return;
            }

            var glm = MainGameManager.Instance != null ? MainGameManager.Instance.gameLogicManager : null;
            var pdm = glm?.playerDataManager;

            var dayIdx = glm != null ? glm.SettlementDayIndex : 0;
            var displayDay = Mathf.Max(1, dayIdx + 1);

            var fallen = pdm != null ? pdm.TotalFallPeopleAmount : 0L;

            var sb = new StringBuilder(256);
            sb.AppendLine($"World day: {displayDay}");
            sb.AppendLine($"Total fallen: {fallen}");
            sb.AppendLine();
            sb.AppendLine("Unlocked fallen milestones (stage rewards):");

            var table = CfgMgr.Cfgs?.TbFallenAmountProgressInfo;
            if (table?.DataList == null || table.DataList.Count == 0)
            {
                sb.AppendLine("(no TbFallenAmountProgressInfo rows)");
            }
            else
            {
                var rows = new List<FallenAmountProgressInfo>(table.DataList);
                rows.Sort((a, b) => a.Amount.CompareTo(b.Amount));

                var any = false;
                foreach (var row in rows)
                {
                    if (row == null)
                    {
                        continue;
                    }

                    if (fallen < row.Amount)
                    {
                        continue;
                    }

                    any = true;
                    sb.AppendLine(FormatUnlockedRow(row));
                }

                if (!any)
                {
                    sb.AppendLine("(none yet — increase Total fallen to reach a milestone threshold)");
                }
            }

            summaryText.text = sb.ToString();
        }

        static string FormatUnlockedRow(FallenAmountProgressInfo row)
        {
            var sb = new StringBuilder();
            sb.Append($"  Stage #{row.Id} — threshold {row.Amount} (fallen reached): ");
            if (row.ExtraAttr == null || row.ExtraAttr.Count == 0)
            {
                sb.Append("(no ExtraAttr)");
            }
            else
            {
                var first = true;
                foreach (var kv in row.ExtraAttr)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }

                    first = false;
                    var name = System.Enum.IsDefined(typeof(EYCAttribute), kv.Key)
                        ? ((EYCAttribute)kv.Key).ToString()
                        : $"Attr{kv.Key}";
                    sb.Append($"{name} +{kv.Value}");
                }
            }

            return sb.ToString();
        }

        public bool OnConfirm() => false;

        public bool OnCancel()
        {
            if (_progressionHubHost != null)
            {
                _progressionHubHost.CloseHub();
                return true;
            }

            return false;
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
