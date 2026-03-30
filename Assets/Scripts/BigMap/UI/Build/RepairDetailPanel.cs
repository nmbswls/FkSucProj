
using My.Map;
using My.Map.Scene;
using UnityEngine;
using static UnityEditor.Progress;

namespace My.UI
{

    public class RepairDetailPanel : PanelBase
    {

        public static RepairDetailPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("RepairDetailPanel");
                if (panel != null && panel is RepairDetailPanel realPanel)
                {
                    return realPanel;
                }
                return null;
            }
        }

        public GameObject RequireLineTemplate;
        public Transform RequireContainer;

        public SceneFacilityRuinPresenter _bindFacilityRuinPresenter;

        private void Awake()
        {
            RequireLineTemplate.gameObject.SetActive(false);
        }


        private void Update()
        {
            if(_bindFacilityRuinPresenter != null)
            {
                var screenPos = Camera.main.WorldToScreenPoint(_bindFacilityRuinPresenter.HintPivot.position);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    transform.parent as RectTransform,
                    screenPos,
                    UIManager.Instance.RootCanvas.worldCamera, // 注意 Canvas 模式，如果是 Overlay 这里传 null
                    out Vector2 uiLocalPos
                );
                uiLocalPos += Vector2.up * 20f;
                this.transform.localPosition = uiLocalPos;
            }
            
        }

        public void UpdateBind(SceneFacilityRuinPresenter ruinPresenter)
        {
            _bindFacilityRuinPresenter = ruinPresenter;


            for (int i = 0; i < RequireContainer.childCount; i++)
            {
                GameObject.Destroy(RequireContainer.GetChild(i));
            }

            for (int i=0;i<3;i++)
            {
                GameObject o = GameObject.Instantiate(RequireLineTemplate, RequireContainer);
            }


            var screenPos = Camera.main.WorldToScreenPoint(_bindFacilityRuinPresenter.HintPivot.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform.parent as RectTransform,
                screenPos,
                UIManager.Instance.RootCanvas.worldCamera, // 注意 Canvas 模式，如果是 Overlay 这里传 null
                out Vector2 uiLocalPos
            );
            uiLocalPos += Vector2.up * 20f;
            this.transform.localPosition = uiLocalPos;
        }

        public override void Hide()
        {
            base.Hide();
            _bindFacilityRuinPresenter = null;
        }
    }

}
