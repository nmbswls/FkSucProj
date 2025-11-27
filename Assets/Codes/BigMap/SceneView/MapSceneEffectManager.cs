using Map.Scene;
using My;
using My.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSceneEffectManager : MonoBehaviour
{
    public int EffectUniqIdCounter = 1;
    public class EffectCtx
    {
        public int UniqId;
        public GameObject EffectGo;
        public float CleanUpTimer;
    }

    public List<EffectCtx> ctxs = new();

    public GameObject SceneRangeWarn_Circle;
    public GameObject SceneRangeWarn_Rect;

    public void Update()
    {
        for(int i=ctxs.Count - 1; i>=0;i--)
        {
            if(LogicTime.time > ctxs[i].CleanUpTimer)
            {
                GameObject.Destroy(ctxs[i].EffectGo);
                ctxs.RemoveAt(i);
            }
        }
    }

    public int ShowSceneWarnRangeCircle(Vector2 centerPos, Vector2 dir, float radius, float duration, Vector2 offset)
    {
        int id = EffectUniqIdCounter++;

        var go = GameObject.Instantiate(SceneRangeWarn_Circle, MainGameManager.Instance.SceneEffectLayer);

        go.transform.position = centerPos;

        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.eulerAngles = new Vector3(0, 0, angle);

        var ctrl = go.GetComponent<SceneRangeWarnCtrl>();
        ctrl.StartCharge(radius, duration);

        var mainView = go.transform.GetChild(0);
        mainView.localPosition = offset;

        var ctx = new EffectCtx();
        ctx.UniqId = id;
        ctx.EffectGo = go;

        ctx.CleanUpTimer = LogicTime.time + duration;
        ctxs.Add(ctx);
        return id;
    }

    public int ShowSceneWarnRangeRect(Vector2 centerPos, Vector2 dir, float width, float len, float duration, Vector2 offset)
    {
        int id = EffectUniqIdCounter++;

        var go = GameObject.Instantiate(SceneRangeWarn_Rect, MainGameManager.Instance.SceneEffectLayer);

        go.transform.position = centerPos;

        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        go.transform.eulerAngles = new Vector3(0, 0, angle);

        var ctrl = go.GetComponent<SceneRangeWarnCtrl>();

        var mainView = go.transform.GetChild(0);
        mainView.localPosition = offset;

        ctrl.StartChargeRect(width, len, duration);
        var ctx = new EffectCtx();
        ctx.UniqId = id;
        ctx.EffectGo = go;

        ctx.CleanUpTimer = LogicTime.time + duration;
        ctxs.Add(ctx);
        return id;
    }

    public void UpdateSceneWarnRangeRect(int effectId, Vector2 centerPos, Vector2 dir)
    {
        var findIt = ctxs.Find((item) => item.UniqId == effectId);
        if(findIt == null)
        {
            return;
        }
        findIt.EffectGo.transform.position = centerPos;

        var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        findIt.EffectGo.transform.eulerAngles = new Vector3(0, 0, angle);
    }

    public void ForceDestroy(int id)
    {
        var findIt = ctxs.Find((item) => item.UniqId == id);
        if (findIt != null)
        {
            GameObject.Destroy(findIt.EffectGo);
            ctxs.Remove(findIt);
        }
    }
}
