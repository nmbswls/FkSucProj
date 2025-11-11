using SuperScrollView;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;



namespace My.UI
{
    public class UISceneInteractMenu4Choose : MonoBehaviour
    {
        [Header("SuperScrollView")]
        public LoopListView2 listView;          // 拖入场景中的 LoopListView2
        public RectTransform viewport;          // ScrollRect 的 Viewport，控制可见高度=5*itemHeight
        public float itemHeight = 20f;          // 与Prefab高度一致
        public string itemPrefabName = "TabItem"; // 在 SuperScrollView 的 ItemPrefabMgr 里注册的名字

        [Header("Data")]
        public List<string> data = new List<string>();


        public RectTransform ScrollView;
        private int currentIndex = 0;   // 鼠标滚轮移动的当前项
        private int selectedIndex = -1; // 按F确认后的选中项（-1 表示尚未确认）

        public int CurrentIndex { get { return currentIndex; } }

        public event Action<int> EvOnTabConfirmed;
        public event Action EvOnCanceled;

        private void Awake()
        {
            ScrollView = transform as RectTransform;

            // 限制只显示5个：设置 viewport 高度
            //if (viewport != null)
            //{
            //    var size = viewport.sizeDelta;
            //    size.y = 5 * itemHeight;
            //    viewport.sizeDelta = size;
            //}

            // 初始化 ListView
            listView.InitListView(data.Count, OnGetItemByIndex);
            // 初始居中显示 currentIndex
            ScrollToCenter(currentIndex);
        }

        private void Update()
        {
        }



        public void MoveCursor(int delta)
        {
            if (data.Count == 0) return;
            int newIndex = Mathf.Clamp(currentIndex + delta, 0, data.Count - 1);
            if (newIndex == currentIndex) return;

            currentIndex = newIndex;
            // 刷新可见项外观
            RefreshVisibleItems();
            // 将当前项滚动至中间（5个视窗的第3个位置）
            ScrollToCenter(currentIndex);
        }

        //private void ConfirmCurrent()
        //{
        //    selectedIndex = currentIndex;
        //    RefreshVisibleItems();

        //    // 如需对外发事件，可在此回调
        //    EvOnTabConfirmed?.Invoke(selectedIndex);
        //}

        //private void Cancel()
        //{
        //    // 如需对外发事件，可在此回调
        //    EvOnCanceled?.Invoke();
        //}

        // SuperScrollView 回调：为给定 index 提供/刷新 item
        private LoopListViewItem2 OnGetItemByIndex(LoopListView2 view, int index)
        {
            if (index < 0 || index >= data.Count) return null;

            var item = view.NewListViewItem(itemPrefabName);
            var viewComp = item.GetComponent<UISceneInteractMenu4ChooseItem>();
            if (viewComp == null)
            {
                Debug.LogError("TabItemView missing on prefab");
                return item;
            }

            bool isCurrent = (index == currentIndex);
            bool isSelected = (index == selectedIndex);
            viewComp.Bind(data[index], isCurrent, isSelected);

            // 固定高度（与 itemHeight 保持一致）
            var rt = item.GetComponent<RectTransform>();
            if (rt) rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemHeight);

            return item;
        }

        private void RefreshVisibleItems()
        {
            int count = listView.ShownItemCount;
            for (int i = 0; i < count; i++)
            {
                var item = listView.GetShownItemByIndex(i);
                if (item == null) continue;
                int idx = item.ItemIndex;
                if (idx < 0 || idx >= data.Count) continue;

                var viewComp = item.GetComponent<UISceneInteractMenu4ChooseItem>();
                if (viewComp == null) continue;

                bool isCurrent = (idx == currentIndex);
                bool isSelected = (idx == selectedIndex);
                viewComp.Bind(data[idx], isCurrent, isSelected);
            }
        }

        // 将指定 index 的项滚动至“中间位置”
        private void ScrollToCenter(int index)
        {
            if (data.Count == 0) return;

            // 5 个可视项，中间是第 3 个（0-based：位置2）
            int targetViewRow = 2;
            // 计算使 index 在 viewport 的目标行的位置所需的滚动偏移（单位：itemHeight）
            // SuperScrollView 使用 Normalize pos 或 MovePanelToItemIndex 的接口
            // 这里使用 MovePanelToItemIndex(index, offset)
            // offset：让 index 的 item 顶部相对于 viewport 顶部的像素偏移
            float offset = targetViewRow * itemHeight;
            listView.MovePanelToItemIndex(index, offset);
        }

        public int SelectedIndex => selectedIndex;

        // 动态设置数据并重建
        public void SetData(List<string> newData, int initialIndex = 0)
        {
            data = newData ?? new List<string>();
            currentIndex = Mathf.Clamp(initialIndex, 0, Mathf.Max(0, data.Count - 1));
            selectedIndex = -1;
            listView.SetListItemCount(data.Count, false);
            listView.RefreshAllShownItem();
            ScrollToCenter(currentIndex);

            this.ScrollView.sizeDelta = new(this.ScrollView.sizeDelta.x, data.Count * itemHeight);
        }

    }

}
