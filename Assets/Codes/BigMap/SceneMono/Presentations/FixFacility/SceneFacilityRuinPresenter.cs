using DG.Tweening;
using Map.Entity;
using My.Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace My.Map.Scene
{
    public class SceneFacilityRuinPresenter : ScenePresentationBase<LogicEntityFacilityRuin>, ISubInteractHolder
    {
        public LogicEntityFacilityRuin FacilityRuinEntity { get { return (LogicEntityFacilityRuin)_logic; } }

        public GameObject MainBlock;

        public Transform ViewRoot;
        [SerializeField]
        private SpriteRenderer[] _sprites;

        /// <summary>
        /// handle 处理者
        /// </summary>
        public SubInteractHandle[] Handles;

        protected override void Awake()
        {
            base.Awake();

            if (Handles != null)
            {
                foreach (var handle in Handles)
                {
                    handle.Owner = this;
                }
            }
        }

        [ContextMenu("Auto Collect Child Sprites")]
        private void CollectSprites()
        {
            _sprites = ViewRoot.GetComponentsInChildren<SpriteRenderer>(true);

            Debug.Log($"已收集 {_sprites.Length} 个 SpriteRenderer");

            // 标记对象已修改，确保 Unity 保存这个列表，否则重启后会丢失
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        public override void Tick(float dt)
        {
            base.Tick(dt);

        }

        public override void Bind(ILogicEntity logic)
        {
            base.Bind(logic);

            if(FacilityRuinEntity.IsRepaired)
            {
                if (_sprites != null)
                {
                    foreach (var view in _sprites)
                    {
                        view.color = new Color(view.color.r, view.color.g, view.color.b, 0);
                    }
                }

                if (MainBlock != null)
                {
                    MainBlock.gameObject.SetActive(false);
                }
            }
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            FacilityRuinEntity.EventOnRepaired += OnRuinRepaired;
        }

        protected override void UnregisterEvents()
        {
            base.UnregisterEvents();

            FacilityRuinEntity.EventOnRepaired -= OnRuinRepaired;
        }

        protected override void OnEventEntityDestroyed(long entityId)
        {

        }


        private void OnRuinRepaired()
        {

            MainGameManager.Instance.ShowFakeFxEffect("修好了", this.transform.position);

            if (_sprites != null)
            {
                foreach (var view in _sprites)
                {
                    view.DOFade(0, 0.3f);
                }
            }
            
            if (MainBlock != null)
            {
                MainBlock.gameObject.SetActive(false);
            }
        }

        public bool CanSubInteractEnable(int subIdx)
        {
            if(FacilityRuinEntity.IsRepaired)
            {
                return false;
            }

            if (FacilityRuinEntity.Cfg.AutoRepair)
            {
                return false;
            }

            return true;
        }

        public List<SceneInteractSelection> GetSubInteractSelections(int subIdx)
        {
            var ret = new List<SceneInteractSelection>();

            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "修复",
            });
            return ret;
        }

        public bool SubTriggerInteract(int subIdx, int selectionId)
        {
            FacilityRuinEntity.TryManualRepair();
            return true;
        }
    }
}

