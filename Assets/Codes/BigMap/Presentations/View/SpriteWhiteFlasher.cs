
namespace My.Map
{
    using UnityEngine;
    using System.Collections;

    public class SpriteWhiteFlasher : MonoBehaviour
    {
        public SpriteRenderer sr;
        [Range(0, 1)] public float peakAmount = 1.5f; // 白混强度
        public float brightBoost = 0.5f;
        public float duration = 0.4f;
        private Material mat;
        private Coroutine routine;

        void Awake()
        {
            if (!sr) sr = GetComponent<SpriteRenderer>();
            // 每实例克隆材质，避免全局修改
            mat = Instantiate(sr.sharedMaterial);
            sr.material = mat;
            mat.SetFloat("_BrightBoost", brightBoost);
        }

        public void Update()
        {
            
        }

        public void TriggerFlash()
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            float t = 0f;
            float up = duration * 0.35f;
            float down = duration - up;
            while (t < up)
            {
                t += Time.deltaTime;
                float s = t / up;
                mat.SetFloat("_FlashAmount", Mathf.Lerp(0f, peakAmount, s));
                yield return null;
            }
            t = 0f;
            while (t < down)
            {
                t += Time.deltaTime;
                float s = t / down;
                mat.SetFloat("_FlashAmount", Mathf.Lerp(peakAmount, 0f, s));
                yield return null;
            }
            mat.SetFloat("_FlashAmount", 0f);
            routine = null;
        }
    }
}