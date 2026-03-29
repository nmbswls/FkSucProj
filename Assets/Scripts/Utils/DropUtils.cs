using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My
{
    public static class DropUtils
    {
        //        public List<DropGroup> DropGroups;
        //        public List<DropBundle> DropBundles;

        private static Dictionary<int, List<DropItem>> groupDropItemMap = new();

        public static void InitializeDropGroups()
        {
            var items = CfgMgr.Cfgs.TbDropItem!.DataList;
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
        /// µôÂä
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
    }
}


