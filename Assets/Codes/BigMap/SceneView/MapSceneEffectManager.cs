using Map.Scene;
using My;
using My.Map;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSceneEffectManager : MonoBehaviour
{
    public static MapSceneEffectManager Instance;

    public int EffectUniqIdCounter = 1;
    public class EffectCtx
    {
        public int UniqId;
        public GameObject EffectGo;
        public float CleanUpTimer;
    }

    public List<EffectCtx> ctxs = new();

    private Dictionary<string, GameObject> _innerPrefabPool = new();


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);
    }


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

    /// <summary>
    /// œ‘ æÃÿ–ß
    /// </summary>
    /// <param name="originPos"></param>
    /// <param name="duration"></param>
    /// <param name="effectName"></param>
    /// <returns></returns>
    public EffectCtx ShowSceneEffect(Vector2 originPos, float duration, string effectName)
    {
        var prefab = Resources.Load<GameObject>($"SceneEffect/{effectName}");
        if (prefab == null) return null;
        var go = GameObject.Instantiate(prefab, MainGameManager.Instance.SceneEffectLayer);
        int id = EffectUniqIdCounter++;

        var ctx = new EffectCtx();
        ctx.UniqId = id;
        ctx.EffectGo = go;
        ctx.CleanUpTimer = LogicTime.time + duration;
        ctxs.Add(ctx);
        return ctx;
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
