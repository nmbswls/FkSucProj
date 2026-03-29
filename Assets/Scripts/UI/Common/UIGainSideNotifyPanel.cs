

using System.Collections;
using System.Collections.Generic;
using My.UI;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

namespace My
{

    public struct LogData
    {
        public string message;
        public Sprite icon;

        public LogData(string msg, Sprite ico)
        {
            message = msg;
            icon = ico;
        }
    }

    public class UIGainSideNotifyPanel : PanelBase
    {
        public static UIGainSideNotifyPanel Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("GainItemSideNtfPanel");
                if (panel != null && panel is UIGainSideNotifyPanel panel2)
                {
                    return panel2;
                }
                return null;
            }
        }

        [Header("Settings")]
        public GameObject logItemPrefab;
        public float spawnInterval = 0.3f; // 每一条日志弹出的间隔时间
        public int maxActiveItems = 5;
        public Transform logItemContainer;

        // 核心：任务队列
        private Queue<LogData> logQueue = new Queue<LogData>();
        private bool isProcessing = false;

        // 活动条目列表（用于管理上浮和数量限制）
        private List<UIGainSideNotifyItem> activeItems = new List<UIGainSideNotifyItem>();

        private void Awake()
        {
            logItemPrefab.SetActive(false);
        }

        /// <summary>
        /// 外部调用的入口：只管把数据扔进来
        /// </summary>
        public void EnqueueLog(string message, Sprite icon)
        {
            logQueue.Enqueue(new LogData(message, icon));

            // 如果当前没有在处理队列，就开始处理
            if (!isProcessing)
            {
                StartCoroutine(ProcessQueueRoutine());
            }
        }

        private IEnumerator ProcessQueueRoutine()
        {
            isProcessing = true;

            while (logQueue.Count > 0)
            {
                LogData data = logQueue.Dequeue();
                CreateLogItem(data);
                yield return new WaitForSeconds(spawnInterval);
            }

            isProcessing = false;
        }

        private void CreateLogItem(LogData data)
        {

            // 1. 清理已销毁的空对象
            // (Item自己销毁后 List 里会留 null，需要先清掉防止报错)
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                if (activeItems[i] == null) activeItems.RemoveAt(i);
            }

            // 2. 检查数量限制 (超出则移除最老的)
            while (activeItems.Count >= maxActiveItems)
            {
                // activeItems[0] 是最老的（最早添加的）
                UIGainSideNotifyItem oldItem = activeItems[0];
                activeItems.RemoveAt(0); // 从列表移除引用

                if (oldItem != null)
                {
                    // 调用 Item 自身的快速退出方法
                    oldItem.ForceExit();
                }
            }

            //// 3. 通知剩下的老条目往上走
            //// 遍历所有活着的条目，让它们的目标 Y 坐标增加一个高度
            //foreach (var item in activeItems)
            //{
            //    if (item != null) item.AddHeightOffset(itemHeight);
            //}

            // 4. 生成新条目
            GameObject obj = Instantiate(logItemPrefab, logItemContainer);
            obj.SetActive(true);

            // 初始化 RectTransform，确保它是从容器底部开始
            // 这里的坐标系逻辑依赖于你之前说的“脱离LayoutGroup”方案
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null)
            {
                // 确保 Anchor/Pivot 是右下角 (1, 0)
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = Vector2.zero; // 重置位置到原点
            }

            // 保证层级在最下（如果你希望新消息盖在旧消息上，用 SetAsLastSibling；反之 SetAsFirstSibling）
            obj.transform.SetAsLastSibling();

            // 5. 初始化脚本并加入列表
            UIGainSideNotifyItem newItem = obj.GetComponent<UIGainSideNotifyItem>();
            if (newItem != null)
            {
                newItem.Initialize(data.message, data.icon);
                // 加入列表末尾
                activeItems.Add(newItem);
            }
        }

    }

}