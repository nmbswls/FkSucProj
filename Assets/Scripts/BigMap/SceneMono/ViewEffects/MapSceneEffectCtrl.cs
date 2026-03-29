

using Animancer;
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
    public void Show()
    {
        if(AnimancerComp != null)
        {
            AnimancerComp.Play(ShowClip, 0, FadeMode.FromStart);
        }
    }
}