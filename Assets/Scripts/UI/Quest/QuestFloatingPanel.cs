using System.Collections.Generic;
using DG.Tweening;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class QuestFloatingPanel : PanelBase
    {
        public static QuestFloatingPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("QuestFloatingPanel");
                if (panel is QuestFloatingPanel hudPanel)
                {
                    return hudPanel;
                }

                return null;
            }
        }

        public RectTransform QuestEmptyHint;
        public RectTransform QuestDetailPanel;
        public CanvasGroup QuestDetailCanvasGroup;

        public TextMeshProUGUI QuestTitle;
        public RectTransform QuestObjLineContainer;

        public GameObject ObjectiveLineTemplate;

        public class QuestObjectiveLine
        {
            public GameObject RootGo;
            public TextMeshProUGUI ObjDesc;
            public TextMeshProUGUI CurrProgressText;
            public TextMeshProUGUI MaxProgressText;
            public Image CompleteHint;
            public Tween CompleteTween;
            public QuestObjectiveRuntime? BindingObjective = null;

            public void Refresh()
            {
                if (BindingObjective == null)
                {
                    Clear();
                    return;
                }

                UpdateObjectiveDescText();
                CompleteHint.gameObject.SetActive(BindingObjective.GetCurrProgress() >= BindingObjective.GetRequireProgress());
                RootGo.gameObject.SetActive(true);
            }

            public void UpdateObjectiveDescText()
            {
                if (BindingObjective.Data.CustomDesc)
                {
                    ObjDesc.text = BindingObjective.Data.FormatDesc;
                    return;
                }

                ObjDesc.text = "Unknown objective";
                switch (BindingObjective.Data.ObjType)
                {
                    case cfg.demo.EQuestObjectiveType.KillMonster:
                    {
                        if (!string.IsNullOrEmpty(BindingObjective.Data.ObjP4))
                        {
                            var unitCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(BindingObjective.Data.ObjP4);
                            var unitName = unitCfg?.Name ?? "Unknown unit";
                            ObjDesc.text = $"Defeat {unitName} {BindingObjective.GetCurrProgress()}/{BindingObjective.GetRequireProgress()}";
                        }

                        break;
                    }
                    case cfg.demo.EQuestObjectiveType.OwnItem:
                    {
                        var itemId = BindingObjective.Data.ObjP4;
                        var itemDef = ItemCatalog.GetItemDef(itemId);
                        var itemName = itemDef?.DisplayName ?? itemId ?? "Item";
                        ObjDesc.text = $"Collect {itemName} {BindingObjective.GetCurrProgress()}/{BindingObjective.GetRequireProgress()}";
                        break;
                    }
                    case cfg.demo.EQuestObjectiveType.SubmitItem:
                    {
                        var itemId = BindingObjective.Data.ObjP4;
                        var itemDef = ItemCatalog.GetItemDef(itemId);
                        var itemName = itemDef?.DisplayName ?? itemId ?? "Item";
                        ObjDesc.text = $"Submit {itemName} {BindingObjective.GetCurrProgress()}/{BindingObjective.GetRequireProgress()}";
                        break;
                    }
                    case cfg.demo.EQuestObjectiveType.Talk:
                    {
                        var desc = string.IsNullOrEmpty(BindingObjective.Data.FormatDesc)
                            ? "Talk"
                            : BindingObjective.Data.FormatDesc;
                        ObjDesc.text = $"{desc} {BindingObjective.GetCurrProgress()}/{BindingObjective.GetRequireProgress()}";
                        break;
                    }
                }
            }

            public void Clear()
            {
                BindingObjective = null;
                CompleteTween?.Kill();
                CompleteTween = null;
                ObjDesc.text = string.Empty;
                CompleteHint.gameObject.SetActive(false);
                RootGo.gameObject.SetActive(false);
            }
        }

        public List<QuestObjectiveLine> ObjectiveLines = new();

        private QuestInstance _bindingQuestInst { get; set; }
        private string _currStepId { get; set; }

        private bool _flagPendingStep = false;
        private Tween NextStepTween { get; set; } = null;

        void Awake()
        {
            for (int i = 0; i < 5; i++)
            {
                var objLineGo = Instantiate(ObjectiveLineTemplate, QuestObjLineContainer);
                var runtime = new QuestObjectiveLine
                {
                    RootGo = objLineGo,
                    ObjDesc = objLineGo.transform.Find("DescText").GetComponent<TextMeshProUGUI>(),
                    CompleteHint = objLineGo.transform.Find("CompleteMark").GetComponent<Image>(),
                };

                ObjectiveLines.Add(runtime);
                objLineGo.gameObject.SetActive(false);
            }

            ObjectiveLineTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            var questSys = MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem;
            var hasPendingTween = NextStepTween != null;
            foreach (var line in ObjectiveLines)
            {
                if (line.CompleteTween != null)
                {
                    hasPendingTween = true;
                }
            }

            if (hasPendingTween)
            {
                return;
            }

            CheckSwitchShowQuest();
            if (!_flagPendingStep)
            {
                return;
            }

            var seq = DOTween.Sequence();
            NextStepTween = seq
                .Append(QuestDetailCanvasGroup.DOFade(0, 0.3f))
                .AppendCallback(ForceUpdateQuestView)
                .Append(QuestDetailCanvasGroup.DOFade(1, 0.3f))
                .OnComplete(() => { NextStepTween = null; })
                .SetLink(gameObject);

            _currStepId = _bindingQuestInst?.ActiveStep?.CurrStepId ?? string.Empty;
            _flagPendingStep = false;
        }

        private void CheckSwitchShowQuest()
        {
            var questSys = MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem;
            int markQuestId = questSys.MarkQuestId;

            if (_bindingQuestInst == null && markQuestId == 0)
            {
                return;
            }

            if (_bindingQuestInst != null && _bindingQuestInst.cacheCfg.QuestId == markQuestId)
            {
                return;
            }

            if (markQuestId == 0)
            {
                _bindingQuestInst = null;
                _currStepId = string.Empty;
            }
            else
            {
                _bindingQuestInst = questSys.GetQuest(markQuestId);
                _currStepId = _bindingQuestInst.ActiveStep?.CurrStepId ?? string.Empty;
            }

            RefreshBindingQuest();
        }

        public override void Show()
        {
            base.Show();
            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestObjUpdate += UpdateQuestObjectiveDetailView;
            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestStepUpdate += UpdateQuestStepView;
        }

        public override void Hide()
        {
            base.Hide();
            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestObjUpdate -= UpdateQuestObjectiveDetailView;
            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestStepUpdate -= UpdateQuestStepView;

            NextStepTween?.Kill();
            NextStepTween = null;

            foreach (var line in ObjectiveLines)
            {
                line.Clear();
            }

            ClearView();
            _flagPendingStep = false;
            _bindingQuestInst = null;
            _currStepId = string.Empty;
        }

        public void RefreshBindingQuest()
        {
            if (_bindingQuestInst == null)
            {
                ClearView();
            }
            else
            {
                ForceUpdateQuestView();
            }

            _flagPendingStep = false;
        }

        public void ForceUpdateQuestView()
        {
            if (_bindingQuestInst == null || !_bindingQuestInst.IsActive)
            {
                QuestEmptyHint.gameObject.SetActive(true);
                QuestDetailPanel.gameObject.SetActive(false);
                return;
            }

            QuestEmptyHint.gameObject.SetActive(false);
            QuestDetailPanel.gameObject.SetActive(true);
            QuestTitle.text = _bindingQuestInst.cacheCfg.Name;

            foreach (var oneLine in ObjectiveLines)
            {
                oneLine.Clear();
            }

            if (_bindingQuestInst.ActiveStep == null)
            {
                return;
            }

            for (int i = 0; i < _bindingQuestInst.ActiveStep.ObjectiveRuntimes.Length; i++)
            {
                var objRuntime = _bindingQuestInst.ActiveStep.ObjectiveRuntimes[i];
                ObjectiveLines[i].BindingObjective = objRuntime;
                ObjectiveLines[i].Refresh();
            }
        }

        public void UpdateQuestObjectiveDetailView(int questId)
        {
            if (_bindingQuestInst == null || questId != _bindingQuestInst.cacheCfg.QuestId)
            {
                return;
            }

            if (_bindingQuestInst.ActiveStep == null)
            {
                return;
            }

            for (int i = 0; i < _bindingQuestInst.ActiveStep.ObjectiveRuntimes.Length; i++)
            {
                var objRuntime = _bindingQuestInst.ActiveStep.ObjectiveRuntimes[i];
                var lineStruct = ObjectiveLines[i];
                lineStruct.UpdateObjectiveDescText();

                if (!lineStruct.CompleteHint.gameObject.activeSelf && objRuntime.GetCurrProgress() >= objRuntime.GetRequireProgress())
                {
                    lineStruct.CompleteTween?.Kill();
                    lineStruct.CompleteTween = null;
                    lineStruct.CompleteHint.gameObject.SetActive(true);
                    lineStruct.CompleteHint.color = new Color(
                        lineStruct.CompleteHint.color.r,
                        lineStruct.CompleteHint.color.g,
                        lineStruct.CompleteHint.color.b,
                        0);

                    lineStruct.CompleteTween = lineStruct.CompleteHint.DOFade(1, 1.5f)
                        .OnComplete(() => { lineStruct.CompleteTween = null; })
                        .OnKill(() => { lineStruct.CompleteTween = null; })
                        .SetLink(gameObject);
                }
                else if (objRuntime.GetCurrProgress() < objRuntime.GetRequireProgress())
                {
                    lineStruct.CompleteTween?.Kill();
                    lineStruct.CompleteTween = null;
                    lineStruct.CompleteHint.gameObject.SetActive(false);
                }
            }
        }

        public void UpdateQuestStepView(int questId)
        {
            _flagPendingStep = true;
        }

        private void ClearView()
        {
            QuestEmptyHint.gameObject.SetActive(true);
            QuestDetailPanel.gameObject.SetActive(false);
        }
    }
}
