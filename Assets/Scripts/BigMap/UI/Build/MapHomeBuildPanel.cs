using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SuperScrollView;
using Unity.VisualScripting.ReorderableList.Internal;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.UIElements;
using My;

namespace My.UI
{
    // 旧内城建造 UI 已废弃（由 SecretBase 替代），保留组件以免 Prefab 引用丢失。
    public class MapHomeBuildPanel : MonoBehaviour
    {
        public OverworldHUDPanel overworldHUDPanel;
        public BuildPreviewController preview;
        public HomeFacilityCfg? currentPlacement;
        private EPlacementRotation rot = EPlacementRotation.R0;
        public LoopListView2 buildItemsList;
        public RectTransform buildListContent;
        public string buildItemPrefabName;
        public UnityEngine.UI.Button btnOpenTownManagement;

        private int currentIndex = 0;
        private int selectedIndex = -1;
        protected List<HomeFacilityCfg> buildingItemDatas = new();

        public void Awake()
        {
            gameObject.SetActive(false);
            if (btnOpenTownManagement != null)
            {
                btnOpenTownManagement.gameObject.SetActive(false);
            }
        }

        public void InitShow()
        {
            buildingItemDatas.Clear();
            if (buildItemsList != null)
            {
                buildItemsList.SetListItemCount(0, false);
            }
        }

        public void ShowBuildDetails()
        {
        }

        public void ClearBuildSelection()
        {
            selectedIndex = -1;
            currentPlacement = null;
        }

        public void TryConfirmPlace(Vector3 worldPos)
        {
        }

        public void CancelBuildMode()
        {
            currentPlacement = null;
            if (preview != null)
            {
                preview.Show(false);
            }

            if (HomeSceneManager.Instance != null && HomeSceneManager.Instance.previewTilemapController != null)
            {
                HomeSceneManager.Instance.previewTilemapController.Clear();
            }
        }
    }
}
