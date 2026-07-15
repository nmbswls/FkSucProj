using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using My.Player;
using UnityEngine;

namespace My
{
    public static class DropUtils
    {
        public sealed class DropReward
        {
            public string ItemId;
            public int Amount;
            public ItemInstance4PremiumEssence PremiumEssence;

            public ItemStack CreateItemStack()
            {
                var stack = ItemCatalog.CreateItemStack(ItemId, Amount);
                if (stack != null && PremiumEssence != null)
                {
                    stack.ItemInstanceId = PremiumEssence.InstanceId;
                    stack.InstanceInfo ??= new ItemInstanceInfo();
                    var component = stack.InstanceInfo.GetOrAdd<ItemInstance4PremiumEssence>();
                    component.TypeId = PremiumEssence.TypeId;
                    component.InstanceId = PremiumEssence.InstanceId;
                    component.Concentration = PremiumEssence.Concentration;
                    component.DropLevel = PremiumEssence.DropLevel;
                    component.QualityTier = PremiumEssence.QualityTier;
                }
                return stack;
            }
        }
        //        public List<DropGroup> DropGroups;
        //        public List<DropBundle> DropBundles;

        private static Dictionary<int, List<DropItem>> groupDropItemMap = new();

        public static void InitializeDropGroups()
        {
            groupDropItemMap.Clear();
            var items = CfgMgr.Cfgs?.TbDropItem?.DataList;
            if (items == null)
            {
                return;
            }
            foreach (var item in items)
            {
                if(!groupDropItemMap.TryGetValue(item.GroupId, out var l))
                {
                    l = new();
                    groupDropItemMap[item.GroupId] = l;
                }

                l.Add(item);
            }
        }

        /// <summary>
        /// 掉落
        /// </summary>
        /// <param name="bundleId"></param>
        /// <returns></returns>
        public static List<(string, int)> GetBundleDropItems(int bundleId)
        {
            var bundle = CfgMgr.Cfgs.TbDropBundle.GetOrDefault(bundleId);
            if(bundle == null)
            {
                return new();
            }

            List<(string, int)> retList = new();
            for (int i = 0; i < bundle.DropGroupIds.Count && i < bundle.DropGroupWeights.Count; i++)
            {
                int groupId = bundle.DropGroupIds[i];
                int weight = bundle.DropGroupWeights[i];

                int certainTimes = weight / 10000;
                int leftWeight = weight % 10000;

                int totalTimes = certainTimes;
                var rand = UnityEngine.Random.Range(0, 10000);
                if (rand < leftWeight)
                {
                    totalTimes += 1;
                }

                if (totalTimes <= 0) continue;
                if(totalTimes > 100)
                {
                    Debug.LogError("create drop too many items raw times " + totalTimes);
                    totalTimes = 100;
                }
                groupDropItemMap.TryGetValue(groupId, out var groupItems);
                if (groupItems == null)
                {
                    Debug.LogError("group not found " + groupId);
                    continue;
                }

                int totalWeight = groupItems.Sum(item => item.WeightInGroup);
                if(totalWeight <= 0)
                {
                    Debug.LogError("group totalWeight invalid " + groupId);
                    continue;
                }

                for(int c = 0; c< totalTimes; c++)
                {
                    int randVal = UnityEngine.Random.Range(0, totalWeight);

                    int itWeight = 0;
                    DropItem? choosedData = null;
                    for (int ii = 0; ii < groupItems.Count; ii++)
                    {
                        itWeight += groupItems[ii].WeightInGroup;
                        if (randVal < itWeight)
                        {
                            choosedData = groupItems[ii];
                            break;
                        }
                    }

                    if (choosedData == null)
                    {
                        continue;
                    }

                    for (int ii = 0; ii < choosedData.ItemIdList.Count; ii++)
                    {
                        if (ii * 2 + 1 < choosedData.ItemCountRangeList.Count)
                        {
                            var randNum = UnityEngine.Random.Range(choosedData.ItemCountRangeList[ii * 2], choosedData.ItemCountRangeList[ii * 2 + 1] + 1);
                            retList.Add(new(choosedData.ItemIdList[ii], randNum));
                        }
                        else
                        {
                            Debug.LogError("config err data num not found " + groupId);
                            continue;
                        }
                    }
                }
                

            }

            return retList;
        }

        public static List<DropReward> GetBundleDropRewards(int bundleId)
        {
            var rewards = new List<DropReward>();
            var raw = GetBundleDropItems(bundleId);
            var rows = CfgMgr.Cfgs?.TbDropItem?.DataList;
            foreach (var item in raw)
            {
                var row = rows?.Find(x => x != null && x.ItemIdList != null && x.ItemIdList.Contains(item.Item1));
                if (row == null || row.PremiumEssenceType == EJingYuanType.None)
                {
                    rewards.Add(new DropReward { ItemId = item.Item1, Amount = item.Item2 });
                    continue;
                }

                var dropLevel = ResolveDropLevelFromItemId(item.Item1);
                var min = Math.Max(0, row.PremiumConcentrationMin);
                var max = Math.Max(min, row.PremiumConcentrationMax);
                for (var i = 0; i < item.Item2; i++)
                {
                    var instance = JingYuanEssenceCatalog.CreateInstanceAtLevel(row.PremiumEssenceType, dropLevel, 1, "drop", item.Item1);
                    instance.Concentration = UnityEngine.Random.Range(min, max + 1);
                    rewards.Add(new DropReward { ItemId = item.Item1, Amount = 1, PremiumEssence = new ItemInstance4PremiumEssence
                    {
                        InstanceId = instance.InstanceId,
                        TypeId = instance.TypeId,
                        Concentration = instance.Concentration,
                        DropLevel = instance.DropLevel,
                        QualityTier = 1,
                    }});
                }
            }
            return rewards;
        }

        static int ResolveDropLevelFromItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 1;
            var marker = "_l";
            var index = itemId.LastIndexOf(marker, StringComparison.Ordinal);
            return index >= 0 && int.TryParse(itemId.Substring(index + marker.Length), out var level)
                ? Math.Max(1, level) : 1;
        }
    }
}


