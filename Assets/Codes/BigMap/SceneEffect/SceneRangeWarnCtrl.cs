using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace My
{

    public class SceneRangeWarnCtrl : MonoBehaviour
    {
        public enum WarnShape { Circle, Rect }
        public enum FillDirection { PosX, NegX, PosY, NegY }

        public WarnShape shape = WarnShape.Circle;
        public float chargeTime = 2f;       // 蓄力时间（秒）
        public float radius = 2f;           // 圆形半径
        public Vector2 size = new Vector2(4f, 2f); // 矩形宽高
        public FillDirection direction = FillDirection.PosX;

        public Color baseColor = new Color(1f, 0.6f, 0.6f, 0.35f);
        public Color fillColor = new Color(1f, 0f, 0f, 0.6f);
        public float edgeSoftness = 0.05f;

        public UnityEvent onChargeComplete; // 填满回调

        [Header("Materials")]
        public Material circleMat; // 绑定到圆盘Renderer
        public Material rectMat;   // 绑定到矩形Renderer

        private Material runtimeMat;
        private float timer;
        private bool charging;
        private Renderer rend;

        void Awake()
        {
            rend = GetComponentInChildren<Renderer>();
        }

        private void Start()
        {
        }

        public void StartCharge(float radius, float time)
        {
            this.transform.localScale = new Vector2(radius, radius);
            chargeTime = time;
            timer = 0f;
            charging = true;
            EnsureMaterial();
            ApplyStaticParams(); // 半径/尺寸/颜色等一次性参数
            ApplyProgress(0f);
            SetVisible(true);
        }

        public void StartChargeRect(float width, float len, float time)
        {
            this.transform.localScale = new Vector2(len, width);
            chargeTime = time;
            timer = 0f;
            charging = true;
            EnsureMaterial();
            ApplyStaticParams(); // 半径/尺寸/颜色等一次性参数
            ApplyProgress(0f);
            SetVisible(true);
        }

        public void StopCharge()
        {
            charging = false;
            SetVisible(false);
        }

        void Update()
        {
            if (!charging) return;
            timer += Time.deltaTime;
            float p = Mathf.Clamp01(timer / Mathf.Max(0.0001f, chargeTime));
            ApplyProgress(p);

            if (p >= 1f)
            {
                charging = false;
                onChargeComplete?.Invoke();
                // 可选择自动隐藏：
                // SetVisible(false);
            }
        }

        private void EnsureMaterial()
        {
            if (shape == WarnShape.Circle)
            {
                runtimeMat = new Material(circleMat);
            }
            else
            {
                runtimeMat = new Material(rectMat);
            }
            if (rend != null) rend.sharedMaterial = runtimeMat;
        }

        private void ApplyStaticParams()
        {
            if (runtimeMat == null) return;
            runtimeMat.SetColor("_BaseColor", baseColor);
            runtimeMat.SetColor("_FillColor", fillColor);
            runtimeMat.SetFloat("_Softness", edgeSoftness);

            if (shape == WarnShape.Circle)
            {
                runtimeMat.SetFloat("_Radius", radius);
            }
            else
            {
                runtimeMat.SetVector("_Size", new Vector4(size.x, size.y, 0, 0));
                runtimeMat.SetFloat("_Direction", (int)direction);
            }
        }

        private void ApplyProgress(float p)
        {
            if (runtimeMat == null) return;
            runtimeMat.SetFloat("_Progress", p);
        }

        private void SetVisible(bool vis)
        {
            if (rend != null) rend.enabled = vis;
        }
    }
}



