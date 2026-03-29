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

namespace My.UI
{


    
    public class MapHomeBuildPanel : MonoBehaviour
    {

        public OverworldHUDPanel overworldHUDPanel;

        
        public BuildPreviewController preview;


        public HomeFacilityCfg? currentPlacement;
        private EPlacementRotation rot = EPlacementRotation.R0;

        public LoopListView2 buildItemsList;
        public RectTransform buildListContent;
        public string buildItemPrefabName;

        private int currentIndex = 0;   // 鼠标滚轮移动的当前项
        private int selectedIndex = -1; // 确认选择项


        protected List<HomeFacilityCfg> buildingItemDatas = new();

        public void Awake()
        {
            buildItemsList.InitListView(0, OnGetBuildItemByIndex);
        }

        public void InitShow()
        {
            buildingItemDatas = MainGameManager.Instance.gameLogicManager.homeDataManager.GetAllBuilableItems();
            
            currentIndex = Mathf.Clamp(0, 0, Mathf.Max(0, buildingItemDatas.Count - 1));
            selectedIndex = -1;

            buildItemsList.SetListItemCount(buildingItemDatas.Count, false);
            buildItemsList.RefreshAllShownItem();
        }


        public void ShowBuildDetails()
        {

        }

        // 外部调用：清除选中（允许没有任何选中）
        public void ClearBuildSelection()
        {
            if (selectedIndex == -1) return;
            int old = selectedIndex;
            selectedIndex = -1;
            RefreshItem(old);
            NotifySelectionChanged(-1, null);
        }

        private LoopListViewItem2 OnGetBuildItemByIndex(LoopListView2 view, int index)
        {
            if (index < 0 || index >= buildingItemDatas.Count) return null;

            var item = view.NewListViewItem(buildItemPrefabName);
            var viewComp = item.GetComponent<MapHomeBuildItem>();
            if (viewComp == null)
            {
                Debug.LogError("OnGetBuildItemByIndex missing on prefab");
                return item;
            }

            // 绑定数据
            var data = buildingItemDatas[index];
            bool isSelected = (index == selectedIndex);
            viewComp.Bind(data, isSelected);

            // 注册点击事件（避免重复注册，先清空再加）
            viewComp.onClick = () => OnBuildItemClicked(index);

            return item;
        }
        private void OnBuildItemClicked(int index)
        {
            if (index < 0 || index >= buildingItemDatas.Count) return;

            // 如果点击的是已选中项，则取消选中（支持“最多一个选中”，也支持“可无选中”）
            if (selectedIndex == index)
            {
                ClearBuildSelection();
                return;
            }

            int old = selectedIndex;
            selectedIndex = index;

            // 刷新旧项和新项的外观
            RefreshItem(old);
            RefreshItem(selectedIndex);

            // 通知业务/管理器
            NotifySelectionChanged(selectedIndex, buildingItemDatas[selectedIndex]);
        }

        // 刷新指定 index 的可视项（如果当前在视窗中）
        private void RefreshItem(int index)
        {
            if (index < 0) return;
            var item = buildItemsList.GetShownItemByItemIndex(index);
            if (item == null) return; // 不在可视范围，无需刷新
            var viewComp = item.GetComponent<MapHomeBuildItem>();
            if (viewComp != null)
            {
                bool isSelected = (index == selectedIndex);
                viewComp.SetSelected(isSelected);
            }
        }
        

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="index"></param>
        /// <param name="data"></param>
        private void NotifySelectionChanged(int index, HomeFacilityCfg data)
        {
            
            Debug.Log($"MapHomeBuildPanel Selection changed: index={index}, data={(data == null ? "null" : data.name)}");

            currentPlacement = data;
            if(data != null)
            {
                preview.Show(true);
                preview.InitPreview(data);
            }
            else
            {
                rot = EPlacementRotation.R0;
            }
        }




        void Update()
        {
            if (currentPlacement == null) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            mouseWorld.z = 0;
            
            var cell = HomeSceneManager.Instance.DataSource.WorldToCell(mouseWorld);
            var worldPivot = HomeSceneManager.Instance.DataSource.CellToWorld(cell);

            
            bool valid = HomeSceneManager.Instance.CanPlace(currentPlacement, rot, cell);
            preview.UpdatePreview(valid, worldPivot);

            if(HomeSceneManager.Instance != null)
            {
                var offsetCelss = currentPlacement.GetFootprint(rot);
                List<Vector3Int> cells = new();
                foreach(var offset in offsetCelss)
                {
                    cells.Add(new Vector3Int(cell.x + offset.x, cell.y + offset.y, 0));
                }
                HomeSceneManager.Instance.previewTilemapController.DrawCells(cells, valid);
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) 
            { 
                rot = Next(rot);
                preview.RefreshRotation(rot);
            }

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (valid) HomeSceneManager.Instance.TryPlace(currentPlacement, rot, cell, false);
            }
        }

        EPlacementRotation Next(EPlacementRotation r) => (EPlacementRotation)(((int)r + 1) % 4);

        public void TryConfirmPlace(Vector3 worldPos)
        {
            var cell = HomeSceneManager.Instance.DataSource.WorldToCell(worldPos);
            bool valid = HomeSceneManager.Instance.CanPlace(currentPlacement, rot, cell);

            if (valid) HomeSceneManager.Instance.TryPlace(currentPlacement, rot, cell, false);
        }

        public void CancelBuildMode()
        {
            currentPlacement = null;
            preview.Show(false);

            HomeSceneManager.Instance.previewTilemapController.Clear();
        }
    }
}

