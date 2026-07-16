using System;
using System.Collections.Generic;
using My.Map.Entity;
using My.UI;
using UnityEngine;

namespace My.Map.Scene
{
    // 玩家放置的运输物资堆：独立外观与「存放」交互，不走搜刮点解锁/读条流程
    public class TransportMarkerPresenter : ScenePresentationBase<TransportLootPointLogicEntity>, ISceneInteractable
    {
        const string ShowNameText = "物资堆";

        [SerializeField] SpriteRenderer pileRenderer;
        [SerializeField] SpriteRenderer flagRenderer;
        [SerializeField] SpriteRenderer glowRenderer;

        float _pulseTime;

        public Vector2 Pos => transform.position;

        public event Action<bool> EventOnInteractStateChanged;

        public string ShowName => ShowNameText;

        public bool InteractFocused { get; set; }
        public bool IsInteractDetail { get; set; }
        public bool WithInteractDetail => true;

        public override void ApplyState(object state)
        {
            if (state is InteractPointState s)
            {
                transform.position = s.Position;
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);
            RefreshGlow();
        }

        void RefreshGlow()
        {
            if (glowRenderer == null)
            {
                return;
            }

            glowRenderer.gameObject.SetActive(true);
            _pulseTime += LogicTime.deltaTime;
            bool hasItems = _logic != null && _logic.HasAnyItem();
            float alpha = hasItems
                ? 0.35f + Mathf.Abs(Mathf.Sin(_pulseTime * 2f)) * 0.22f
                : 0.16f;
            var color = glowRenderer.color;
            color.a = alpha;
            glowRenderer.color = color;
        }

        public bool TriggerInteract(int selectionId, int playerId)
        {
            if (selectionId != 2 || _logic == null)
            {
                return false;
            }

            UIOrchestrator.Instance.TryEnterLootDetailMode(_logic);
            return true;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            return new List<SceneInteractSelection>
            {
                new SceneInteractSelection
                {
                    SelectId = 2,
                    SelectContent = "存放",
                    Selectable = true,
                },
            };
        }

        public bool CanInteractEnable() => true;

        public bool IsAutoInteract() => false;

        public Vector3 GetHintAnchorPosition()
        {
            return GetWorldPosition() + new Vector3(0f, 0.22f, 0f);
        }

        public float GetHintOffsetInfos() => -1f;
    }
}
