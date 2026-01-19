using System.Collections;
using System.Collections.Generic;
using My.UI;
using UnityEngine;


namespace My.Map
{
    public class AmbientChatPanel : PanelBase
    {
        [Header("配置")]
        public GameObject bubblePrefab;
        public RectTransform spawnContainer; // 必须拖入那个全屏的 ChatContainer

        [Header("文案内容")]
        [TextArea] public string[] chatterLines;

        [Header("生成设置")]
        public float spawnInterval = 0.5f; // 生成频率高一点，才有氛围
        public Vector2 scaleRange = new Vector2(0.7f, 1.1f);

        [Header("边缘控制 (关键)")]
        [Tooltip("边缘区域的厚度（像素），数值越小越贴边")]
        public float edgeThickness = 250f;
        [Tooltip("上下边缘的留白，防止文字被刘海或任务栏挡住")]
        public float verticalPadding = 100f;

        private float timer;

        void Update()
        {
            if(MainGameManager.Instance.playerScenePresenter != null && MainGameManager.Instance.playerScenePresenter.IsInBusyZone)
            {
                timer += Time.deltaTime;
                if (timer >= spawnInterval)
                {
                    SpawnBubble();
                    // 随机化间隔，让出现节奏不规律
                    timer = Random.Range(0f, 0.2f);
                }
            }
        }

        void SpawnBubble()
        {
            if (chatterLines.Length == 0) return;

            // 1. 决定生成在哪一侧 (0=左, 1=右, 2=上, 3=下)
            // 暗喻幻想风格主要集中在左右两侧，上下较少。我们增加左右的权重。
            int side = GetWeightedSide();

            // 2. 计算坐标
            Vector2 spawnPos = CalculateEdgePosition(side);

            // 3. 实例化
            GameObject go = Instantiate(bubblePrefab, spawnContainer);
            // 设为第一个子物体，保证新生成的在最下面（或者是SetAsLastSibling覆盖上面，看喜好）
            go.transform.SetAsLastSibling();

            AmbientBubble bubble = go.GetComponent<AmbientBubble>();

            // 4. 设置属性
            string content = chatterLines[Random.Range(0, chatterLines.Length)];
            float scale = Random.Range(scaleRange.x, scaleRange.y);

            // 传入 side 参数，告诉气泡它在哪一侧，以便它自己旋转角度
            bubble.Setup(content, spawnPos, scale, scaleRange, side);
        }

        // 增加左右两侧的出现概率 (左:35%, 右:35%, 上:15%, 下:15%)
        int GetWeightedSide()
        {
            float r = Random.value;
            if (r < 0.35f) return 0; // 左
            if (r < 0.70f) return 1; // 右
            if (r < 0.85f) return 2; // 上
            return 3;                // 下
        }

        Vector2 CalculateEdgePosition(int side)
        {
            float width = spawnContainer.rect.width;
            float height = spawnContainer.rect.height;

            // 容器中心是 (0,0)
            float halfW = width / 2;
            float halfH = height / 2;

            float x = 0;
            float y = 0;

            switch (side)
            {
                case 0: // 左侧边缘区域
                        // x 在 [-halfW] 到 [-halfW + thickness] 之间
                    x = Random.Range(-halfW, -halfW + edgeThickness);
                    y = Random.Range(-halfH + verticalPadding, halfH - verticalPadding);
                    break;

                case 1: // 右侧边缘区域
                        // x 在 [halfW - thickness] 到 [halfW] 之间
                    x = Random.Range(halfW - edgeThickness, halfW);
                    y = Random.Range(-halfH + verticalPadding, halfH - verticalPadding);
                    break;

                case 2: // 上方边缘区域
                    x = Random.Range(-halfW + edgeThickness, halfW - edgeThickness); // 避开四个角落
                    y = Random.Range(halfH - edgeThickness, halfH);
                    break;

                case 3: // 下方边缘区域
                    x = Random.Range(-halfW + edgeThickness, halfW - edgeThickness);
                    y = Random.Range(-halfH, -halfH + edgeThickness);
                    break;
            }

            return new Vector2(x, y);
        }
    }
}


