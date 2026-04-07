using Map.Scene;
using My;
using My.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MapSceneEffectManager;

public class MapSceneEffectManager : MonoBehaviour
{
    public static MapSceneEffectManager Instance;

    public Transform PoolCacheRoot;
    public int EffectUniqIdCounter = 1;
    public class EffectCtx
    {
        public int UniqId;
        public string EffectName;
        public GameObject EffectGo;
        public MapSceneEffectCtrl EffectCtrl;
        public float CleanUpTimer;

        public long? BindingUnit = null;
        public Vector3 BindingUnitVec;
    }

    public List<EffectCtx> ctxs = new();

    private Dictionary<string, GameObject> _innerPrefabPool = new();
    private Dictionary<string, Queue<GameObject>> _innerObjPool = new();

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);

        GameObject go = new GameObject();
        go.name = "cache";

        go.transform.SetParent(this.transform);

        PoolCacheRoot = go.transform;
    }


    public void Update()
    {
        for(int i=ctxs.Count - 1; i>=0;i--)
        {
            if (ctxs[i].BindingUnit != null)
            {
                var pres = SceneAOIManager.Instance.GetActivePresentation(ctxs[i].BindingUnit.Value);
                if (pres != null)
                {
                    if (!ctxs[i].EffectGo.activeSelf)
                    {
                        ctxs[i].EffectGo.SetActive(true);
                    }

                    ctxs[i].EffectGo.transform.position = pres.GetWorldPosition() + ctxs[i].BindingUnitVec;
                }
                else
                {
                    if(ctxs[i].EffectGo.activeSelf)
                    {
                        ctxs[i].EffectGo.SetActive(false);
                    }
                }
            }

            if(ctxs[i].CleanUpTimer != -1 && LogicTime.time > ctxs[i].CleanUpTimer)
            {
                // 存？
                GameObject.Destroy(ctxs[i].EffectGo);
                ctxs.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 显示特效
    /// </summary>
    /// <param name="originPos"></param>
    /// <param name="duration"></param>
    /// <param name="effectName"></param>
    /// <returns></returns>
    public EffectCtx ShowSceneEffect(Vector2 originPos, float duration, string effectName, long? bindingUnitId, Vector2? dir = null)
    {
        var prefab = Resources.Load<GameObject>($"SceneEffect/{effectName}");
        if (prefab == null) return null;

        _innerObjPool.TryGetValue(effectName, out var ll);
        GameObject newGo = null;
        if(ll == null || ll.Count == 0)
        {
            newGo = GameObject.Instantiate(prefab, MainGameManager.Instance.SceneEffectLayer);
        }
        else
        {
            newGo = ll.Dequeue();
            newGo.SetActive(true);
        }



        int id = EffectUniqIdCounter++;

        var ctx = new EffectCtx();

        newGo.transform.position = originPos;

        if (dir != null && dir != Vector2.zero)
        {
            // 计算角度（弧度转角度）
            float angle = Mathf.Atan2(dir.Value.y, dir.Value.x) * Mathf.Rad2Deg;

            // 赋值给localEulerAngles，仅在Z轴旋转
            transform.localEulerAngles = new Vector3(0, 0, angle);
        }

        ctx.UniqId = id;
        ctx.EffectName = effectName;
        ctx.EffectGo = newGo;
        ctx.BindingUnit = bindingUnitId;

        var ctrl = newGo.GetComponent<MapSceneEffectCtrl>();
        if(ctrl != null)
        {
            ctx.EffectCtrl = ctrl;
            ctx.EffectCtrl.Show();
        }

        if (duration < 0)
        {
            ctx.CleanUpTimer = -1;
        }
        else
        {
            ctx.CleanUpTimer = LogicTime.time + duration;
        }
        ctxs.Add(ctx);
        return ctx;
    }

    /// <summary>
    /// 获取已有场景效果
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public EffectCtx? FindSceneEffect(int id)
    {
        var findIt = ctxs.Find((item) => item.UniqId == id);
        return findIt;
    }

    public void ForceDestroy(int id)
    {
        var findIt = ctxs.Find((item) => item.UniqId == id);
        if (findIt != null)
        {
            _innerObjPool.TryGetValue(findIt.EffectName, out var ll);
            if(ll == null)
            {
                ll = new();
                _innerObjPool[findIt.EffectName] = ll;
            }

            if(ll.Count > 5)
            {
                GameObject.Destroy(findIt.EffectGo);
            }
            else
            {
                findIt.EffectGo.SetActive(false);
                ll.Enqueue(findIt.EffectGo);
            }
            ctxs.Remove(findIt);
        }
    }
}
