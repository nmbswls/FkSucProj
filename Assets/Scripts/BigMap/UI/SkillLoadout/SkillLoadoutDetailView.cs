using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.SkillLoadout
{
    public sealed class SkillLoadoutDetailView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI nameLabel;
        [SerializeField] TextMeshProUGUI descText;
        [SerializeField] TextMeshProUGUI levelChangeText;
        [SerializeField] TextMeshProUGUI costLineText;
        [SerializeField] TextMeshProUGUI statusLineText;
        [SerializeField] Button btnLearn;

        int _entryId;
        bool _learnButtonWired;
        System.Action<int> _onLearnClicked;

        void Awake()
        {
            EnsureReferences();
            EnsureLearnButtonWired();

            if (descText != null)
            {
                descText.gameObject.SetActive(false);
            }

            Hide();
        }

        public void SetLearnHandler(System.Action<int> onLearnClicked)
        {
            _onLearnClicked = onLearnClicked;
        }

        void EnsureReferences()
        {
            if (nameLabel == null)
            {
                nameLabel = transform.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
            }

            if (descText == null)
            {
                descText = transform.Find("DescText")?.GetComponent<TextMeshProUGUI>();
            }

            if (levelChangeText == null)
            {
                levelChangeText = transform.Find("LevelChange")?.GetComponent<TextMeshProUGUI>();
            }

            if (costLineText == null)
            {
                costLineText = transform.Find("CostLine")?.GetComponent<TextMeshProUGUI>();
            }

            if (statusLineText == null)
            {
                statusLineText = transform.Find("StatusLine")?.GetComponent<TextMeshProUGUI>();
            }

            if (btnLearn == null)
            {
                btnLearn = transform.Find("LearnBtn")?.GetComponent<Button>();
            }
        }

        void EnsureLearnButtonWired()
        {
            if (btnLearn == null || _learnButtonWired)
            {
                return;
            }

            btnLearn.onClick.RemoveListener(OnLearnButtonClicked);
            btnLearn.onClick.AddListener(OnLearnButtonClicked);
            _learnButtonWired = true;
        }

        public void Show(int entryId)
        {
            EnsureReferences();
            EnsureLearnButtonWired();

            var entry = SkillLearnCatalog.TryGetLearnEntry(entryId);
            if (entry == null || string.IsNullOrEmpty(entry.SkillId))
            {
                Hide();
                return;
            }

            _entryId = entryId;

            var sys = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.SkillSystem;
            bool isLearned = sys != null && sys.IsSkillLearned(entry.SkillId);
            RefreshContent(entry, isLearned);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _entryId = 0;
            gameObject.SetActive(false);
        }

        void RefreshContent(SkillLearnEntry entry, bool isLearned)
        {
            var skillCfg = SkillLibrary.GetSkillConfig(entry.SkillId);
            int learnLevel = entry.SkillLevel > 0 ? entry.SkillLevel : 1;

            SetText(nameLabel, SkillLearnEntryTextUtil.ResolveDisplayName(entry, skillCfg));
            SetText(levelChangeText, isLearned ? $"当前等级 {learnLevel}" : $"学习等级 {learnLevel}");
            SetText(costLineText, SkillLearnEntryTextUtil.BuildLearnCostLine(entry.LearnConds));
            SetText(statusLineText, SkillLearnEntryTextUtil.BuildDetailStatusLine(entry, isLearned));
            RefreshLearnButton(entry, isLearned);
        }

        void RefreshLearnButton(SkillLearnEntry entry, bool isLearned)
        {
            if (btnLearn == null)
            {
                return;
            }

            bool showLearn = !isLearned;
            btnLearn.gameObject.SetActive(showLearn);
            if (!showLearn)
            {
                return;
            }

            btnLearn.interactable = true;
        }

        void OnLearnButtonClicked()
        {
            if (_entryId <= 0)
            {
                return;
            }

            _onLearnClicked?.Invoke(_entryId);
        }

        static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
