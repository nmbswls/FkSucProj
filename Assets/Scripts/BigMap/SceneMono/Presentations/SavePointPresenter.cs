using My;
using My.Map;
using My.Map.Entity;
using My.UI;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map.Scene
{
    public class SavePointPresenter : ScenePresentationBase<LogicEntitySavePoint>, ISceneInteractable
    {
        public LogicEntitySavePoint SaveEntity => (LogicEntitySavePoint)_logic;

        public string ShowName => "Save Point";

        public Vector2 Pos => transform.position;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => true;

        public bool CanInteractEnable()
        {
            return _logic != null && !_logic.MarkDestroyed;
        }

        public bool TriggerInteract(int selectionId)
        {
            if (selectionId != 1)
            {
                return false;
            }

            LogicTime.RequestPause("SavePoint");
            var panel = UIManager.Instance.ShowPanel("SavePointPanel") as SavePointPanel;
            if (panel != null)
            {
                panel.BeginFlow();
            }
            else
            {
                Debug.LogError("[SavePointPresenter] SavePointPanel not registered or prefab missing.");
                LogicTime.ReleasePause("SavePoint");
            }

            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            return new List<SceneInteractSelection>
            {
                new SceneInteractSelection
                {
                    SelectId = 1,
                    SelectContent = "Save",
                    Selectable = true,
                },
            };
        }

        public float GetHintOffsetInfos()
        {
            return -1f;
        }

        public bool IsAutoInteract()
        {
            return false;
        }
    }
}
