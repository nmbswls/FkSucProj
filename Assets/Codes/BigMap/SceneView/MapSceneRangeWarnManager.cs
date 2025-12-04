using Map.Scene;
using My;
using My.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public class MapSceneRangeWarnManager : MonoBehaviour
    {
        
        public string SceneRangeWarn_Circle = "SceneRangeWarn_Circle";
        public string SceneRangeWarn_Rect = "SceneRangeWarn_Rect";

        public Dictionary<int, SceneRangeWarnCtrl> _warnEffectDict = new();


        public int ShowSceneWarnRangeCircle(Vector2 centerPos, Vector2 dir, float radius, float duration, Vector2 offset)
        {
            var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(centerPos, duration, SceneRangeWarn_Circle);
            if(fxCtx == null)
            {
                return 0;
            }

            var comp = fxCtx.EffectGo.GetComponent<SceneRangeWarnCtrl>();
            comp.transform.position = centerPos;

            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            comp.transform.eulerAngles = new Vector3(0, 0, angle);

            comp.StartCharge(radius, duration);

            var mainView = comp.transform.GetChild(0);
            mainView.localPosition = offset;

            return fxCtx.UniqId;
        }

        public int ShowSceneWarnRangeRect(Vector2 centerPos, Vector2 dir, float width, float len, float duration, Vector2 offset)
        {
            var fxCtx = MapSceneEffectManager.Instance.ShowSceneEffect(centerPos, duration, SceneRangeWarn_Rect);
            if (fxCtx == null)
            {
                return 0;
            }

            var comp = fxCtx.EffectGo.GetComponent<SceneRangeWarnCtrl>();
            comp.transform.position = centerPos;


            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            comp.transform.eulerAngles = new Vector3(0, 0, angle);

            var mainView = comp.transform.GetChild(0);
            mainView.localPosition = offset;

            comp.StartChargeRect(width, len, duration);
            return fxCtx.UniqId;
        }

        public void UpdateSceneWarnRangeRect(int effectId, Vector2 centerPos, Vector2 dir)
        {
            _warnEffectDict.TryGetValue(effectId, out var fxCtx);
            if (fxCtx == null || fxCtx.gameObject == null)
            {
                return;
            }

            fxCtx.transform.position = centerPos;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            fxCtx.transform.eulerAngles = new Vector3(0, 0, angle);
        }

    }
}

