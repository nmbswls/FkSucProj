
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


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
        private Dictionary<long, SceneInteractUIHinter> sceneInteractHintDicts = new(0);
        private Queue<SceneInteractUIHinter> _hintPool = new();

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
                }
                hint.InitBind(interactPoint);

                hint.BindInteractPoint = interactPoint;
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
