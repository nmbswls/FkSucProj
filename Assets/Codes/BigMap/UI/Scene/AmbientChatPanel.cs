using System.Collections;
using System.Collections.Generic;
using My.UI;
using UnityEngine;


namespace My.Map
{
    public class AmbientChatPanel : PanelBase
    {
        [Header("Configuration")]
        public GameObject bubblePrefab;
        public RectTransform spawnContainer; // 通常是 Canvas 或全屏 Panel

        [Header("Content")]
        public string[] chatterLines; // 闲谈文案库

        [Header("Spawn Settings")]
        public float spawnInterval = 1.5f; // 生成间隔
        public float edgePadding = 100f;   // 距离屏幕边缘的内边距
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f); // 大小随机范围

        private float timer;

        void Update()
        {
            //if(MainGameManager.Instance.playerScenePresenter != null && MainGameManager.Instance.playerScenePresenter.IsInBusyZone)
            {
                timer += Time.deltaTime;
                if (timer >= spawnInterval)
                {
                    SpawnBubble();
                    timer = 0;
                    // 稍微随机化下一次生成时间，避免太机械
                    timer -= Random.Range(0f, 0.5f);
                }
            }
        }

        void SpawnBubble()
        {
            if (chatterLines.Length == 0) return;

            // 1. 实例化
            GameObject go = Instantiate(bubblePrefab, spawnContainer);
            AmbientBubble bubble = go.GetComponent<AmbientBubble>();

            // 2. 随机内容
            string content = chatterLines[Random.Range(0, chatterLines.Length)];

            // 3. 计算屏幕四周的位置逻辑
            // 我们希望文字出现在四周，而不是屏幕正中间挡住主角
            Vector2 spawnPos = GetRandomPositionAroundEdges();

            // 4. 随机大小（模拟远近层次）
            float scale = Random.Range(scaleRange.x, scaleRange.y);

            // 5. 初始化
            bubble.Setup(content, spawnPos, scale);
        }

        // 核心算法：获取屏幕边缘区域的随机点
        Vector2 GetRandomPositionAroundEdges()
        {
            float width = spawnContainer.rect.width;
            float height = spawnContainer.rect.height;

            // 定义中间的“安全区域”（不生成文字的区域），例如屏幕中心的 50%
            float safeZoneWidth = width * 0.5f;
            float safeZoneHeight = height * 0.5f;

            float x = 0;
            float y = 0;

            // 简单的随机策略：随机决定是在 左/右 还是 上/下 区域
            // 0=左, 1=右, 2=上, 3=下
            int side = Random.Range(0, 4);

            switch (side)
            {
                case 0: // 左侧条状区域
                    x = Random.Range(-width / 2 + edgePadding, -safeZoneWidth / 2);
                    y = Random.Range(-height / 2 + edgePadding, height / 2 - edgePadding);
                    break;
                case 1: // 右侧条状区域
                    x = Random.Range(safeZoneWidth / 2, width / 2 - edgePadding);
                    y = Random.Range(-height / 2 + edgePadding, height / 2 - edgePadding);
                    break;
                case 2: // 上方条状区域
                    x = Random.Range(-width / 2 + edgePadding, width / 2 - edgePadding);
                    y = Random.Range(safeZoneHeight / 2, height / 2 - edgePadding);
                    break;
                case 3: // 下方条状区域
                    x = Random.Range(-width / 2 + edgePadding, width / 2 - edgePadding);
                    y = Random.Range(-height / 2 + edgePadding, -safeZoneHeight / 2);
                    break;
            }

            return new Vector2(x, y);
        }
    }
}


