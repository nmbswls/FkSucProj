using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static My.MiniGame.DeepAbsorbQteBar;


namespace My.MiniGame
{
    public class DeepAbsorbQteBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform barRect;
        [SerializeField] private RectTransform cursorRect;

        [SerializeField] private GameObject gridCellPrefab; // 格子预制体(Image)
        [SerializeField] private RectTransform gridParent;      // 格子生成的父节点
        [SerializeField] private int totalGrids = 40;       // 总格数
        [SerializeField] private float gridWidth = 15f;     // 单格宽度

        [Header("Input")]
        [SerializeField] private KeyCode inputKey = KeyCode.Space;

        [Header("Visual Colors")]
        public Color colorFail = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 灰色
        public Color colorNormal = new Color(1f, 1f, 1f, 0.8f);     // 白色
        public Color colorSuccess = new Color(0.2f, 0.8f, 1f, 1f);  // 蓝色
        public Color colorPerfect = new Color(1f, 0.8f, 0f, 1f);    // 金色

        [Header("Flow")]
        [SerializeField] private bool autoReset = true;
        [SerializeField] private float resetDelay = 0.8f;

        public enum ZoneType { Fail = 0, Normal = 1, Success = 2, Perfect = 3 }
        private ZoneType[] gridMap; // 存储每一格的类型

        public int Difficulty = 1;

        // 运行时状态
        private float currentSpeed;
        private List<GameObject> spawnedGrids = new List<GameObject>();

        private bool movingRight;
        private bool isRunning = false;
        private bool isFrozen;
        private float cursorPositionX = 0f;
        private float totalWidth;
        private float startX; // 新增变量：记录起始偏移量

        private Action<ZoneType> onResult; // 可选：外部回调


        private void Awake()
        {
            ValidateRefs();

            totalWidth = totalGrids * gridWidth;
            gridMap = new ZoneType[totalGrids];
            gridCellPrefab.SetActive(false);

            SpawnGridVisuals();
        }

        private void Update()
        {
            if (!isRunning) return;

            MoveCursor();

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                CheckHit();
            }
        }

        public void InitCursorPos()
        {
            cursorPositionX = 0;
        }

        public void ResetGame()
        {
            Cleanup();

            Difficulty = Mathf.Clamp(Difficulty, 1, 10);
            currentSpeed = 200f + (Difficulty * 50f);

            GenerateGridMap(Difficulty);

            UpdateGridVisuals();
            // 5. 重置光标
            movingRight = true;
            UpdateCursorVisual();

            isRunning = true;
        }

        private void ValidateRefs()
        {
            if (barRect == null || cursorRect == null)
            {
                Debug.LogError("[QTEBar] Missing references.");
                enabled = false;
                return;
            }

            
        }

        /// <summary>
        /// 核心算法：根据难度填充 gridMap 数组
        /// </summary>
        private void GenerateGridMap(int diff)
        {
            int centerIndex = totalGrids / 2;

            // 动态计算各区域的“半径”（格子数）
            // 难度10时: Perfect半径=0(只有中心1格), Success半径=1(左右各扩1格)
            // 难度1时: Perfect半径=5, Success半径=10
            int radiusPerfect = Mathf.Max(0, 5 - Mathf.CeilToInt(diff / 2.0f));
            int radiusSuccess = radiusPerfect + Mathf.Max(1, 6 - Mathf.CeilToInt(diff / 2.0f));
            int radiusNormal = radiusSuccess + Mathf.Max(1, 4 - Mathf.CeilToInt(diff / 3.0f));

            for (int i = 0; i < totalGrids; i++)
            {
                int dist = Mathf.Abs(i - centerIndex);

                if (dist <= radiusPerfect)
                {
                    gridMap[i] = ZoneType.Perfect;
                }
                else if (dist <= radiusSuccess)
                {
                    gridMap[i] = ZoneType.Success;
                }
                else if (dist <= radiusNormal)
                {
                    gridMap[i] = ZoneType.Normal;
                }
                else
                {
                    gridMap[i] = ZoneType.Fail;
                }
            }
        }

        private void MoveCursor()
        {
            float move = currentSpeed * Time.deltaTime;

            if (movingRight)
            {
                cursorPositionX += move;
                if (cursorPositionX >= totalWidth)
                {
                    cursorPositionX = totalWidth;
                    movingRight = false;
                }
            }
            else
            {
                cursorPositionX -= move;
                if (cursorPositionX <= 0)
                {
                    cursorPositionX = 0;
                    movingRight = true;
                }
            }

            UpdateCursorVisual();
        }

        private void UpdateCursorVisual()
        {
            if (cursorRect != null)
            {
                // cursorPositionX 是逻辑坐标(0~800)，需要加上 startX 转换成视觉坐标(-400~400)
                cursorRect.anchoredPosition = new Vector2(startX + cursorPositionX, cursorRect.anchoredPosition.y);
            }
        }

        private void CheckHit()
        {
            isRunning = false; // 停止移动

            // 计算当前落在哪一个格子上
            // 防止索引越界（比如刚好在边缘）
            int hitIndex = Mathf.FloorToInt(cursorPositionX / gridWidth);
            hitIndex = Mathf.Clamp(hitIndex, 0, totalGrids - 1);

            ZoneType result = gridMap[hitIndex];

            // 抛出结果，外部去处理UI显示
            onResult?.Invoke(result);
        }

        private void Cleanup()
        {
            //foreach (var g in spawnedGrids)
            //{
            //    if (g != null) Destroy(g);
            //}
            //spawnedGrids.Clear();
        }


        // 可选：外部订阅结果
        public void SetResultCallback(Action<ZoneType> callback)
        {
            onResult = callback;
        }

        // 在编辑器参数变动时动态更新
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (barRect == null) return;
            // 仅在编辑器预览更新区域位置
        }
