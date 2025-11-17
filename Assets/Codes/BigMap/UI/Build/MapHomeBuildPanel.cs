using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SuperScrollView;
using Unity.VisualScripting.ReorderableList.Internal;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace My.UI
{


    
    public class MapHomeBuildPanel : MonoBehaviour
    {

        public OverworldHUDPanel overworldHUDPanel;

        
        public BuildPreviewController preview;


        public HomePlaceableObject? currentPlacement;
        private EPlacementRotation rot = EPlacementRotation.R0;

        public LoopListView2 buildItemsList;
        public List<string> buildableItems = new List<string>();
        public string buildItemPrefabName;

        private int currentIndex = 0;   // 鼠标滚轮移动的当前项
        private int selectedIndex = -1; // 按F确认后的选中项（-1 表示尚未确认）

        public void Awake()
        {
            buildItemsList.InitListView(0, OnGetBuildItemByIndex);
        }

        public void InitShow()
        {

        }


        public void ShowBuildDetails()
        {

        }

        private LoopListViewItem2 OnGetBuildItemByIndex(LoopListView2 view, int index)
        {
            if (index < 0 || index >= buildableItems.Count) return null;

            var item = view.NewListViewItem(buildItemPrefabName);
            var viewComp = item.GetComponent<UISceneInteractMenu4ChooseItem>();
            //if (viewComp == null)
            //{
            //    Debug.LogError("TabItemView missing on prefab");
            //    return item;
            //}

            //bool isCurrent = (index == currentIndex);
            //bool isSelected = (index == selectedIndex);
            //viewComp.Bind(data[index].Item2, isCurrent, isSelected, data[index].Item3);

            return item;
        }




        void Update()
        {
            if (currentPlacement == null) return;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            mouseWorld.z = 0;
            
            var cell = HomeSceneManager.Instance.WorldToCell(mouseWorld);
            var worldPivot = HomeSceneManager.Instance.CellToWorld(cell);

            
            bool valid = HomeSceneManager.Instance.CanPlace(currentPlacement, rot, cell);
            preview.UpdatePreview(valid, worldPivot);

            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) rot = Next(rot);

            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                if (valid) HomeSceneManager.Instance.TryPlace(currentPlacement, rot, cell, false);
            }
            if (UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                CancelBuildMode();
                overworldHUDPanel.QuitBuildMode();
            }
        }

        

        EPlacementRotation Next(EPlacementRotation r) => (EPlacementRotation)(((int)r + 1) % 4);

        


        void CancelBuildMode()
        {
            currentPlacement = null;
            preview.gameObject.SetActive(false);
        }
    }
}

