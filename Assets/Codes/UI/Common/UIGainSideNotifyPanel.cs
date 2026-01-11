

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

        // 核心：任务队列
        private Queue<LogData> logQueue = new Queue<LogData>();
        private bool isProcessing = false;

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

            // 只要队列里还有东西，就一直循环
            while (logQueue.Count > 0)
            {
                // 1. 取出数据
                LogData data = logQueue.Dequeue();

                // 2. 生成 UI
                CreateLogItem(data);

                // 3. 强制间隔（这就是节奏控制的关键）
                yield return new WaitForSeconds(spawnInterval);
            }

            isProcessing = false;
        }

        private void CreateLogItem(LogData data)
        {
            GameObject obj = Instantiate(logItemPrefab, transform);
            obj.SetActive(true);
            // 确保顺序：新来的在最下面（或者最上面，取决于你的偏好）
            obj.transform.SetAsLastSibling();

            // 初始化内容
            UIGainSideNotifyItem item = obj.GetComponent<UIGainSideNotifyItem>();
            if (item != null)
            {
                item.Setup(data.message, data.icon);
            }
        }

    }

}