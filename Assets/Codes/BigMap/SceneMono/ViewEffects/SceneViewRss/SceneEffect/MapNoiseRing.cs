using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapNoiseRing : MonoBehaviour
{
    [Header("核心参数")]
    [Range(0f, 1f)] public float baseAlpha = 0.8f;     // 初始透明度
    public float minDuration = 0.25f;                  // 最快扩散时间（噪音最大时）
    public float maxDuration = 1.5f;                   // 最慢扩散时间（噪音最小时）
    public float minScale = 0.1f;                      // 初始缩放
    public float maxScaleAtMaxNoise = 4f;              // 噪音=1时的目标缩放
    public AnimationCurve noiseToDuration = AnimationCurve.EaseInOut(0, 1, 1, 0);
    // 上面曲线：输入noise[0..1] 输出一个[0..1]的t，之后映射到min/maxDuration（你也可以直接用线性映射）

    [Header("外观")]
    public Color ringColor = Color.white;
    public bool useAdditiveMaterial = false; // 使用加色材质可更像特效

    [Header("回收")]
    public bool autoDestroy = true;
    public float extraLife = 0.05f; // 动画结束后延时销毁/回收

    private SpriteRenderer _sr;
    private Tweener _scaleTween;
    private Tweener _alphaTween;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (useAdditiveMaterial && _sr.material != null)
        {
            // 可在项目里准备一个加色材质 "Sprites/Default-Additive"
            // _sr.material = Resources.Load<Material>("Sprites/Default-Additive");
        }
    }

    /// <summary>
    /// 播放一次噪音环动画
    /// noiseStrength: 0..1（0=极小，1=极大）
    /// worldPos: 噪音中心世界坐标
    /// </summary>
    public void Play(float noiseStrength, Vector3 worldPos)
    {
        transform.position = worldPos;

        // 取消旧Tween
        _scaleTween?.Kill();
        _alphaTween?.Kill();

        // 初始状态
        transform.localScale = Vector3.one * minScale;

        // 颜色 & alpha
        var c = ringColor;
        c.a = baseAlpha * Mathf.Clamp01(noiseStrength * 0.8f + 0.2f); // 噪音越大初始越亮
        _sr.color = c;

        // 计算目标缩放 & 时长
        float targetScale = Mathf.Lerp(minScale * 1.2f, maxScaleAtMaxNoise, Mathf.Clamp01(noiseStrength));
        float t01 = Mathf.Clamp01(noiseToDuration.Evaluate(Mathf.Clamp01(noiseStrength)));
        float duration = Mathf.Lerp(maxDuration, minDuration, t01); // 噪音大 -> 时间短（扩散快）

        // 扩散（缩放）动画
        _scaleTween = transform.DOScale(targetScale, duration)
            .SetEase(Ease.OutCubic);

        // 透明度淡出
        _alphaTween = DOTween.To(
            () => _sr.color,
            col => _sr.color = col,
            new Color(c.r, c.g, c.b, 0f),
            duration
        ).SetEase(Ease.InQuad);

        // 结束回收/销毁
        DOVirtual.DelayedCall(duration + extraLife, () =>
        {
            if (autoDestroy)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        });
    }

    private void OnDisable()
    {
        _scaleTween?.Kill();
        _alphaTween?.Kill();
    }
}
