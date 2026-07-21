using System.Collections.Generic;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My.Farm
{
    // 静态农田区域：挂在 Map 分块加载的 prefab 上，注册格子原点并刷新作物表现
    public sealed class FarmPlotAreaProvider : MonoBehaviour
    {
        [SerializeField] string plotId = "home01_field_a";
        [SerializeField] string logicAreaIdOverride;
        [SerializeField] Transform visualRoot;
        [SerializeField] FarmCropCellView[] cellViews;
        [SerializeField] Color emptyColor = new(0.45f, 0.35f, 0.2f, 0.35f);
        [SerializeField] Color seedColor = new(0.75f, 0.7f, 0.35f, 0.7f);
        [SerializeField] Color growColor = new(0.35f, 0.7f, 0.3f, 0.85f);
        [SerializeField] Color matureColor = new(0.9f, 0.75f, 0.2f, 0.95f);

        string _logicAreaId;
        FarmPlot _cfg;

        void OnEnable()
        {
            ResolveCfg();
            if (_cfg == null)
            {
                return;
            }

            _logicAreaId = string.IsNullOrEmpty(logicAreaIdOverride) ? _cfg.LogicAreaId : logicAreaIdOverride;
            FarmPlotAreaRegistry.Register(_logicAreaId, plotId, transform.position);
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.farmSystem != null)
            {
                glm.farmSystem.EvOnFarmChanged -= RefreshVisuals;
                glm.farmSystem.EvOnFarmChanged += RefreshVisuals;
            }

            RefreshVisuals();
        }

        void OnDisable()
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            if (glm?.farmSystem != null)
            {
                glm.farmSystem.EvOnFarmChanged -= RefreshVisuals;
            }

            FarmPlotAreaRegistry.Unregister(_logicAreaId, plotId);
        }

        void ResolveCfg()
        {
            _cfg = CfgMgr.Cfgs?.TbFarmPlot?.GetOrDefault(plotId);
        }

        void RefreshVisuals()
        {
            ResolveCfg();
            if (_cfg == null)
            {
                return;
            }

            var glm = MainGameManager.Instance?.gameLogicManager;
            var farm = glm?.farmSystem;
            if (farm == null)
            {
                return;
            }

            bool visible = farm.IsPlotVisible(_cfg);
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                SetAllCellsActive(false);
                return;
            }

            var persist = farm.GetOrCreateTownFarm(_logicAreaId);
            FarmPlotPersist plot = null;
            for (int i = 0; i < persist.Plots.Count; i++)
            {
                if (persist.Plots[i].PlotId == plotId)
                {
                    plot = persist.Plots[i];
                    break;
                }
            }

            if (plot == null || cellViews == null)
            {
                return;
            }

            float cellSize = _cfg.CellSize > 0.01f ? _cfg.CellSize : 1f;
            int n = Mathf.Min(cellViews.Length, plot.Cells.Count);
            for (int i = 0; i < cellViews.Length; i++)
            {
                var view = cellViews[i];
                if (view == null)
                {
                    continue;
                }

                if (i >= n)
                {
                    view.gameObject.SetActive(false);
                    continue;
                }

                var cell = plot.Cells[i];
                view.Bind(plotId, cell, cellSize, transform.position, PickColor(cell), farm, _logicAreaId);
            }
        }

        void SetAllCellsActive(bool active)
        {
            if (cellViews == null)
            {
                return;
            }

            for (int i = 0; i < cellViews.Length; i++)
            {
                if (cellViews[i] != null)
                {
                    cellViews[i].gameObject.SetActive(active);
                }
            }
        }

        Color PickColor(FarmCellPersist cell)
        {
            if (string.IsNullOrEmpty(cell.CropId))
            {
                return emptyColor;
            }

            var crop = FarmCatalog.GetCrop(cell.CropId);
            if (crop != null && FarmCatalog.IsMature(crop, cell.GrowProgress))
            {
                return matureColor;
            }

            if (crop != null && FarmCatalog.IsSprouted(crop, cell.GrowProgress))
            {
                return growColor;
            }

            return seedColor;
        }
    }
}
