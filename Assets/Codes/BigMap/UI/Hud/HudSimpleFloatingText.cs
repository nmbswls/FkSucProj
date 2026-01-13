


using TMPro;
using UnityEngine;

namespace My.UI
{
    public class HudSimpleFloatingText : MonoBehaviour
    {
        [Header("设置")]
        public float moveSpeed = 2f;      // 向上飘动的速度
        public float fadeDuration = 1f;   // 消失所需时间
        private Vector3 offset = new Vector3(-0.05f, 0, 0); // 生成时的偏移量

        private TextMeshProUGUI textMesh; // 如果是 Legacy Text，改为 private Text textMesh;
        private float alpha;
        private float timer;

        void Awake()
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
            // 如果是 Legacy Text: textMesh = GetComponent<Text>();
        }

        public void Setup(string text, Vector3 startPos, Color? color = null)
        {
            // 设置文字内容
            textMesh.text = text;

            // 如果传了颜色就设置颜色，否则使用预制体默认颜色
            if (color.HasValue)
            {
                textMesh.color = color.Value;
            }

            // 设置初始位置 (世界坐标转UI坐标的逻辑通常由管理器处理，这里假设传入的是正确位置)
            // 或者如果是 World Space 的 Canvas，直接设置 position 即可
            transform.position = startPos + offset;

            // 重置状态
            alpha = 1f;
            timer = 0f;

            // 确保完全不透明开始
            SetAlpha(1f);
        }

        void Update()
        {
            // 1. 向上移动
            transform.position += Vector3.left * moveSpeed * Time.deltaTime;

            // 2. 处理淡出
            timer += Time.deltaTime;
            if (timer > 0)
            {
                // 计算当前的 Alpha 值 (从 1 变到 0)
                float progress = timer / fadeDuration;
                alpha = Mathf.Lerp(1f, 0f, progress);

                SetAlpha(alpha);

                // 3. 消失后销毁物体
                if (timer >= fadeDuration)
                {
                    Destroy(gameObject);
                }
            }
        }

        private void SetAlpha(float a)
        {
            Color c = textMesh.color;
            c.a = a;
            textMesh.color = c;
        }
    }


}
