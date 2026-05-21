using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.UI;
using My.Map;
using My.Input;
using TMPro;

namespace My.UI
{

    /// <summary>
    /// 轮盘menu
    /// </summary>
    public class MapPlayerRadialMenu : PanelWithInput
    {
        public enum ERadialFunc
        {
            UseSkill,
            ChangeHuman,
            RepairItem,
        }


        [System.Serializable]
        public class RadialItem
        {
            public ERadialFunc RadialFunc;

            public string SkillId;
            public bool Interactable = true;
        }

        public int SectorCount = 8;

        [Header("Refs")]
        public RectTransform sectorContainer;
        public RadialSectorItem sectorPrefab;

        public TextMeshProUGUI chosenAbilityLabel;

        [Header("Appearance")]
        public float radius = 180f;
        public float innerRadius = 60f; // 内圈死区，避免误触中心
        public Color colorNormal = new Color(0.2f, 0.2f, 0.25f, 0.8f);
        public Color colorHighlight = new Color(0.85f, 0.7f, 0.2f, 0.95f);

        [Header("Behavior")]
        public bool cancelIfCenter = true; // 松开且在中心区域不选择

        private List<RadialSectorItem> sectors = new List<RadialSectorItem>();
        private int currentIndex = -1;
        private bool isOpen;
        private Camera uiCam; // 若 Canvas 是 ScreenSpace-Overlay，可为 null

        private float LastHoldUpdateTime;

        private List<RadialItem> builds;

        public static void ShowMenu()
        {
            var panel = UIManager.Instance.ShowPanel("MapPlayerRadialMenu", null) as MapPlayerRadialMenu;
            if (panel == null)
            {
                return;
            }
            
            panel.BuildMenu();
        }    


        private void CollectRadialItems()
        {
            if (builds == null)
            {
                builds = new();
                for (int i = 0; i < 9; i++)
                {
                    builds.Add(null);
                }
            }
            builds.Add(new RadialItem() { RadialFunc = ERadialFunc.UseSkill, SkillId = "player_ziwei", Interactable = true });
            builds.Add(new RadialItem() { RadialFunc = ERadialFunc.UseSkill, SkillId = "fix_clothes", Interactable = true });

            builds.Add(new RadialItem() { RadialFunc = ERadialFunc.RepairItem, Interactable = true });

            builds.Add(new RadialItem() { RadialFunc = ERadialFunc.ChangeHuman, Interactable = true });
        }

        // 动态设置条目
        public void BuildMenu()
        {
            Clear();

            CollectRadialItems();

            float count = SectorCount;
            float step = 360f / count;
            float fillAmount = step / 360f;

            for (int i = 0; i < count; i++)
            {
                var inst = Instantiate(sectorPrefab, sectorContainer);
                inst.gameObject.SetActive(true);
                inst.index = i;
                if(i < builds.Count)
                {
                    inst.SetData(builds[i],
                             colorNormal, fillAmount);
                }
                else
                {
                    inst.SetData(null, colorNormal, fillAmount);
                }

                // 设置旋转/摆放
                float startAngle = 0 - i * step; // 从正上方开始
                //float endAngle = startAngle + step;
                //inst.startAngle = Mathf.Repeat(startAngle, 360f);
                //inst.endAngle = Mathf.Repeat(endAngle, 360f);

                inst.SectRoot.localRotation = Quaternion.Euler(0, 0, startAngle + step / 2f - 1);
                inst.label.text = i.ToString();
                // 图标位置在圆环中线
                if (inst.InfoRoot != null)
                {
                    float midAngleRad = Mathf.Deg2Rad * (startAngle + 90);
                    Vector2 dir = new Vector2(Mathf.Cos(midAngleRad), Mathf.Sin(midAngleRad));
                    inst.InfoRoot.anchoredPosition = dir * ((radius + innerRadius) * 0.5f);
                }
                sectors.Add(inst);
            }
            
        }



        void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera;

            BuildMenu();

            sectorPrefab.gameObject.SetActive(false);
        }

        void Update()
        {
            if (!isOpen) return;

            UpdateSelectionByPointer();
            HandleClose();
        }


        public override void Show()
        {
            base.Show();

            SetOpen(true, instant: true);
            LastHoldUpdateTime = LogicTime.time;
        }

        public override void Hide()
        {
            base.Hide();

            SetOpen(false);
        }

        private void HandleClose()
        {
            // 超时未更新 关闭
            if(LogicTime.time - LastHoldUpdateTime > 1f)
            {
                OnReleaseToConfirm();
            }
        }

        private void OnReleaseToConfirm()
        {
            if (!isOpen) return;

            if (!cancelIfCenter || currentIndex >= 0)
                ConfirmCurrent();

            UIManager.Instance.HidePanel("MapPlayerRadialMenu");
        }

