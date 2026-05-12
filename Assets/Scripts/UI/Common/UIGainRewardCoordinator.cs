
using System.Collections;
using My.UI;
using UnityEngine;

namespace My
{
    public class UIGainRewardCoordinator : PanelBase
    {
        public static UIGainRewardCoordinator Instance
        {
            get
            {
                var panel = UIManager.Instance.GetShowingPanel("UIGainRewardCoordinator");
                if (panel != null && panel is UIGainRewardCoordinator panel2)
                {
                    return panel2;
                }
                return null;
            }
        }

        public RectTransform flyingLayer;       // 飞行图标的父节点（全屏Panel）
        public RectTransform FlyTarget;

        public GameObject flyerPrefab;      // 挂载 RewardFlyer 的 Prefab

        private void Awake()
        {
            flyerPrefab.gameObject.SetActive(false);
        }

        /// <summary>
        /// 公开
        /// </summary>
        public void CreateScreenItem(string itemName, int count, Sprite icon)
        {
            // 0. 安全检查
            if (OverworldHUDPanel.Instance == null) return;
            if (flyerPrefab == null || flyingLayer == null || FlyTarget == null)
            {
                Debug.LogError("CreateScreenItem: 缺少必要的引用 (Prefab/Layer/Target)");
                return;
            }

            // 1. 实例化物体
            GameObject flyerObj = Instantiate(flyerPrefab, flyingLayer);
            flyerObj.SetActive(true);
            // 重置一下局部坐标，确保它生成时不会带着Prefab里的偏移
            flyerObj.transform.localPosition = Vector3.zero;

            UINewRewardFlyer flyer = flyerObj.GetComponent<UINewRewardFlyer>();

            // ================= 坐标计算核心逻辑 =================

            // A. 确定屏幕中心的【像素坐标】并加上随机偏移
            Vector2 screenCenterPixel = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 100f; // 100像素半径内随机
            Vector2 spawnScreenPos = screenCenterPixel + randomOffset;

            // B. 获取当前 Canvas 的相机 (UI相机)
            // 如果是 Overlay 模式，传 null；如果是 Camera 模式，传 worldCamera
            Camera uiCamera = null;
            Canvas rootCanvas = flyingLayer.GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = rootCanvas.worldCamera;
            }

            // C. 将【屏幕像素坐标】转换为 flyingLayer 所在的【世界坐标】
            Vector3 startWorldPos;
            RectTransformUtility.ScreenPointToWorldPointInRectangle(
                flyingLayer as RectTransform, // 以此为参考平面
                spawnScreenPos,               // 输入屏幕点
                uiCamera,                     // 输入相机
                out startWorldPos             // 输出世界坐标
            );

            // D. 获取目标的【世界坐标】
            Vector3 endWorldPos = FlyTarget.position;
            // 修正 Z 轴：强制让终点 Z 值等于起点 Z 值，保证在同一平面飞行，防止被背景遮挡
            endWorldPos.z = startWorldPos.z;

            // 2. 初始化飞行逻辑
            // 2. 初始化飞行逻辑 (传入计算好的两个世界坐标)
            flyer.Initialize(icon, startWorldPos, endWorldPos, () =>
            {
                // --- 飞行结束后的回调 ---

                // A. 视觉反馈
                StartCoroutine(ShakeBackpack());

            });
        }

        private IEnumerator ShakeBackpack()
        {

            if(OverworldHUDPanel.Instance == null)
            {
                yield break;
            }

            //Vector3 originalPos = FlyTarget.anchoredPosition;
            //float elapsed = 0.0f;
            //float duration = 0.2f;
            //float magnitude = 10f; // 抖动幅度

            //while (elapsed < duration)
            //{
            //    float x = Random.Range(-1f, 1f) * magnitude;
            //    float y = Random.Range(-1f, 1f) * magnitude;

            //    backpackTarget.anchoredPosition = originalPos + new Vector3(x, y, 0);

            //    elapsed += Time.deltaTime;
            //    yield return null;
            //}

            //backpackTarget.anchoredPosition = originalPos;
        }
    }


}