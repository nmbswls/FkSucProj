using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    // 内城背景 NPC：10 套样式池化，供 HomeFacilityPresenter 租用
    public static class HomeBgNpcPool
    {
        public const int StyleCount = 10;

        private static readonly Color[] StyleTints =
        {
            Color.white,
            new Color(1f, 0.92f, 0.92f, 1f),
            new Color(0.92f, 0.95f, 1f, 1f),
            new Color(0.92f, 1f, 0.94f, 1f),
            new Color(1f, 0.98f, 0.88f, 1f),
            new Color(0.94f, 0.90f, 1f, 1f),
            new Color(0.88f, 0.96f, 1f, 1f),
            new Color(1f, 0.90f, 0.82f, 1f),
            new Color(0.90f, 1f, 0.98f, 1f),
            new Color(0.96f, 0.92f, 0.88f, 1f),
        };

        private static Transform _poolRoot;
        private static GameObject _template;
        private static readonly List<Queue<HomeBgNpc>> Pools = new();

        private static void EnsureInit()
        {
            if (_poolRoot != null)
            {
                return;
            }

            var go = new GameObject("HomeBgNpcPoolRoot");
            Object.DontDestroyOnLoad(go);
            _poolRoot = go.transform;

            for (int i = 0; i < StyleCount; i++)
            {
                Pools.Add(new Queue<HomeBgNpc>());
            }

            _template = Resources.Load<GameObject>("Home/BgNpc/HomeBgNpcTemplate");
        }

        public static Color GetStyleTint(int styleId)
        {
            return StyleTints[Mathf.Abs(styleId) % StyleCount];
        }

        public static HomeBgNpc Rent(int styleId, Transform parent)
        {
            EnsureInit();
            styleId = Mathf.Clamp(styleId, 0, StyleCount - 1);
            var q = Pools[styleId];
            HomeBgNpc npc = null;
            if (q.Count > 0)
            {
                npc = q.Dequeue();
            }
            else
            {
                npc = CreateNewInstance(styleId);
            }

            if (npc == null)
            {
                return null;
            }

            npc.transform.SetParent(parent, false);
            npc.gameObject.SetActive(true);
            npc.ApplyStyle(styleId);
            return npc;
        }

        public static void Return(HomeBgNpc npc)
        {
            if (npc == null)
            {
                return;
            }

            EnsureInit();
            npc.StopFacilityRoutine();
            npc.gameObject.SetActive(false);
            npc.transform.SetParent(_poolRoot, false);
            int sid = Mathf.Clamp(npc.LastStyleId, 0, StyleCount - 1);
            Pools[sid].Enqueue(npc);
        }

        private static HomeBgNpc CreateNewInstance(int styleId)
        {
            GameObject go;
            if (_template != null)
            {
                go = Object.Instantiate(_template, _poolRoot);
            }
            else
            {
                go = new GameObject($"PooledHomeBgNpc_{styleId}");
                go.transform.SetParent(_poolRoot, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Home/BgNpc/fallback_dot");
                if (sr.sprite == null)
                {
                    var tex = Texture2D.whiteTexture;
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }

                sr.sortingOrder = 5;
                go.AddComponent<HomeBgNpc>();
            }

            var comp = go.GetComponent<HomeBgNpc>();
            if (comp == null)
            {
                comp = go.AddComponent<HomeBgNpc>();
            }

            return comp;
        }
    }
}
