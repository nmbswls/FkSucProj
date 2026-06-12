
using Animancer;
using System;
using UnityEngine;

// 场景特效入口：绑定信息、progress 状态与可序列化配置；复杂表现由同 GameObject 上的其他组件订阅实现
public class MapSceneEffectCtrl : MonoBehaviour
{
    public enum EEffectType
    {
        SimpleAnim,
        Particle,
    }

    public AnimancerComponent AnimancerComp;
    public AnimationClip ShowClip;

    [SerializeField] Vector2 unitBindOffset = new Vector2(0f, 0.55f);

    public float Progress01 { get; private set; }
    public int BoundEffectUniqId { get; private set; }
    public long? BindingUnitId { get; private set; }
    public int BindingRandomSeed { get; private set; }
    public Vector2 UnitBindOffset => unitBindOffset;

    public event Action OnShown;
    public event Action<float> OnProgressChanged;

    internal void BindFromManager(MapSceneEffectManager.EffectCtx ctx)
    {
        BoundEffectUniqId = ctx != null ? ctx.UniqId : 0;
        BindingUnitId = ctx?.BindingUnit;

        if (BindingUnitId != null)
        {
            BindingRandomSeed = (int)(BindingUnitId.Value % int.MaxValue);
        }
        else
        {
            BindingRandomSeed = UnityEngine.Random.Range(1, 99999);
        }

        if (ctx?.BindingUnit != null)
        {
            ctx.BindingUnitVec = new Vector3(unitBindOffset.x, unitBindOffset.y, 0f);
        }
    }

    public MapSceneEffectManager.EffectCtx GetBoundEffectCtx()
    {
        if (BoundEffectUniqId == 0 || MapSceneEffectManager.Instance == null)
        {
            return null;
        }

        return MapSceneEffectManager.Instance.FindSceneEffect(BoundEffectUniqId);
    }

    public void SetProgress01(float progress01)
    {
        Progress01 = Mathf.Clamp01(progress01);
        OnProgressChanged?.Invoke(Progress01);
    }

    public void Show()
    {
        Progress01 = 0f;

        if (AnimancerComp != null && ShowClip != null)
        {
            AnimancerComp.Play(ShowClip, 0, FadeMode.FromStart);
        }

        OnShown?.Invoke();
    }
}
