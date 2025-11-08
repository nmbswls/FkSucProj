//using Global.Display.Event;
//using Map.Logic.Events;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;




//public sealed class LogicBridge : MonoBehaviour
//{
//    public DisplayBus Display;
//    public Map.Logic.Events.MapLogicEventBus LogicBus;

//    // 保存订阅句柄
//    private Map.Logic.Events.MapLogicSubscription subSkill;
//    private Map.Logic.Events.MapLogicSubscription subDamage;
//    private Map.Logic.Events.MapLogicSubscription subTransform;

//    private Coroutine eofCoroutine;

//    void Awake()
//    {
//        // 初始化处理器

//        // 订阅逻辑总线，并保存句柄
//        //subSkill = LogicBus.Subscribe(skillToAnim);
//        //subDamage = LogicBus.Subscribe(dmgToUI);
//        //subTransform = LogicBus.Subscribe(tfToView);

//        // 启动 EOF 协程
//        eofCoroutine = StartCoroutine(EndOfFramePump());
//    }

//    void Update() { Display.PumpUpdate(); }
//    void FixedUpdate() { Display.PumpFixed(); }

//    System.Collections.IEnumerator EndOfFramePump()
//    {
//        while (true)
//        {
//            yield return new WaitForEndOfFrame();
//            Display.PumpEndOfFrame();
//        }
//    }

//    void OnDestroy()
//    {
//        // 停止协程
//        if (eofCoroutine != null) StopCoroutine(eofCoroutine);

//        // 解除逻辑层订阅
//        if (subSkill != null) LogicBus.Unsubscribe(subSkill);
//        if (subDamage != null) LogicBus.Unsubscribe(subDamage);
//        if (subTransform != null) LogicBus.Unsubscribe(subTransform);

//        // 如显示层也有直接订阅（例如单独的显示处理器注册到 DisplayBus），同样需要保存并解绑：
//        // Display.Unsubscribe(displaySubX);
//    }
//}