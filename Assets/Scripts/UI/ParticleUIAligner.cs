using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My
{
    public class ParticleUIAligner : MonoBehaviour
    {
        [Header("引用")]
        public Canvas canvas;
        public Camera uiCamera;
        public RectTransform targetUIElement;  // 需要对齐的UI元素

        private ParticleSystem ps;

        void Start()
        {
            ps = GetComponent<ParticleSystem>();
            AlignToUI();
        }

        /// <summary>
        /// 将粒子系统对齐到指定UI元素的世界坐标
        /// </summary>
        public void AlignToUI()
        {
            // 获取UI元素的世界坐标
            Vector3 worldPos = GetUIWorldPosition(targetUIElement);
            transform.position = worldPos;
        }

        private Vector3 GetUIWorldPosition(RectTransform rectTransform)
        {
            // RectTransform -> 世界坐标（适用于Camera模式）
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // 返回中心点
            return (corners[0] + corners[2]) / 2f;
        }
    } 
}