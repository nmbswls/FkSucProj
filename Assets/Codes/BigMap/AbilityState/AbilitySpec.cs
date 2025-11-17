using Map.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


namespace Map.Entity
{
    //[Serializable]
    //public class HitWindow
    //{
    //    public PhaseKind Phase;        // 在哪个阶段内
    //    public float StartOffset;      // 相对阶段开始的时间
    //    public float EndOffset;        // 相对阶段开始的时间
    //    // 简化的命中形状参数
    //    public float Radius = 2.5f;
    //    public float AngleDegrees = 140f; // 扇形角度；若=360则为圆环
    //    public float Damage = 30f;
    //    public string DamageType = "Physical";
    //}


    //public enum EffectTrigger
    //{
    //    OnStart,           // Ability刚开始
    //    OnPhaseEnter,      // 进入某阶段（配合Effect内的Phase过滤）
    //    OnPhaseExit,       // 离开某阶段
    //    OnComplete,        // 完成
    //    OnCancel           // 取消
    //}

    

    //public interface IEntityActionEffect
    //{
    //    void Apply(MapAbilitySpecConfig spec, AbilityContext ctx);
    //}

    //[Serializable]
    //public class BaseEntityActioEffect : IEntityActionEffect
    //{
    //    public virtual void Apply(MapAbilitySpecConfig spec, AbilityContext ctx)
    //    {

    //    }
    //}

    

    //[Serializable]
    //// 2) 开门/解锁效果
    //public class OpenDoorEffect : BaseEntityActioEffect
    //{
    //    public bool RequireKey;
    //    public string KeyItemId;

    //    public OpenDoorEffect(bool requireKey, string keyId)
    //    {
    //        RequireKey = requireKey; KeyItemId = keyId;
    //    }

    //    public override void Apply(AbilitySpec spec, AbilityContext ctx)
    //    {
    //        if (ctx.Target == null) { Debug.LogWarning("No door target"); return; }

    //        Debug.Log("OpenDoorEffect open " + ctx.Target.Id);

    //        //var door = ctx.Target.GetComponent<Door>();
    //        //if (door == null) { Debug.LogWarning("Target is not a Door"); return; }

    //        //if (door.IsLocked)
    //        //{
    //        //    if (RequireKey)
    //        //    {
    //        //        var inv = ctx.Actor.GetComponent<Inventory>();
    //        //        if (inv != null && inv.HasItem(KeyItemId))
    //        //        {
    //        //            inv.ConsumeItem(KeyItemId, 1);
    //        //            door.Unlock();
    //        //            door.ToggleOpen(true);
    //        //            Debug.Log("Door unlocked with key and opened.");
    //        //        }
    //        //        else
    //        //        {
    //        //            Debug.Log("Need key to unlock door.");
    //        //        }
    //        //    }
    //        //    else
    //        //    {
    //        //        // 可扩展为转入撬锁能力
    //        //        Debug.Log("Door locked; no key requirement in effect. Consider lockpick.");
    //        //    }
    //        //}
    //        //else
    //        //{
    //        //    door.ToggleOpen(!door.IsOpen);
    //        //    Debug.Log("Door toggled.");
    //        //}
    //    }
    //}

    //[Serializable]
    //public class CameraShakeEffect : BaseEntityActioEffect
    //{
    //    public float Intensity = 0.5f;
    //    public override void Apply(AbilitySpec spec, AbilityContext ctx)
    //    {
    //        Debug.Log($"CameraShake intensity {Intensity}");
    //    }
    //}

    //[Serializable]
    //public class PlayAnimTagEffect : BaseEntityActioEffect
    //{
    //    public string AnimTag;
    //    public PlayAnimTagEffect(string tag)
    //    {
    //    }
    //    public override void Apply(MapAbilitySpecConfig spec, AbilityContext ctx)
    //    {
    //        // 这里应对接 Animator/StateMachine
    //        Debug.Log($"PlayAnim: {AnimTag} at phase {ctx.CurrentPhase}");
    //    }
    //}

    //[Serializable]
    //public class ShowBottomProgressEffect : BaseEntityActioEffect
    //{
    //    public string hintText;
    //    public float progressTime;
    //    public ShowBottomProgressEffect(string hintText, float progressTime)
    //    {
    //        this.hintText = hintText;
    //        this.progressTime = progressTime;
    //    }
    //    public override void Apply(MapAbilitySpecConfig abilityConfig, AbilityContext ctx)
    //    {
    //        // 这里应对接 Animator/StateMachine
    //        Debug.Log($"ShowBottomProgressEffect at phase {ctx.CurrentPhase}");
    //        ctx.viewer?.ShowBottomProgress(hintText, progressTime);
    //    }
    //}
}






public interface ISceneAbilityViewer
{
    long ShowBottomProgress(string hintText, float progressTime);

    void TryCancelButtomProgress(long showId);

    void ShowFakeFxEffect(string hintText, Vector2 logicPos);

    void ShowNoiseEffect(float intensity, Vector2 logicPos);

    void ShowClickkkWindow(string windowType, Vector2 showPos, float duration);

    void CloseClickkkWindow(string windowType, bool isInterrupt);

    void DoDeepZhaquSmallGame(long targetUnitId, object extraParam);
}
