
using Animancer;
using My;
using UnityEngine;

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

    SceneEffectProgressPresentation _progressPresentation;
    protected int BoundEffectUniqId { get; private set; }

    void Awake()
    {
        _progressPresentation = GetComponent<SceneEffectProgressPresentation>();
    }

    internal void BindFromManager(MapSceneEffectManager.EffectCtx ctx)
    {
        BoundEffectUniqId = ctx != null ? ctx.UniqId : 0;
        OnEffectBound(ctx);
    }

    protected virtual void OnEffectBound(MapSceneEffectManager.EffectCtx ctx)
    {
        if (ctx?.BindingUnit != null)
        {
            ctx.BindingUnitVec = new Vector3(unitBindOffset.x, unitBindOffset.y, 0f);
        }
    }

    protected MapSceneEffectManager.EffectCtx GetBoundEffectCtx()
    {
        if (BoundEffectUniqId == 0 || MapSceneEffectManager.Instance == null)
        {
            return null;
        }

        return MapSceneEffectManager.Instance.FindSceneEffect(BoundEffectUniqId);
    }

    public virtual void SetProgress01(float progress01)
    {
        _progressPresentation?.Apply(progress01);
    }

    public virtual void Show()
    {
        if (AnimancerComp != null && ShowClip != null)
        {
            AnimancerComp.Play(ShowClip, 0, FadeMode.FromStart);
        }

        _progressPresentation?.ResetPresentation();
    }
}
