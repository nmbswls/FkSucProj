using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Config
{

    [CreateAssetMenu(menuName = "GP/Config/GlobalDropTable")]
    [Serializable]
    public class GlobalDropTable : ScriptableObject
    {

        [Serializable]
        public class OneDropData
        {
            public List<string> ItemIds;// n
            public List<int> DropNums; // 2*n

            public int ChckLevelNeed; //  
            public int Weight = 1;
        }

        [Serializable]
        public class DropGroup
        {
            public string DropGroupId;
            public List<OneDropData> DropDatas;
        }

        [Serializable]
        public class DropBundle
        {
            public string BundleId;
            public List<string> DropGroups;
            public List<int> DropWrights;
        }


        public List<DropGroup> DropGroups;
        public List<DropBundle> DropBundles;

        private Dictionary<string, DropGroup> runtimeGroupMap = null;
        private Dictionary<string, DropBundle> runtimeBundleMap = null;

        /// <summary>
        /// µôÂä
        /// </summary>
        /// <param name="bundleId"></param>
        /// <returns></returns>
        public List<(string, int)> GetBundleDropItems(string bundleId)
        {
            if(runtimeGroupMap == null)
            {
                runtimeGroupMap = new();
                foreach (var group in DropGroups)
                {
                    runtimeGroupMap[group.DropGroupId] = group;
                }
            }

            if(runtimeBundleMap == null)
            {
                runtimeBundleMap = new();
                foreach (var b in DropBundles)
                {
                    runtimeBundleMap[b.BundleId] = b;
                }
            }

            runtimeBundleMap.TryGetValue(bundleId, out var bundle);
            if(bundle == null)
            {
                return new();
            }

            List<(string, int)> retList = new();
            for(int i=0;i<bundle.DropGroups.Count && i < bundle.DropWrights.Count; i++)
            {
                string groupId = bundle.DropGroups[i];
                int weight = bundle.DropWrights[i];

                var rand = UnityEngine.Random.Range(0, 10000);
                if(rand >= weight)
                {
                    continue;
                }

                runtimeGroupMap.TryGetValue(groupId, out var group);
                if (group == null)
                {
                    Debug.LogError("group not found " + groupId);
                    continue;
                }

                int totalWeight = group.DropDatas.Sum(item => item.Weight);
                int randVal = UnityEngine.Random.Range(0, totalWeight);

                int itWeight = 0;
                OneDropData? choosedData = null;
                for (int ii=0;ii<group.DropDatas.Count;ii++)
                {
                    itWeight += group.DropDatas[ii].Weight;
                    if(randVal < itWeight)
                    {
                        choosedData = group.DropDatas[ii];
                        break;
                    }
                }

                if(choosedData == null)
                {
                    continue;
                }

                for(int ii = 0; ii< choosedData.ItemIds.Count; ii++)
                {
                    if(ii * 2 + 1 < choosedData.DropNums.Count)
                    {
                        var randNum = UnityEngine.Random.Range(choosedData.DropNums[ii * 2], choosedData.DropNums[ii * 2 + 1] + 1);
                        retList.Add(new(choosedData.ItemIds[ii], randNum));
                    }
                    else
                    {
                        Debug.LogError("config err data num not found " + groupId);
                        continue;
                    }
                }

            }

            return retList;
        }

    }
}



