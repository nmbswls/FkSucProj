
using System.Collections.Generic;
using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using UnityEditor;
using UnityEngine;

namespace My
{
    /// <summary>
    /// 建筑物
    /// </summary>
    public class HomeFacilityPresenter : ScenePresentationBase<HomeFacilityLogicEntity>, ISubInteractHolder
    {

        public HomeFacilityLogicEntity FacilityEntity { get { return (HomeFacilityLogicEntity)_logic; } }

        public Transform ViewRoot;
        [SerializeField]
        private SpriteRenderer[] _sprites;

        /// <summary>
        /// 
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

        public bool CanSubInteractEnable(int subIdx)
        {
            return true;
        }

        public List<SceneInteractSelection> GetSubInteractSelections(int subIdx)
        {
            var ret = new List<SceneInteractSelection>();

            ret.Add(new SceneInteractSelection()
            {
                SelectId = 1,
                SelectContent = "查看",
            });

            return ret;
        }

        public bool SubTriggerInteract(int subIdx, int selectionId)
        {
            MainGameManager.Instance.ShowMapSpeachBubble(MainGameManager.Instance.playerScenePresenter.Id, $"我是{FacilityEntity.InnerFacilityRef.Id}。", 1f);
            return true;
        }
    }
}