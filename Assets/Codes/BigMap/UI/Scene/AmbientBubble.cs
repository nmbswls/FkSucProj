using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map
{
    /// <summary>
    /// 氛围感
    /// </summary>
    public class AmbientBubble : MonoBehaviour
    {
        [Header("UI 组件")]
        public TextMeshProUGUI contentText;
        public Image MainBubble;

        public CanvasGroup canvasGroup;

        public RectTransform rectTransform;

        [Header("基础动画参数")]
        public float floatDistance = 80f; // 向上漂浮距离
        public float lifeTime = 5f;       // 存活时间
        public float fadeTime = 0.8f;     // 淡入淡出时间

        [Header("氛围感设置 (透视)")]
        public float sideRotationAngle = 25f; // 侧边倾斜角度

        // ---------------------------------------------------------
        // 新增：整合进来的 Jitter (文字沸腾) 参数
        // ---------------------------------------------------------
        [Header("文字沸腾特效 (Vertex Jitter)")]
        [Tooltip("角度抖动幅度")]
        public float JitterAngleMultiplier = 1.5f;
        [Tooltip("位置波动幅度 (值越小越细腻，越大越狂乱)")]
        public float JitterCurveScale = 0.6f;

        // 内部变量
        private bool isInitialized = false;

        public int Style = 0;
        public Transform NormalCornor;
        public Transform SpeModeCornor;

        public Color NormalBgColor;
        public Color SpeModeBgColor;
        public Color NormalTextColor;
        public Color SpeModeTextColor;

        private List<TextMeshProUGUI> cornerTextList = new();

        private void Awake()
        {
            for(int i=0;i< NormalCornor.childCount;i++)
            {
                var comp = NormalCornor.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (comp == null) continue;
                cornerTextList.Add(comp);
            }
            for (int i = 0; i < SpeModeCornor.childCount; i++)
            {
                var comp = SpeModeCornor.GetChild(i).GetComponent<TextMeshProUGUI>();
                if (comp == null) continue;
                cornerTextList.Add(comp);
            }
        }

        // ---------------------------------------------------------
        // 初始化与生命周期
        // ---------------------------------------------------------
        public void Setup(string text, Vector2 startPos, float scale, Vector2 globalScaleRange, int side, int style = 0)
        {
            // 1. 基础设置
            this.Style = style;

            contentText.text = text;
            rectTransform.anchoredPosition = startPos;
            transform.localScale = Vector3.one * scale;

            // 2. 应用透视和对齐 (边缘氛围感核心)
            ApplyAtmosphereStyle(side);

            // 3. 计算颜色分层 (近实远虚)
            float normalizedScale = Mathf.InverseLerp(globalScaleRange.x, globalScaleRange.y, scale);
            float targetAlpha = Mathf.Lerp(0.7f, 1f, normalizedScale);

            // 初始设为全透明
            canvasGroup.alpha = 0;

            // 标记初始化完成，开始在Update中运行Jitter
            isInitialized = true;

            // 4. 开始 DoTween 动画序列
            AnimateBubble(targetAlpha);

            RefreshStyle();
        }

        void Update()
        {
            // 每一帧更新顶底数据，产生沸腾效果
            if (isInitialized && contentText != null)
            {
                ApplyVertexJitter(contentText);
                foreach(var oneText in cornerTextList)
                {
                    ApplyVertexJitter(oneText);
                }
            }
        }

        // ---------------------------------------------------------
        // 动画逻辑 (DoTween)
        // ---------------------------------------------------------
        void AnimateBubble(float targetAlpha)
        {
            // 淡入
            canvasGroup.DOFade(targetAlpha, fadeTime);

            // 漂浮移动
            Vector2 endPos = rectTransform.anchoredPosition;
            endPos.y += floatDistance;
            rectTransform.DOAnchorPos(endPos, lifeTime).SetEase(Ease.Linear);

            // 整体 UI 块的微弱晃动 (模拟手持感)
            rectTransform.DOShakeAnchorPos(lifeTime, strength: 2f, vibrato: 3, randomness: 90, snapping: false);

            // 销毁流程
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(lifeTime - fadeTime);
            seq.Append(canvasGroup.DOFade(0f, fadeTime));
            seq.OnComplete(() => Destroy(gameObject));
        }

        void ApplyAtmosphereStyle(int side)
        {
            rectTransform.localRotation = Quaternion.identity;

            switch (side)
            {
                case 0: // 左侧：向右看
                    rectTransform.localRotation = Quaternion.Euler(0, sideRotationAngle, 0);
                    //contentText.alignment = TextAlignmentOptions.MidlineLeft;
                    break;
                case 1: // 右侧：向左看
                    rectTransform.localRotation = Quaternion.Euler(0, -sideRotationAngle, 0);
                    //contentText.alignment = TextAlignmentOptions.MidlineRight;
                    break;
                case 2: // 上方
                    rectTransform.localRotation = Quaternion.Euler(10, 0, 0);
                    break;
                case 3: // 下方
                    rectTransform.localRotation = Quaternion.Euler(-10, 0, 0);
                    break;
            }
        }


        void RefreshStyle()
        {
            NormalCornor.gameObject.SetActive(false);
            SpeModeCornor.gameObject.SetActive(false);

            if (Style == 0)
            {
                NormalCornor.gameObject.SetActive(true);
                MainBubble.color = new Color();

                MainBubble.color = NormalBgColor;
                contentText.color = NormalTextColor;
            }
            else
            {
                SpeModeCornor.gameObject.SetActive(true);
                MainBubble.color = new Color();

                MainBubble.color = SpeModeBgColor;
                contentText.color = SpeModeTextColor;
            }
        }

        // ---------------------------------------------------------
        // 核心特效：顶点噪点 (原 TextJitterEffect 逻辑)
        // ---------------------------------------------------------
        void ApplyVertexJitter(TextMeshProUGUI textPro)
        {
            // 强制刷新网格信息
            textPro.ForceMeshUpdate();
            TMP_TextInfo textInfo = textPro.textInfo;

            // 遍历所有字符
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // 跳过不可见字符（空格等）
                if (!charInfo.isVisible) continue;

                // 获取顶点索引
                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;
                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                // --- 核心算法：随机矩阵变换 ---

                // 1. 计算随机偏移 (Jitter)
                Vector3 jitterOffset = new Vector3(
                    Random.Range(-0.25f, 0.25f),
                    Random.Range(-0.25f, 0.25f),
                    0
                ) * JitterCurveScale;

                // 2. 计算随机旋转
                float randomAngle = Random.Range(-5f, 5f) * JitterAngleMultiplier;
                Quaternion jitterRot = Quaternion.Euler(0, 0, randomAngle);

                // 3. 构建变换矩阵
                Matrix4x4 matrix = Matrix4x4.TRS(jitterOffset, jitterRot, Vector3.one);

                // 4. 应用到字符的四个顶点
                // 为了让旋转围绕字符中心，需要先平移到中心，旋转，再移回去
                Vector3 charCenter = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2;

                vertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 0] - charCenter) + charCenter;
                vertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 1] - charCenter) + charCenter;
                vertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 2] - charCenter) + charCenter;
                vertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(vertices[vertexIndex + 3] - charCenter) + charCenter;
            }

            // 将修改后的顶点数据推送到Mesh
            textPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
