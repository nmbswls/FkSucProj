
using System.Collections.Generic;
using System.Linq;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using TMPro;
using UnityEditorInternal.VersionControl;
using UnityEngine;
using UnityEngine.UI;
using static QuickDebugShow;


namespace My.UI
{

    public class SceneSmallIconLayerPanel : PanelBase, IRefreshable
    {
        public static SceneSmallIconLayerPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("SmallIconLayer");
                if (panel != null && panel is SceneSmallIconLayerPanel sceneSmallIconLayer)
                {
                    return sceneSmallIconLayer;
                }
                return null;
            }
        }

        public QuickDebugShow DebugIconsShower;


        public override void Setup(object data = null)
        {
            //BottomProgressPanel.Setup();
        }

        public void Refresh() { /* 更新任务/提示等 */ }

        public override int FocusPriority => 0;
        public bool OnConfirm() => false;
        public bool OnCancel() => false;
        public bool OnNavigate(Vector2 dir) => false;
        public bool OnHotkey(int index) => false;

        public GameObject InteractHintPrefab;
        public GameObject EvilAlertPrefab;
        private Dictionary<long, SceneInteractUIHinter> sceneInteractHintDicts = new();
        private Queue<SceneInteractUIHinter> _hintPool = new();

        public class SceneAlertUIStruct
        {
            public GameObject Go;
            //public TextMeshProUGUI Val;
            public NpcUnitLogicEntity bindingNpc;
        }
        private Dictionary<long, SceneAlertUIStruct> _evilAlertRecords = new Dictionary<long, SceneAlertUIStruct>();


        public Canvas TopCanvas;
        public void Awake()
        {
            InteractHintPrefab.SetActive(false);
            EvilAlertPrefab.SetActive(false);

            TopCanvas = GetComponentInParent<Canvas>();
        }

        public void Update()
        {
            UpdateSceneEvilAlertUnits();

            LowFreqCleanInvalidEntry();
        }

        private float _lowFreqCleanInvalidTimer;
        private List<long> _lowFreqCleanCaches = new List<long>();

        private void LowFreqCleanInvalidEntry()
        {
            if(LogicTime.time - _lowFreqCleanInvalidTimer < 1.0f)
            {
                return;
            }

            _lowFreqCleanInvalidTimer = LogicTime.time;

            _lowFreqCleanCaches.Clear();

            foreach (var k in _evilAlertRecords.Keys)
            {
                if (_evilAlertRecords[k].bindingNpc == null 
                    || _evilAlertRecords[k].bindingNpc.MarkDestroyed 
                    || _evilAlertRecords[k].bindingNpc.MarkDespawn
                    || !_evilAlertRecords[k].bindingNpc.IsEvilAlert)
                {
                    _lowFreqCleanCaches.Add(k);
                    continue;
                }
            }

            foreach(var oneId in _lowFreqCleanCaches)
            {
                var o = _evilAlertRecords[oneId];
                GameObject.Destroy(o.Go);
                _evilAlertRecords.Remove(oneId);
            }
        }

        protected void UpdateSceneEvilAlertUnits()
        {
            foreach (var k in _evilAlertRecords.Keys)
            {
                if (_evilAlertRecords[k].bindingNpc == null 
                    || _evilAlertRecords[k].bindingNpc.MarkDestroyed 
                    || _evilAlertRecords[k].bindingNpc.MarkDespawn
                    || !_evilAlertRecords[k].bindingNpc.IsEvilAlert)
                {
                    _evilAlertRecords[k].Go.SetActive(false); 
                    continue;
                }

                var o = _evilAlertRecords[k];
                var worldPos = o.bindingNpc.Pos;
                // convert
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

                Vector2 uiLocalPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform.parent as RectTransform,
                    screenPos,
                    TopCanvas.worldCamera,
                    out uiLocalPos
                );
                uiLocalPos += Vector2.up * 20;
                o.Go.transform.localPosition = uiLocalPos;
            }

            var goodEntity = MainGameManager.Instance.gameLogicManager.AreaManager.GetAlertingLogicEntities();
            foreach (var p in goodEntity)
            {
                if(p is not NpcUnitLogicEntity npcEntity)
                {
                    continue;
                }

                long entityId = p.Id;
                if (!_evilAlertRecords.ContainsKey(p.Id))
                {
                    SceneAlertUIStruct newStruct = new();
                    newStruct.Go = GameObject.Instantiate(EvilAlertPrefab, transform);
                    newStruct.Go.SetActive(true);

                    newStruct.bindingNpc = npcEntity;
                    _evilAlertRecords[p.Id] = newStruct;
                }
            }
        }

        public override void Hide()
        {
            base.Hide();

            foreach(var hintItem in sceneInteractHintDicts.Values)
            {
                hintItem.Clear();
                hintItem.gameObject.SetActive(false);

                if (_hintPool.Count < 10)
                {
                    _hintPool.Enqueue(hintItem);
                }
                else
                {
                    GameObject.Destroy(hintItem.gameObject);
                }
            }
            sceneInteractHintDicts.Clear();


            DebugIconsShower.Clear();
        }

        public void OnScenePresentationBinded(IScenePresentation scenePresentation)
        {
            if (scenePresentation is ISceneInteractable interactPoint)
            {

                SceneInteractUIHinter hint = null;
                if (_hintPool.Count > 0)
                {
                    hint = _hintPool.Dequeue();
                }
                else
                {
                    var newHintGo = GameObject.Instantiate(InteractHintPrefab, transform);
                    hint = newHintGo.GetComponent<SceneInteractUIHinter>();
                    newHintGo.SetActive(true);
                }
                hint.InitBind(interactPoint);

                hint.sceneInteract = interactPoint;
                hint.gameObject.SetActive(true);
                sceneInteractHintDicts[interactPoint.Id] = hint;

                hint.transform.position = scenePresentation.GetWorldPosition();
                hint.transform.localPosition = new Vector3(hint.transform.localPosition.x, hint.transform.localPosition.y, 0);
            }
        }

        public void OnScenePresentationUbbind(IScenePresentation scenePresentation)
        {
            if (scenePresentation is ISceneInteractable interactPoint)
            {
                sceneInteractHintDicts.TryGetValue(scenePresentation.Id, out var hintItem);
                if (hintItem != null)
                {
                    hintItem.Clear();
                    hintItem.gameObject.SetActive(false);
                    sceneInteractHintDicts.Remove(scenePresentation.Id);

                    if (_hintPool.Count < 10)
                    {
                        _hintPool.Enqueue(hintItem);
                    }
                    else
                    {
                        GameObject.Destroy(hintItem.gameObject);
                    }
                }
            }
        }
    }

}
