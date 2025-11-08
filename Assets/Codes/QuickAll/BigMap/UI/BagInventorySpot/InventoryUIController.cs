using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.Port;
using UnityEngine.UIElements;
using Bag;
using static AnyContainerItemCell;
using static UnityEngine.Rendering.DebugUI.Table;

public class InventoryUIController : MonoBehaviour
{
    public LoopGridView GridView;
    [Range(1, 10)]
    public int Columns = 5;
    public string ItemPrefabName = "ItemCellPrefab";

    private int Capacity => BindingInventory.Slots.Count;

    public PlayerInventoryModel BindingInventory { get { return MainGameManager.Instance.gameLogicManager.playerDataManager.inventoryModel; } }
    public static InventoryUIController Instance;

    private void Awake()
    {
        Instance = this;
        GridView.SetGridFixedGroupCount(GridFixedType.ColumnCountFixed, Columns);
        GridView.InitGridView(0, OnGetItemByIndex);

        gameObject.SetActive(false);

    }

    public void InitilaizeView()
    {
        GridView.SetListItemCount(BindingInventory.Slots.Count);
    }

    void OnDestroy()
    {
    }

    private void OnInventoryChanged(int idx)
    {
        GridView.RefreshAllShownItem(); // 简化，实际可局部刷新
    }

    private void OnInventoryAllChanged()
    {
        GridView.SetListItemCount(BindingInventory.Slots.Count);
        GridView.RefreshAllShownItem();
    }

    LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
    {
        // 注意：部分版本是 OnGetItemByRowColumn 回调签名不同，按你的 API 改名
        // itemIndex = 行序号（row），列用 column 参数
        var item = grid.NewListViewItem(ItemPrefabName);
        var cell = item.GetComponent<AnyContainerItemCell>();

        //int slotIndex = row * Columns + column;

        if (itemIndex < Capacity)
        {
            var stack = BindingInventory.Slots[itemIndex];
            item.gameObject.SetActive(true);
            cell.Bind(stack, itemIndex, EContainerType.Inventory, null);
        }
        else
        {
            // 超范围：隐藏或者用空数据绑定
            item.gameObject.SetActive(false);
        }
        return item;
    }

    public void UseItem(int index)
    {
        var stack = BindingInventory.Slots[index];
        if (stack == null || stack.IsEmpty) return;
        if (!FakeItemDatabase.CanUse(stack.ItemID)) return;

        stack.RemoveFromStack(1);
        if (stack.Count <= 0) BindingInventory.Slots[index] = null;

        MainGameManager.Instance.gameLogicManager.playerLogicEntity.PlayerAbilityController.TryUseItem(stack.ItemID);

        //UIBus.RaiseInventoryChanged(index);
        OnInventoryAllChanged();
    }

    public void SplitItem(int index, int count)
    {
        if (BindingInventory.TrySplit(index, count))
        {
            //UIBus.RaiseInventoryAllChanged();
        }

        OnInventoryAllChanged();
    }

    public void DropItemToGround(int index, int count)
    {
        BindingInventory.RemoveAt(index, count);
        // 这里可生成场景掉落物，本示例仅移除
        //UIBus.RaiseInventoryAllChanged();

        OnInventoryAllChanged();
    }
}
