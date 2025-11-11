
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

        public QuickHudShow DebugIconsShower;

        public SceneInteractUIHinter InteractHinter;


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

    }

}
