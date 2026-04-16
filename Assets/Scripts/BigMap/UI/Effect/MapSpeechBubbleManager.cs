using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.UI
{

    public class MapSpeechBubbleManager : MonoBehaviour
    {
        public static MapSpeechBubbleManager Instance { get; private set; }

        [Header("UI 设置")]
        public GameObject bubblePrefab;

        // 对象池
        private Queue<MapSpeechBubble> bubblePool = new Queue<MapSpeechBubble>();

        // 核心：记录正在说话的各个角色 (Key: 角色的InstanceID, Value: 该角色的频道数据)
        private Dictionary<long, CharacterChannel> activeChannels = new Dictionary<long, CharacterChannel>();
        // 缓存一个待移除列表，防止遍历时修改集合报错
        private List<long> deadChannelIds = new List<long>();

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        void Update()
        {
            // 1. 统一 Tick 所有频道
            foreach (var kvp in activeChannels)
            {
                var channel = kvp.Value;
                bool isFinished = channel.Tick(Time.deltaTime); // 驱动状态机

                if (isFinished)
                {
                    deadChannelIds.Add(kvp.Key);
                }
            }

            // 2. 清理已经说完话的频道
            if (deadChannelIds.Count > 0)
            {
                foreach (var id in deadChannelIds)
                {
                    activeChannels.Remove(id);
                }
                deadChannelIds.Clear();
            }
        }

        // ================== 对外接口 ==================

        /// <summary>
        /// 让某个目标说话（自动加入队列）
        /// </summary>
        public void Say(IScenePresentation target, string text, float duration = 2f, int priority = 1,  float extraInterval = 0)
        {
            if (target == null) return;

            long id = target.Id;

            // 如果这个角色还没有“频道”，给他开一个
            if (!activeChannels.ContainsKey(id))
            {
                activeChannels[id] = new CharacterChannel(this, target);
            }

            // 往他的频道里塞一句话
            activeChannels[id].Enqueue(text, duration, priority, extraInterval);
        }

        public void StopAllSaying(long id)
        {
            activeChannels.TryGetValue(id, out var channel);
            if (channel != null)
            {
                //channel.isf
            }
        }


        // ================== 池化底层逻辑 ==================

        public MapSpeechBubble GetBubbleFromPool()
        {
            if (bubblePool.Count > 0) return bubblePool.Dequeue();

            GameObject obj = Instantiate(bubblePrefab);
            obj.SetActive(true);
            obj.transform.SetParent(SceneSmallIconLayerPanel.Instance.transform, false);
            return obj.GetComponent<MapSpeechBubble>();
        }

        public void ReturnBubbleToPool(MapSpeechBubble bubble)
        {
            if (bubble != null)
            {
                bubble.Hide();
                bubblePool.Enqueue(bubble);
            }
        }

        // ================== 内部类：角色频道 ==================
        // 负责管理【单个角色】的对话队列和协程
        private class CharacterChannel
        {
            private MapSpeechBubbleManager manager;
            private IScenePresentation target;
            private MapSpeechBubble currentBubble;

            // 简单数据结构
            struct Cmd 
            { 
                public string text; 
                public float duration;
                public int priority;
                public float extraInterval; 
            }
            private Queue<Cmd> queue = new Queue<Cmd>();

            // 状态变量
            private float timer;
            private bool isShowingBubble; // true=显示中, false=间隔等待中
            private Cmd currentCmd;
            private float popInterval = 0.3f;

            public CharacterChannel(MapSpeechBubbleManager mgr, IScenePresentation t)
            {
                manager = mgr;
                target = t;
            }

            public void Enqueue(string text, float duration, int priority, float extraInterval = 0)
            {
                if(queue.Count > 0)
                {
                    while(queue.Count > 0 && queue.Peek().priority < priority)
                    {
                        queue.Dequeue();
                    }
                }
                queue.Enqueue(new Cmd { text = text, duration = duration, priority = priority, extraInterval = extraInterval });
            }

            // 返回 true 表示频道空闲可以移除了
            public bool Tick(float dt)
            {
                // 如果宿主挂了，归还气泡并自杀
                if (target == null)
                {
                    CleanUp();
                    return true;
                }

                // --- 状态机逻辑 ---

                if (timer > 0)
                {
                    // 倒计时阶段
                    timer -= dt;

                    // 气泡跟随逻辑放在这里或者 Bubble 自身的 Update 都可以
                    // 如果放在这里，Bubble 就可以做成纯数据接收者，完全没有任何 Update
                    if (isShowingBubble && currentBubble != null)
                    {
                        // 简单的跟随逻辑
                        Camera mainCam = Camera.main;
                        Camera uiCam = UIManager.Instance.UICamera;
                        //RectTransform canvasRect = UIManager.Instance.transform;
                        RectTransform bubbleRect = currentBubble.GetComponent<RectTransform>();

                        Vector3 worldPos = target.PivotHeader.position;

                        // 3. 将世界坐标转换为屏幕像素坐标
                        Vector3 screenPos3D = mainCam.WorldToScreenPoint(worldPos);

                        Vector2 screenPos2D = new Vector2(screenPos3D.x, screenPos3D.y);
                        Vector2 localPos;

                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            SceneSmallIconLayerPanel.Instance.transform as RectTransform,     // 相对谁的坐标系？(父容器)
                            screenPos2D,    // 屏幕上的点在哪里？
                            uiCam,          // 谁拍的UI？(Overlay填null)
                            out localPos    // 结果存到这里
                        );

                        // 6. 应用坐标
                        bubbleRect.anchoredPosition = localPos;
                    }
                }
                else
                {
                    // 倒计时结束，发生状态切换
                    if (isShowingBubble)
                    {
                        // === 刚刚结束显示 ===

                        // 检查是否有后续间隔
                        if (popInterval + currentCmd.extraInterval > 0)
                        {
                            // 进入间隔期：回收气泡，设置间隔计时
                            if (currentBubble != null)
                            {
                                manager.ReturnBubbleToPool(currentBubble);
                                currentBubble = null;
                            }
                            isShowingBubble = false;
                            timer = popInterval + currentCmd.extraInterval;
                        }
                        else
                        {
                            // 无间隔：尝试播放下一条
                            TryPlayNext();
                        }
                    }
                    else
                    {
                        // === 刚刚结束间隔 ===
                        TryPlayNext();
                    }
                }

                // 如果没气泡，也没指令，说明空闲了
                return (currentBubble == null && queue.Count == 0 && timer <= 0);
            }

            private void TryPlayNext()
            {
                if (queue.Count > 0)
                {
                    currentCmd = queue.Dequeue();

                    // 获取/复用气泡
                    if (currentBubble == null)
                    {
                        currentBubble = manager.GetBubbleFromPool();
                        currentBubble.Init(target);
                    }

                    currentBubble.SetText(currentCmd.text);

                    isShowingBubble = true;
                    timer = currentCmd.duration;
                }
                else
                {
                    // 没话说了，清理现场
                    CleanUp();
                }
            }

            private void CleanUp()
            {
                if (currentBubble != null)
                {
                    manager.ReturnBubbleToPool(currentBubble);
                    currentBubble = null;
                }
                isShowingBubble = false;
                timer = 0;
            }
        }
    }
}


