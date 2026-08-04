using System;
using cfg.demo;
using My.Config;
using My.SecretBase;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public static class JingYuanFacilityAccess
    {
        static bool _granted;

        public static bool CanOpenTune(GameLogicManager glm)
        {
            return _granted && glm != null && glm.IsInSecretBaseContext();
        }

        public static void Grant() => _granted = true;

        public static void Revoke() => _granted = false;
    }

    // 精元池：信息 / 升级 / 存取 / 分解 / 秘仪入口
    public sealed class JingYuanPoolPanel : PanelBase
    {
        public const string PanelIdConst = "JingYuanPoolPanel";
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI infoText;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Button upgradeButton;
        [SerializeField] Button depositButton;
        [SerializeField] Button withdrawButton;
        [SerializeField] Button decomposeButton;
        [SerializeField] Button ritualButton;
        [SerializeField] Button tuneButton;
        [SerializeField] Button warehouseButton;
        [SerializeField] Button closeButton;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            ResolveRefs();
            Wire(upgradeButton, OnUpgrade);
            Wire(depositButton, OnDeposit);
            Wire(withdrawButton, OnWithdraw);
            Wire(decomposeButton, OnDecompose);
            Wire(ritualButton, OnRitual);
            Wire(tuneButton, OpenTune);
            Wire(warehouseButton, OpenWarehouse);
            Wire(closeButton, () => UIManager.Instance.HidePanel(PanelIdConst));
        }

        public override void Show()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext())
            {
                UIManager.Instance.HidePanel(PanelIdConst);
                return;
            }

            base.Show();
            JingYuanPoolService.OnPoolChanged -= Refresh;
            JingYuanPoolService.OnPoolChanged += Refresh;
            Refresh();
        }

        public override void Hide()
        {
            JingYuanPoolService.OnPoolChanged -= Refresh;
            JingYuanFacilityAccess.Revoke();
            base.Hide();
        }

        void OnDestroy()
        {
            JingYuanPoolService.OnPoolChanged -= Refresh;
        }

        void Refresh()
        {
            ResolveRefs();
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;

            var level = JingYuanPoolService.GetFacilityLevel(glm);
            var cfg = JingYuanPoolService.GetLevelConfig(glm, level);
            var next = JingYuanPoolService.GetNextLevelConfig(glm);
            var stored = JingYuanPoolService.GetStored(glm);
            var cap = JingYuanPoolService.GetCapacity(glm);
            var bag = CountBagJingyuan(glm);
            var residue = glm.playerDataManager?.JingYuanEssenceSystem?.JingYuanResidue ?? 0;
            var per = Math.Max(1, cfg?.DecomposeJingyuanPerResidue ?? 10);

            if (titleText != null)
                titleText.text = cfg != null && !string.IsNullOrEmpty(cfg.DisplayName) ? cfg.DisplayName : "精元池";

            if (infoText != null)
            {
                var overflow = JingYuanPoolService.GetOverflow(glm);
                var overflowLine = overflow > 0
                    ? $"超额 {overflow}（日结开始时丢弃未消化部分）\n"
                    : string.Empty;
                var upgradeLine = next == null
                    ? "已达最高等级"
                    : $"下一等级 Lv{next.Level}：容量 {next.PoolCapacity}，消耗池精元 {next.UpgradeCostJingyuan}";
                infoText.text =
                    $"等级 Lv{level}\n" +
                    $"池内精元：{stored} / {cap}\n" +
                    overflowLine +
                    $"背包精元：{bag}\n" +
                    $"残精：{residue}\n" +
                    $"分解效率：{per} 精元 → 1 残精\n" +
                    $"转化参考：约 20~25 精元 = 1 浊液\n" +
                    $"{upgradeLine}\n" +
                    (cfg != null ? cfg.Desc : string.Empty);
            }

            if (upgradeButton != null)
            {
                var can = JingYuanPoolService.CanUpgrade(glm, out _);
                upgradeButton.interactable = can;
                var label = upgradeButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = next == null ? "已满级" : "升级";
            }

            SetButtonLabel(depositButton, "存入精元");
            SetButtonLabel(withdrawButton, "取出精元");
            SetButtonLabel(decomposeButton, "分解为残精");
            var ritualId = JingYuanPoolService.PickRitualId(glm);
            var ritual = CfgMgr.Cfgs?.TbJingYuanPoolRitual?.GetOrDefault(ritualId);
            SetButtonLabel(ritualButton, ritual != null ? ritual.DisplayName : "秘仪");
            SetButtonLabel(tuneButton, "调精");
            SetButtonLabel(warehouseButton, "精华仓库");
        }

        void OnUpgrade()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;
            if (!JingYuanPoolService.TryUpgrade(glm, out var reason))
            {
                SetStatus(FormatFail(reason));
                return;
            }
            SetStatus("升级成功");
            Refresh();
        }

        void OnDeposit()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;
            var bag = CountBagJingyuan(glm);
            var amount = bag;
            if (amount <= 0)
            {
                SetStatus("背包没有精元");
                return;
            }
            if (!JingYuanPoolService.TryDepositFromInventory(glm, amount, out var reason))
            {
                SetStatus(FormatFail(reason));
                return;
            }
            SetStatus($"已存入 {amount}");
            Refresh();
        }

        void OnWithdraw()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;
            // 初版一次取出最多 100，便于外出携带调试
            var amount = Math.Min(100, JingYuanPoolService.GetStored(glm));
            if (amount <= 0)
            {
                SetStatus("池内没有精元");
                return;
            }
            if (!JingYuanPoolService.TryWithdrawToInventory(glm, amount, out var reason))
            {
                SetStatus(FormatFail(reason));
                return;
            }
            SetStatus($"已取出 {amount}");
            Refresh();
        }

        void OnDecompose()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;
            var cfg = JingYuanPoolService.GetLevelConfig(glm);
            var per = Math.Max(1, cfg?.DecomposeJingyuanPerResidue ?? 10);
            var amount = JingYuanPoolService.GetStored(glm) / per * per;
            if (amount <= 0)
            {
                SetStatus($"至少需要 {per} 精元才能分解");
                return;
            }
            // 默认分解一档（per），长按/批量后续再做
            amount = per;
            if (!JingYuanPoolService.TryDecompose(glm, amount, out var residue, out var reason))
            {
                SetStatus(FormatFail(reason));
                return;
            }
            SetStatus($"分解获得残精 +{residue}");
            Refresh();
        }

        void OnRitual()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null) return;
            var ritualId = JingYuanPoolService.PickRitualId(glm);
            if (!JingYuanPoolService.TryStartRitual(glm, ritualId, out var reason))
            {
                SetStatus(FormatFail(reason));
                return;
            }
            SetStatus("秘仪已开始");
            Refresh();
        }

        void OpenTune()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext()) return;
            JingYuanFacilityAccess.Grant();
            PlayerProgressionHubPanel.OpenJingYuanTune();
        }

        void OpenWarehouse()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm == null || !glm.IsInSecretBaseContext()) return;
            UIManager.Instance.ShowPanel(JingYuanWarehousePanel.PanelIdConst);
        }

        void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text ?? string.Empty;
        }

        static long CountBagJingyuan(GameLogicManager glm)
        {
            var inv = glm?.playerDataManager?.InventorySystem;
            if (inv == null) return 0;
            return inv.GetItemTotal(JingYuanPoolService.JingYuanItemId, includeWarehouse: true);
        }

        static string FormatFail(string reason) => reason switch
        {
            "max_level" => "已达最高等级",
            "jingyuan_not_enough" => "池内精元不足",
            "item_not_enough" => "材料不足",
            "pool_full" => "精元池已满",
            "pool_empty" => "池内没有精元",
            "too_few" => "精元数量不足以分解",
            "ritual_missing" => "秘仪配置缺失",
            _ => "操作失败",
        };

        static void Wire(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static void SetButtonLabel(Button button, string text)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = text;
        }

        void ResolveRefs()
        {
            titleText ??= FindText("Title");
            infoText ??= FindText("InfoText") ?? FindText("Description");
            statusText ??= FindText("StatusText");
            upgradeButton ??= FindButton("UpgradeButton");
            depositButton ??= FindButton("DepositButton");
            withdrawButton ??= FindButton("WithdrawButton");
            decomposeButton ??= FindButton("DecomposeButton");
            ritualButton ??= FindButton("RitualButton");
            tuneButton ??= FindButton("TuneButton");
            warehouseButton ??= FindButton("WarehouseButton");
            closeButton ??= FindButton("CloseButton");
        }

        TextMeshProUGUI FindText(string name)
        {
            var t = FindChild(name);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }

        Button FindButton(string name)
        {
            var t = FindChild(name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        Transform FindChild(string name)
        {
            return transform.Find(name)
                   ?? transform.Find("BuiltRoot/" + name)
                   ?? transform.Find("Frame/" + name)
                   ?? transform.Find("Content/" + name);
        }
    }
}
