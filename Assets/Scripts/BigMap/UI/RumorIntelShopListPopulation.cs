using System;
using My.Config;
using My.Player;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 列表填充：从 Prefab 模板克隆行；不包含在 RumorIntelShopPanel 中。
    public sealed class RumorIntelShopListPopulation : MonoBehaviour
    {
        [SerializeField] RectTransform listHost;

        [SerializeField] GameObject sectionRowTemplate;

        [SerializeField] GameObject hintRowTemplate;

        [SerializeField] GameObject buyRowTemplate;

        public void ClearAndPopulate(string mapId, Action<string> tryBuy)
        {
            if (listHost == null || CfgMgr.Cfgs == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.playerDataManager == null)
            {
                return;
            }

            if (sectionRowTemplate == null || hintRowTemplate == null || buyRowTemplate == null)
            {
                Debug.LogError("[RumorIntelShop] Row templates not assigned on RumorIntelShopListPopulation.");
                return;
            }

            for (var i = listHost.childCount - 1; i >= 0; i--)
            {
                Destroy(listHost.GetChild(i).gameObject);
            }

            var rumor = glm.playerDataManager.RumorIntel;

            AddSection("=== Fixed ===");
            var fixedList = rumor.ListPurchasableFixed(mapId);
            foreach (var def in fixedList)
            {
                AddBuyRow(def.ThumbName, def.CostItemId, def.CostCount, def.RumorId, tryBuy);
            }

            if (fixedList.Count == 0)
            {
                AddHint("No fixed intel available.");
            }

            AddSection("=== Rumor pool (pick one) ===");
            if (rumor.HasActiveRandomIntel(mapId))
            {
                AddHint("Random intel slot occupied; finish infiltration or wait expire.");
            }
            else
            {
                rumor.EnsureRandomOffersForShop(mapId);
                foreach (var rid in rumor.GetRandomOfferIds(mapId))
                {
                    var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(rid);
                    if (def == null)
                    {
                        continue;
                    }

                    AddBuyRow(def.ThumbName, def.CostItemId, def.CostCount, def.RumorId, tryBuy);
                }
            }

            AddSection("=== Pending (next infiltration) ===");
            var actives = rumor.GetActiveSnapshot(mapId);
            foreach (var a in actives)
            {
                var def = CfgMgr.Cfgs.TbRumorIntel.GetOrDefault(a.RumorId);
                var txt = def != null ? def.FullText : a.RumorId;
                AddHint($"- {txt}");
            }

            if (actives.Count == 0)
            {
                AddHint("(none)");
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(listHost);
        }

        void AddSection(string title)
        {
            var go = Instantiate(sectionRowTemplate, listHost);
            go.SetActive(true);
            var row = go.GetComponent<RumorIntelShopSectionRowView>();
            if (row == null)
            {
                Debug.LogError("[RumorIntelShop] sectionRowTemplate missing RumorIntelShopSectionRowView.");
                return;
            }

            row.Apply(title);
        }

        void AddHint(string msg)
        {
            var go = Instantiate(hintRowTemplate, listHost);
            go.SetActive(true);
            var row = go.GetComponent<RumorIntelShopHintRowView>();
            if (row == null)
            {
                Debug.LogError("[RumorIntelShop] hintRowTemplate missing RumorIntelShopHintRowView.");
                return;
            }

            row.Apply(msg);
        }

        void AddBuyRow(string thumb, string costId, long cost, string rumorId, Action<string> tryBuy)
        {
            var go = Instantiate(buyRowTemplate, listHost);
            go.SetActive(true);
            var row = go.GetComponent<RumorIntelShopBuyRowView>();
            if (row == null)
            {
                Debug.LogError("[RumorIntelShop] buyRowTemplate missing RumorIntelShopBuyRowView.");
                return;
            }

            row.Apply(thumb, costId, cost, rumorId, tryBuy);
        }
    }
}
