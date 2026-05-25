using cfg.demo;
using My.Config;
using My.Map.Entity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public sealed class SkillLearnEntryView : MonoBehaviour
    {
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI ReasonText;
        public Button LearnButton;
        public TextMeshProUGUI LearnButtonText;

        SkillLearnEntry _entry;
        System.Action<int> _onLearnClicked;

        void Awake()
        {
            if (LearnButton != null)
            {
                LearnButton.onClick.RemoveAllListeners();
                LearnButton.onClick.AddListener(OnLearnClicked);
            }
        }

        public void Bind(SkillLearnEntry entry, System.Action<int> onLearnClicked)
        {
            _entry = entry;
            _onLearnClicked = onLearnClicked;
            RefreshState();
        }

        public void RefreshState()
        {
            if (_entry == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            string display = _entry.DisplayName;
            if (string.IsNullOrEmpty(display))
            {
                SkillLearnCatalog.TryGetEntitySkillData(_entry.SkillId, out var skillCfg);
                display = skillCfg != null && !string.IsNullOrEmpty(skillCfg.Desc)
                    ? skillCfg.Desc
                    : _entry.SkillId;
            }

            if (TitleText != null)
            {
                TitleText.text = display;
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            string reason = string.Empty;
            bool canLearn = mgr != null && mgr.CanLearnSkillFromEntry(_entry.EntryId, out reason);
            string reasonText = canLearn ? string.Empty : TranslateLearnReason(reason);

            if (ReasonText != null)
            {
                ReasonText.text = reasonText;
                ReasonText.gameObject.SetActive(!canLearn && !string.IsNullOrEmpty(reasonText));
            }

            if (LearnButtonText != null)
            {
                LearnButtonText.text = mgr != null && mgr.SkillSystem.IsSkillLearned(_entry.SkillId) ? "已学" : "学习";
            }

            if (LearnButton != null)
            {
                LearnButton.interactable = canLearn;
            }
        }

        void OnLearnClicked()
        {
            if (_entry == null)
            {
                return;
            }

            _onLearnClicked?.Invoke(_entry.EntryId);
        }

        static string TranslateLearnReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return "无法学习";
            }

            return reason switch
            {
                "already_learned" => "已学习",
                "cond_fail" => "条件未满足",
                "no_entry" => "配置缺失",
                "no_player" => "角色数据不可用",
                _ => reason,
            };
        }
    }
}
