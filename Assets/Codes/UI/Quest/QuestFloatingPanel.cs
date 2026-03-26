
using System.Collections.Generic;
using DG.Tweening;
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
        }
        public List<QuestObjectiveLine> ObjectiveLines = new();

        private QuestInstance _bindingQuestInst;
        private string _bindingStepId;

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


            if(_bindingQuestInst == null && markQuestId != 0)
            {
                RefreshBindingQuest();
            }

            if(_bindingQuestInst != null && _bindingQuestInst.cacheCfg.QuestId != markQuestId)
            {
                RefreshBindingQuest();
            }


            if (_flagPendingStep && _fadeQuestCounter == 0)
            {
                UpdateQuestView();
                _flagPendingStep = false;
            }
        }


        public override void Show()
        {
            base.Show();

            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestObjUpdate += UpdateQuestDetailView;
            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestStepUpdate += UpdateQuestStepView;
        }

        public override void Hide()
        {
            base.Hide();

            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestObjUpdate -= UpdateQuestDetailView;
            MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem.EventOnQuestStepUpdate -= UpdateQuestStepView;
        }

        public void RefreshBindingQuest()
        {
            var questSys = MainGameManager.Instance.gameLogicManager.playerDataManager.QuestSystem;
            int markQuestId = questSys.MarkQuestId;
            if(markQuestId == 0)
            {
                ClearView();
                return;
            }

            _bindingQuestInst = questSys.GetQuest(markQuestId);

            if(_bindingQuestInst == null)
            {
                ClearView();
                return;
            }

            UpdateQuestView();
        }

        private void OnQuestUpdate(int questId)
        {
            if(_bindingQuestInst != null && questId == _bindingQuestInst.cacheCfg.QuestId)
            {
                UpdateQuestView();
            }
        }

        public void UpdateQuestView()
        {
            QuestEmptyHint.gameObject.SetActive(false);

            QuestTitle.text = _bindingQuestInst.cacheCfg.Name;
            QuestDetailPanel.gameObject.SetActive(true);

            if(_bindingQuestInst.ActiveStep == null)
            {
                foreach(var oneLine in ObjectiveLines)
                {
                    oneLine.RootGo.SetActive(false);
                }
            }
            else
            {
                for (int i=0;i< _bindingQuestInst.ActiveStep.ObjectiveRuntimes.Length; i++)
                {
                    var obj = _bindingQuestInst.ActiveStep.ObjectiveRuntimes[i];

                    ObjectiveLines[i].RootGo.SetActive(true);
                }
            }
        }


        private int _fadeQuestCounter;
        private bool _flagPendingStep = false;
        public void UpdateQuestDetailView(int questId)
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
                ObjectiveLines[i].ObjDesc.text = objRuntime.Data.ToString() + objRuntime.GetCurrProgress() + "/" + objRuntime.GetRequireProgress();

                if(objRuntime.GetCurrProgress() >= objRuntime.GetRequireProgress())
                {
                    ObjectiveLines[i].CompleteHint.gameObject.SetActive(true);
                    ObjectiveLines[i].CompleteHint.color = new Color(ObjectiveLines[i].CompleteHint.color.r, ObjectiveLines[i].CompleteHint.color.g, ObjectiveLines[i].CompleteHint.color.b, 0);
                    _fadeQuestCounter += 1;
                    ObjectiveLines[i].CompleteHint.DOFade(1, 0.5f)
                        .OnComplete(() =>
                    {
                        //UIManager.Instance.HidePanel("DeepAbsorbPanel");
                        _fadeQuestCounter -= 1;
                    }).OnKill(() =>
                    {
                        _fadeQuestCounter -= 1;
                    });
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