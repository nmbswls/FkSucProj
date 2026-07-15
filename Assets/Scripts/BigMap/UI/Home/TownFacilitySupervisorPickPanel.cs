using System.Collections.Generic;
using My;
using My.Config;
using My.Home;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.Home
{
    public sealed class TownFacilitySupervisorPickPanel : PanelWithInput
    {
        public const string PanelIdConst = "TownFacilitySupervisorPickPanel";

        [SerializeField] TextMeshProUGUI txtTitle;
        [SerializeField] Button btnClear;
        [SerializeField] Button btnClose;
        [SerializeField] TownFacilitySupervisorPickRowView[] candidateRows;

        TownFacilitySupervisorPickOpenArgs _args;
        const int ExpectedRowCount = 8;

        void Awake()
        {
            panelId = PanelIdConst;
            layer = UILayer.Popup;
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(CloseSelf);
            }

            if (btnClear != null)
            {
                btnClear.onClick.AddListener(OnClickClear);
            }
        }

        public static TownFacilitySupervisorPickPanel Open(TownFacilitySupervisorPickOpenArgs args)
        {
            return UIManager.Instance.ShowPanel(PanelIdConst, args) as TownFacilitySupervisorPickPanel;
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            _args = data as TownFacilitySupervisorPickOpenArgs;
            Refresh();
        }

        void Refresh()
        {
            if (_args == null)
            {
                return;
            }

            if (txtTitle != null)
            {
                txtTitle.text = $"选择监工 · 槽位 {_args.SlotIndex + 1}";
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var hm = glm?.homeDataManager;
            if (hm == null)
            {
                HideAllRows();
                return;
            }

            if (!string.IsNullOrEmpty(_args.LogicAreaId))
            {
                hm.SetTownContext(_args.LogicAreaId);
            }

            var candidates = hm.GetAssignableSupervisors(_args.LogicAreaId);
            if (candidateRows == null || candidateRows.Length == 0)
            {
                return;
            }

            for (int i = 0; i < candidateRows.Length; i++)
            {
                var row = candidateRows[i];
                if (row == null)
                {
                    continue;
                }

                if (i >= candidates.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var candidate = candidates[i];
                row.gameObject.SetActive(true);
                if (row.Label != null)
                {
                    row.Label.text = candidate.DisplayName;
                }

                if (row.Desc != null)
                {
                    row.Desc.text = string.IsNullOrEmpty(candidate.DisplayTitle)
                        ? candidate.CharacterKey
                        : candidate.DisplayTitle;
                }

                if (row.Button != null)
                {
                    row.Button.onClick.RemoveAllListeners();
                    var captured = candidate.CharacterKey;
                    row.Button.onClick.AddListener(() => OnPickCharacter(captured));
                }
            }
        }

        void HideAllRows()
        {
            if (candidateRows == null)
            {
                return;
            }

            foreach (var row in candidateRows)
            {
                if (row != null)
                {
                    row.gameObject.SetActive(false);
                }
            }
        }

        void OnPickCharacter(string characterKey)
        {
            ApplySelection(characterKey);
        }

        void OnClickClear()
        {
            ApplySelection(null);
        }

        void ApplySelection(string characterKey)
        {
            var hm = MainGameManager.Instance?.gameLogicManager?.homeDataManager;
            if (hm == null || _args == null)
            {
                return;
            }

            if (hm.TrySetFacilitySupervisor(
                    _args.SiteId,
                    _args.InstanceId,
                    _args.FacilityId,
                    _args.SlotIndex,
                    characterKey,
                    out _))
            {
                CloseSelf();
            }
        }

        void CloseSelf()
        {
            UIManager.Instance.HidePanel(PanelIdConst);
        }

        public override bool OnCancel()
        {
            CloseSelf();
            return true;
        }

        void OnValidate()
        {
            if (candidateRows != null && candidateRows.Length < ExpectedRowCount)
            {
                Debug.LogWarning("TownFacilitySupervisorPickPanel expects 8 prefab rows.");
            }
        }
    }
}
