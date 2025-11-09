using Bag;
using SuperScrollView;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AnyContainerItemCell;


public class LootPointUIController : MonoBehaviour
{
    public static LootPointUIController Instance;

    public ILootableObj Loot;

    public LoopGridView GridView;

    public Button QuitBtn;

    [Range(1, 10)]
    public int Columns = 5;
    public string ItemPrefabName = "ItemCellPrefab";

    void Awake()
    {
        Instance = this;

        QuitBtn.onClick.AddListener(() => { MainUIManager.Instance.TryQuitLootDetailMode(); });
        GridView.InitGridView(0, OnGetItemByIndex);

        this.gameObject.SetActive(false);
    }

    public void InitializeContent(ILootableObj bindingObj)
    {
        this.Loot = bindingObj;
        GridView.SetListItemCount(bindingObj.LootItems.Count + 1);
    }

    public void RefreshContent()
    {
        GridView.RefreshAllShownItem();
    }

    LoopGridViewItem OnGetItemByIndex(LoopGridView grid, int itemIndex, int row, int column)
    {
        // 注意：部分版本是 OnGetItemByRowColumn 回调签名不同，按你的 API 改名
        // itemIndex = 行序号（row），列用 column 参数
        var item = grid.NewListViewItem(ItemPrefabName);
        var cell = item.GetComponent<AnyContainerItemCell>();

        int slotIndex = row * Columns + column;
        if(slotIndex < Loot.LootItems.Count)
        {
            var stack = Loot.LootItems[slotIndex];
            item.gameObject.SetActive(true);
            cell.Bind(stack, slotIndex, EContainerType.LootPoint, null);
        }
        else
        {
            cell.ClearEmpty(slotIndex, EContainerType.LootPoint);
            //item.gameObject.SetActive(false);
        }

        return item;
    }
}