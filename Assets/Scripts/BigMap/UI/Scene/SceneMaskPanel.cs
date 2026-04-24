using My.Map.View;
using UnityEngine;

namespace My.UI
{
    public class SceneMaskPanel : PanelBase
    {
        public static SceneMaskPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("SceneMask");
                if (panel != null && panel is SceneMaskPanel retPanel)
                {
                    return retPanel;
                }
                return null;
            }
        }

        public GameObject NightOverlay;
        public GameObject DawnOnverlay;

        public GameObject ViewMask;

        public void ShowDayTime()
        {
            NightOverlay.SetActive(false);
            DawnOnverlay.SetActive(false);
            ViewMask.SetActive(false);
        }

        public void ShowDawn()
        {
            NightOverlay.SetActive(false);
            DawnOnverlay.SetActive(true);

            ViewMask.SetActive(false);
        }

        public void ShowHunting()
        {
            NightOverlay.SetActive(true);
            DawnOnverlay.SetActive(false);

            ViewMask.SetActive(true);
        }
    }
}
