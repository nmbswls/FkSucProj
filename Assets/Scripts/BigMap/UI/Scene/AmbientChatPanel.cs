using System.Collections;
using System.Collections.Generic;
using System.Linq;
using My.UI;
using UnityEngine;


namespace My.Map
{
    public class AmbientChatPanel : PanelBase
    {
        [System.Serializable]
        public class ChatSlot
        {
            public Vector2 position;   // 基础锚点位置
            public int side;           // 0=左, 1=右
            public float cooldownTimer; // 冷却倒计时
        }

        [Header("配置")]
        public GameObject bubblePrefab;
        public RectTransform spawnContainer;

        [Header("文案")]
        [TextArea] public string[] chatterLines;

        [Header("生成规则")]
        public float spawnInterval = 0.6f;
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

        [Header("插槽设置 (Slot Settings)")]
        [Tooltip("气泡垂直方向的间距，大约是气泡的高度")]
        public float slotHeightSpacing = 120f;
        [Tooltip("边缘距离")]
        public float edgePadding = 150f;
        [Tooltip("选中插槽后的随机偏移范围")]
        public float positionJitter = 30f;
        [Tooltip("插槽冷却时间（秒），防止同一位置连续生成")]
        public float slotCooldown = 3.0f;

        private List<ChatSlot> slots = new List<ChatSlot>();
        private float spawnTimer;

        void Start()
        {
            // 游戏开始时计算所有可用的插槽位置
            GenerateSlots();
        }

        void Update()
        {
            // 1. 更新所有插槽的冷却时间
            UpdateSlotsCooldown();

            
            // 2. 生成逻辑
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                TrySpawnBubble();
                // 稍微随机化下一次生成的时间点，避免太机械
                spawnTimer = Random.Range(-0.1f, 0.1f);
            }
        }

        // 预计算屏幕两侧的生成点
        void GenerateSlots()
        {
            slots.Clear();
            float containerHeight = spawnContainer.rect.height;
            float containerWidth = spawnContainer.rect.width;

            // 计算每一侧能放多少个插槽
            // 我们留出上下 10% 的余量，不让气泡生成在屏幕最顶端或最底端
            float availableHeight = containerHeight * 0.8f;
            int slotCountPerSide = Mathf.FloorToInt(availableHeight / slotHeightSpacing);

            float startY = -availableHeight / 2; // 从下方开始

            // 生成左侧插槽
            for (int i = 0; i < slotCountPerSide; i++)
            {
                float y = startY + (i * slotHeightSpacing);
                // 左侧 X 坐标
                float leftX = -containerWidth / 2 + edgePadding;

                slots.Add(new ChatSlot
                {
                    position = new Vector2(leftX, y),
                    side = 0,
                    cooldownTimer = 0f
                });

                // 右侧 X 坐标 (对称生成)
                float rightX = containerWidth / 2 - edgePadding;
                slots.Add(new ChatSlot
                {
                    position = new Vector2(rightX, y),
                    side = 1,
                    cooldownTimer = 0f
                });
            }
        }

        void UpdateSlotsCooldown()
        {
            foreach (var slot in slots)
            {
                if (slot.cooldownTimer > 0)
                {
                    slot.cooldownTimer -= Time.deltaTime;
                }
            }
        }

        void TrySpawnBubble()
        {
            if(MainGameManager.Instance.gameLogicManager.MainStage != GameLogicManager.EMainGameStage.Running)
            {
                return;
            }
            var playerPrenster = MainGameManager.Instance.playerScenePresenter;
            if (playerPrenster == null)
            {
                return;
            }

            if(!playerPrenster.PlayerEntity.IsInBusyZone)
            { 
                return; 
            }

            if (chatterLines.Length == 0) return;

            // 1. 获取所有“空闲”的插槽 (冷却时间 <= 0)
            var availableSlots = slots.Where(s => s.cooldownTimer <= 0).ToList();

            // 如果没有空闲位置，这一帧就不生成（避免重叠的关键！）
            if (availableSlots.Count == 0) return;

            // 2. 随机选一个插槽
            ChatSlot selectedSlot = availableSlots[Random.Range(0, availableSlots.Count)];

            // 3. 激活插槽冷却 (重置计时器)
            selectedSlot.cooldownTimer = slotCooldown;

            // 4. 计算最终位置 (基础位置 + 随机偏移 Jitter)
            // 这样即使选中同一个插槽，每次位置也会有微妙不同
            Vector2 finalPos = selectedSlot.position + Random.insideUnitCircle * positionJitter;

            // 5. 生成实体
            SpawnBubbleAt(finalPos, selectedSlot.side);
        }

        void SpawnBubbleAt(Vector2 pos, int side)
        {
            GameObject go = Instantiate(bubblePrefab, spawnContainer);
            go.transform.SetAsLastSibling(); // 保证在最上层 (或者 FirstSibling 在最底层)

            AmbientBubble bubble = go.GetComponent<AmbientBubble>();
            string content = chatterLines[Random.Range(0, chatterLines.Length)];
            float scale = Random.Range(scaleRange.x, scaleRange.y);

            // 调用之前的 Setup 方法
            var rand = UnityEngine.Random.Range(0, 100);
            bubble.Setup(content, pos, scale, scaleRange, side, rand < 50 ? 0 : 1);
        }

        // 编辑器辅助：画出插槽位置，方便调试
        void OnDrawGizmosSelected()
        {
            if (spawnContainer == null) return;

            Gizmos.color = Color.yellow;

            // 既然在编辑器模式下可能还没运行Start，我们简单模拟一下位置
            if (!Application.isPlaying) return;

            foreach (var slot in slots)
            {
                // 将 RectTransform 的局部坐标转换为世界坐标绘制
                Vector3 worldPos = spawnContainer.TransformPoint(slot.position);

                if (slot.cooldownTimer > 0)
                    Gizmos.color = Color.red; // 冷却中显示红色
                else
                    Gizmos.color = Color.green; // 可用显示绿色

                Gizmos.DrawWireSphere(worldPos, 20f);
            }
        }
    }
}


