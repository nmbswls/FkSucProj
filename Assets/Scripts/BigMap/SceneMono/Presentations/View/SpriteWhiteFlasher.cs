
namespace My.Map
{
    using UnityEngine;
    using System.Collections;

    public class SpriteWhiteFlasher : MonoBehaviour
    {
        public SpriteRenderer sr;

        // 属性 ID 缓存（性能优化：用整数ID比字符串查找快很多）
        private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
        private static readonly int BrightBoostID = Shader.PropertyToID("_BrightBoost");

        [Range(0, 1)] public float peakAmount = 1.0f; // 建议 Shader 里把逻辑写好，这里只传0-1
        private float brightBoost = 0.24f;
        private float duration = 0.3f;

        private Coroutine routine;

        // MPB 缓存：不需要每次 update 都 new 一个
        private MaterialPropertyBlock _mpb;

        void Awake()
        {
            if (!sr) sr = GetComponentInChildren<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();

            // 预先设置好静态属性，避免运行时反复设置
            // 注意：这里需要先获取当前状态，以免覆盖了其他属性
            sr.GetPropertyBlock(_mpb);
            _mpb.SetFloat(BrightBoostID, brightBoost);
            sr.SetPropertyBlock(_mpb);
        }

        // 可以在 Editor 调整参数测试
        [ContextMenu("Test Flash")]
        public void TriggerFlash()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(FlashProcess());
        }

        IEnumerator FlashProcess()
        {
            _savedBaseColor = sr.color;
            yield return FlashProcessCore(multiplyPeakColor: null);
            if (sr != null)
            {
                sr.color = _savedBaseColor;
            }
        }

        IEnumerator FlashProcessCore(Color? multiplyPeakColor)
        {
            float t = 0f;
            float up = duration * 0.35f;
            float down = duration - up;

            while (t < up)
            {
                t += Time.deltaTime;
                float val = Mathf.Lerp(0f, peakAmount, t / up);
                UpdateMaterial(val);
                ApplyOptionalColorTint(multiplyPeakColor, val);
                yield return null;
            }

            t = 0f;

            while (t < down)
            {
                t += Time.deltaTime;
                float val = Mathf.Lerp(peakAmount, 0f, t / down);
                UpdateMaterial(val);
                ApplyOptionalColorTint(multiplyPeakColor, val);
                yield return null;
            }

            UpdateMaterial(0f);
            if (multiplyPeakColor != null && sr != null)
            {
                sr.color = _savedBaseColor;
            }

            routine = null;
        }

        Color _savedBaseColor = Color.white;

        void ApplyOptionalColorTint(Color? multiplyPeakColor, float amountNormalized)
        {
            if (multiplyPeakColor == null || sr == null)
            {
                return;
            }

            float k = amountNormalized * 0.55f;
            Color c = Color.Lerp(_savedBaseColor, multiplyPeakColor.Value, k);
            c.a = _savedBaseColor.a;
            sr.color = c;
        }

        public void TriggerPinkBodyGrazingFlash()
        {
            if (sr == null)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            _savedBaseColor = sr.color;
            var pink = new Color(1f, 0.72f, 0.88f, 1f);
            routine = StartCoroutine(FlashProcessCore(pink));
        }

        [ContextMenu("Test Pink Flash")]
        public void TestPinkFlash()
        {
            TriggerPinkBodyGrazingFlash();
        }

        // 核心优化方法
        private void UpdateMaterial(float amount)
        {
            // 1. 获取当前 PropertyBlock (必须先 Get，否则会丢失之前的其他属性修改)
            sr.GetPropertyBlock(_mpb);

            // 2. 修改我们关注的值
            _mpb.SetFloat(FlashAmountID, amount);

            // 3. 设置回去
            sr.SetPropertyBlock(_mpb);
        }
    }
}