        private void UpdateSelectionByPointer()
        {
            // 鼠标位置换算到本地坐标
            var mousePos = MainGameManager.Instance.inputBinder.LastPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                sectorContainer, mousePos, uiCam, out Vector2 local);

            float dist = local.magnitude;
            if (dist < innerRadius || dist > radius * 1.2f)
            {
                Highlight(-1);
                return;
            }

            float count = SectorCount;
            float step = 360f / count;
            float angle = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;

            float startAngle = -90f - step / 2f;

            // 将右方0度转换为上方0度（-90），并映射到0-360
            //angle = Mathf.Repeat(angle - 90f, 360f);
            int idx = AngleToIndex(-angle + 90);
            Highlight(idx);

            if(idx < builds.Count)
            {
                chosenAbilityLabel.text = builds[idx].SkillId;
            }
            else
            {
                chosenAbilityLabel.text = "无";
            }
        }

        private int AngleToIndex(float angle)
        {
            int count = sectors.Count;
            if (count == 0) return -1;
            float step = 360f / count;
            // 上方为 index=0
            int idx = Mathf.RoundToInt(Mathf.Repeat((angle) / step, count)) % count;
            return idx;
        }

        private void Highlight(int idx)
        {
            currentIndex = idx;
            for (int i = 0; i < sectors.Count; i++)
            {
                bool on = (i == idx);
                sectors[i].SetHighlight(on, colorNormal, colorHighlight);
            }
        }

        private void ConfirmCurrent()
        {
            if (currentIndex < 0 || currentIndex >= sectors.Count) return;
            var sector = sectors[currentIndex];
            if (sector.innerItem == null) return;

            switch(sector.innerItem.RadialFunc)
            {
                case ERadialFunc.UseSkill:
                    {
                        MainGameManager.Instance.gameLogicManager.playerLogicEntity.ablilityManager.UseSkill(sector.innerItem.SkillId);
                    }
                    break;
                case ERadialFunc.ChangeHuman:
                    {
                        MainGameManager.Instance.gameLogicManager.TrySetPlayerHumanMode(!MainGameManager.Instance.gameLogicManager.PlayerHumanMode);
                    }
                    break;
                case ERadialFunc.RepairItem:
                    {
                        var itemId = MainGameManager.Instance.gameLogicManager.playerDataManager.HumanQuickBar.GetActiveConsumableItemId();
                        if(string.IsNullOrEmpty(itemId))
                        {
                            Debug.Log("当前无修复道具" + itemId);
                            break;
                        }

                        int needJingYuan = 50;
                        if(!MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.CheckHaveItem("jingyuan", needJingYuan))
                        {
                            Debug.Log("当前不够" + itemId);
                            break;
                        }
                        Debug.Log("修复道具" + itemId);
                        
                        MainGameManager.Instance.gameLogicManager.playerLogicEntity.abilityController.TryUseAbility("player_supply_item", overrideParams: new Dictionary<string, string>()
                        {
                            ["InteractTime"] = "3.0",
                        }, phaseOverrideAnims: new Dictionary<string, string>()
                        {
                            ["Interacting"] = "repair"
                        },
                        onAbilityEnd: (complete) =>
                        {
                            if (complete)
                            {
                                MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.CostItem("jingyuan", needJingYuan);
                                MainGameManager.Instance.gameLogicManager.playerDataManager.InventorySystem.GiveItemToPlayer(itemId, 1);
                            }
                        });

                    }
                    break;
            }
            
            //MainGameManager.Instance.gameLogicManager.playerLogicEntity.abilityController.TryUseAbility(sector.AbilityId);
        }

        public void SetOpen(bool open, bool instant = false)
        {
            isOpen = open;
            if (canvasGroup == null) return;
            canvasGroup.blocksRaycasts = open;
            canvasGroup.interactable = open;
            canvasGroup.alpha = open ? 1f : 0f;
            if (!open) Highlight(-1);
            // 可加淡入淡出协程
        }

        public void Clear()
        {
            foreach (var s in sectors)
                if (s) Destroy(s.gameObject);
            sectors.Clear();
            currentIndex = -1;

            builds?.Clear();
        }

        public override bool OnNavigate(Vector2 dir)
        {
            return false;
        }

        public override bool OnHoldUpdate(string holdKey)
        {
            if(holdKey == EInputKey.Tab.ToString())
            {
                this.LastHoldUpdateTime = LogicTime.time;
                return true;
            }
            return false;
        }

        public override bool OnHoldingEnd(string holdKey)
        {
            if (holdKey == EInputKey.Tab.ToString())
            {
                this.LastHoldUpdateTime = 0;
                return true;
            }
            return false;
        }
        
    }
}