#endif


        private void SpawnGridVisuals()
        {
            // 1. 强制设置父节点属性，确保它在屏幕中间
            RectTransform parentRect = gridParent.GetComponent<RectTransform>();
            parentRect.anchorMin = new Vector2(0.5f, 0.5f);
            parentRect.anchorMax = new Vector2(0.5f, 0.5f);
            parentRect.pivot = new Vector2(0.5f, 0.5f); // 中心点在中间
            parentRect.anchoredPosition = Vector2.zero; // 位置归零
            parentRect.sizeDelta = new Vector2(totalWidth, parentRect.sizeDelta.y); // 设为总宽

            // 2. 计算起始偏移量 (核心修改)
            // 因为父节点Pivot在中间，左边缘的坐标就是 -总宽/2
            startX = -totalWidth / 2f;

            
            for (int i = 0; i < totalGrids; i++)
            {
                GameObject obj = Instantiate(gridCellPrefab, gridParent);
                RectTransform rt = obj.GetComponent<RectTransform>();
                obj.SetActive(true);
                // 设置锚点为父节点左中，方便计算
                // 注意：这里我们改变策略，让子物体锚点跟随父节点中心，通过坐标偏移来定位
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f); // Pivot设在格子的左边，方便对齐

                // 3. 应用偏移量：起始位置 + 当前格索引 * 格宽
                float xPos = startX + (i * gridWidth);
                rt.anchoredPosition = new Vector2(xPos, 0);

                rt.sizeDelta = new Vector2(gridWidth, rt.sizeDelta.y); // 宽度固定，高度沿用Prefab

                spawnedGrids.Add(obj);
            }
        }

        private void UpdateGridVisuals()
        {
            for (int i = 0; i < totalGrids; i++)
            {
                GameObject obj = spawnedGrids[i];
                // 设置颜色
                Image img = obj.transform.GetChild(0).GetComponent<Image>();
                switch (gridMap[i])
                {
                    case ZoneType.Perfect: img.color = colorPerfect; break;
                    case ZoneType.Success: img.color = colorSuccess; break;
                    case ZoneType.Normal: img.color = colorNormal; break;
                    case ZoneType.Fail: img.color = colorFail; break;
                }
            }
        }
    }
}

