
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
                if (panel != null && panel is QuestFloatingPanel hudPanel)
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
                if(BindingObjective == null)
                {
                    Clear();
                    return;
                }

                UpdateObjectiveDescText();


                if (BindingObjective.GetCurrProgress() >= BindingObjective.GetRequireProgress())
                {
                    CompleteHint.gameObject.SetActive(true);
                }
                else
                {
                    CompleteHint.gameObject.SetActive(false);
                }

                RootGo.gameObject.SetActive(true);

            }

            public void UpdateObjectiveDescText()
            {
                if (BindingObjective.Data.CustomDesc)
                {
                    ObjDesc.text = BindingObjective.Data.FormatDesc;
                }
                else
                {
                    ObjDesc.text = "未知条件需求";
                    switch (BindingObjective.Data.ObjType)
                    {
                        case cfg.demo.EQuestObjectiveType.KillMonster:
                            {
                                if (!string.IsNullOrEmpty(BindingObjective.Data.ObjP4))
                                {
                                    var unitCfg = CfgMgr.Cfgs.TbUnitNpc.GetOrDefault(BindingObjective.Data.ObjP4);
                                    string mName = unitCfg?.Name ?? "未知单位";
                                    ObjDesc.text = $"击杀{mName} {BindingObjective.GetCurrProgress()}/{BindingObjective.GetRequireProgress()}";
                                }
                            }
                            break;
                        default:
                            {

                                break;
                            }
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
            for(int i=0;i<5;i++)
            {
                var objLineGo = GameObject.Instantiate(ObjectiveLineTemplate, QuestObjLineContainer);

                var runtime = new QuestObjectiveLine();
                runtime.RootGo = objLineGo;
                runtime.ObjDesc = objLineGo.transform.Find("DescText").GetComponent<TextMeshProUGUI>();
                runtime.CompleteHint = objLineGo.transform.Find("CompleteMark").GetComponent<Image>();

                ObjectiveLines.Add(runtime);
                objLineGo.gameObject.SetActive(false);
            }

            ObjectiveLineTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            var questSys = MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem;
            int markQuestId = questSys.MarkQuestId;

            bool hasPendingTween = false;
            foreach(var line in ObjectiveLines)
            {
                if(line.CompleteTween != null)
                {
                    hasPendingTween = true;
                }
            }

            if(NextStepTween != null)
            {
                hasPendingTween = true;
            }

            if(!hasPendingTween)
            {
                CheckSwitchShowQuest();

                if (_flagPendingStep)
                {
                    // 切换任务 此时因为一定没有特效播放 选择是否使用渐变

                    //ForceUpdateQuestView();
                    var seq = DOTween.Sequence();
                    
                    NextStepTween = seq
                        .Append(QuestDetailCanvasGroup.DOFade(0, 0.3f))
                        .AppendCallback(() =>
                        {
                            ForceUpdateQuestView();
                        })
                        .Append(QuestDetailCanvasGroup.DOFade(1, 0.3f))
                        .OnComplete(() =>
                        {
                            NextStepTween = null;
                        }).SetLink(gameObject);

                    _currStepId = _bindingQuestInst?.ActiveStep?.CurrStepId ?? string.Empty;
                    _flagPendingStep = false;
                }
            }
        }

        private void CheckSwitchShowQuest()
        {
            var questSys = MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem;
            int markQuestId = questSys.MarkQuestId;

            if(_bindingQuestInst == null && markQuestId == 0)
            {
                return;
            }

            if (_bindingQuestInst != null && _bindingQuestInst.cacheCfg.QuestId == markQuestId)
            {
                return;
            }

            if(markQuestId == 0)
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

            // 清理一切

            NextStepTween?.Kill();
            NextStepTween = null;

            foreach(var line in ObjectiveLines)
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
            if(_bindingQuestInst == null)
            {
                ClearView();
            }
            else
            {
                ForceUpdateQuestView();
            }

            _flagPendingStep = false;
        }


        /// <summary>
        /// 强制刷新一次任务视窗
        /// 通常用于切换显示
        /// </summary>
        public void ForceUpdateQuestView()
        {
            if(_bindingQuestInst == null || !_bindingQuestInst.IsActive)
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

            if (_bindingQuestInst.ActiveStep != null)
            {
                for (int i = 0; i < _bindingQuestInst.ActiveStep.ObjectiveRuntimes.Length; i++)
                {
                    var objRuntime = _bindingQuestInst.ActiveStep.ObjectiveRuntimes[i];

                    ObjectiveLines[i].BindingObjective = objRuntime;
                    ObjectiveLines[i].Refresh();
                }
            }
        }

        

        /// <summary>
        /// 更新单条目标变化
        /// </summary>
        /// <param name="questId"></param>
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

                // 检查播放完成动效
                if(!lineStruct.CompleteHint.gameObject.activeSelf && objRuntime.GetCurrProgress() >= objRuntime.GetRequireProgress())
                {
                    if(lineStruct.CompleteTween != null)
                    {
                        lineStruct.CompleteTween.Kill();
                        lineStruct.CompleteTween = null;
                    }

                    lineStruct.CompleteHint.gameObject.SetActive(true);
                    lineStruct.CompleteHint.color = new Color(lineStruct.CompleteHint.color.r, lineStruct.CompleteHint.color.g, lineStruct.CompleteHint.color.b, 0);

                    lineStruct.CompleteTween = lineStruct.CompleteHint.DOFade(1, 1.5f)
                        .OnComplete(() =>
                        {
                            //UIManager.Instance.HidePanel("DeepAbsorbPanel");
                            lineStruct.CompleteTween = null;
                        }).OnKill(() =>
                        {
                            lineStruct.CompleteTween = null;
                        }).SetLink(gameObject);
                }
                // 否则执行隐藏
                else if(objRuntime.GetCurrProgress() < objRuntime.GetRequireProgress())
                {
                    if (lineStruct.CompleteTween != null)
                    {
                        lineStruct.CompleteTween.Kill();
                        lineStruct.CompleteTween = null;
                    }
                    lineStruct.CompleteHint.gameObject.SetActive(false);
                }
            }

        }

        /// <summary>
        /// 播放步进特效
        /// </summary>
        /// <param name="questId"></param>
        public void UpdateQuestStepView(int questId)
        {
            _flagPendingStep = true;
        }

        /// <summary>
        /// 清理所有view
        /// </summary>
        private void ClearView()
        {
            QuestEmptyHint.gameObject.SetActive(true);
            QuestDetailPanel.gameObject.SetActive(false);
        }
    }



}