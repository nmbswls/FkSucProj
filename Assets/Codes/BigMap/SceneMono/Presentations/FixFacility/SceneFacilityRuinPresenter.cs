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
    public class SceneFacilityRuinPresenter : ScenePresentationBase<LogicEntityFacilityRuin>, ISceneInteractable
    {
        public LogicEntityFacilityRuin FacilityRuinEntity { get { return (LogicEntityFacilityRuin)_logic; } }

        public string ShowName => "废墟";

        public Vector2 Pos => transform.position;

        public GameObject MainBlock;

        public Transform ViewRoot;
        [SerializeField]
        private SpriteRenderer[] _sprites;

        
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



        public bool CanInteractEnable()
        {
            if(FacilityRuinEntity.IsRepaired)
            {
                return false;
            }

            if(FacilityRuinEntity.Cfg.AutoRepair)
            {
                return false;
            }

            return true;
        }

        public bool TriggerInteract(int selectionId)
        {
            if (FacilityRuinEntity.Cfg.AutoRepair)
            {
                MainGameManager.Instance.ShowMapSpeachBubble(FacilityRuinEntity.Id, "没修好。", 1f);
            }
            else
            {
                FacilityRuinEntity.TryManualRepair();
            }

            return true;
        }

        public Vector3 GetHintAnchorPosition()
        {
            return transform.position;
        }

        public List<SceneInteractSelection> GetInteractSelections()
        {
            var ret = new List<SceneInteractSelection>();


            if (FacilityRuinEntity.Cfg.AutoRepair)
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "查看",
                });
            }
            else
            {
                ret.Add(new SceneInteractSelection()
                {
                    SelectId = 1,
                    SelectContent = "修理",
                });
            }

            return ret;
        }

        public float GetHintOffsetInfos()
        {
            return -1;
        }
    }
}

