using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.Map
{
    public class GhostTrailSpawner : MonoBehaviour
    {
        public bool IsShowing = false;


        [Header("Ghost Settings")]
        public GameObject target;                  // 要生成残影的对象（带SpriteRenderer）
        public Material ghostMaterial;             // 半透明残影材质（Sprite用的透明shader/URP Sprite-Unlit）
        public float spawnInterval = 0.05f;        // 残影生成间隔
        public float ghostLife = 0.3f;             // 残影存活时间
        public int poolSize = 32;                  // 对象池大小
        public float minSpeedToSpawn = 0.1f;       // 速度阈值（2D也使用世界坐标）

        private float _timer;

        public Transform SelfGhostContainer;
        List<GameObject> ghosts = new List<GameObject>();
        private readonly Queue<GameObject> _pool = new Queue<GameObject>();
        private Vector3 _lastPos;

        //private SpriteRenderer _targetSR;
        private List<SpriteRenderer> _targetSrList = new();

        void Awake()
        {
            if (target == null)
            {
                Debug.LogError("GhostTrailSpawner2D: target is null.");
                enabled = false;
                return;
            }

            for(int i=0;i<target.transform.childCount;i++)
            {
                var sr = target.transform.GetChild(i).GetComponent<SpriteRenderer>();
                if (sr == null) continue;
                _targetSrList.Add(sr);
            }
           

            _lastPos = target.transform.position;

            // 预建对象池
            for (int i = 0; i < poolSize; i++)
            {
                var ghost = CreateGhostInstance();
                ghost.SetActive(false);
                _pool.Enqueue(ghost);
            }
        }

        void Update()
        {
            if (!IsShowing) return;

            float speed = (target.transform.position - _lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-6f);
            _lastPos = target.transform.position;

            _timer += Time.deltaTime;
            if (_timer >= spawnInterval && speed > minSpeedToSpawn)
            {
                _timer = 0f;
                SpawnGhost();
            }
        }

        GameObject CreateGhostInstance()
        {
            var ghost = new GameObject("Ghost2D");
            ghost.transform.SetParent(MainGameManager.Instance.SceneEffectLayer);
            ghost.transform.localScale = target.transform.localScale;

            // 添加并配置 SpriteRenderer
            var sr = ghost.AddComponent<SpriteRenderer>();

            // 残影材质（可选），若为空则继承默认材质
            if (ghostMaterial != null)
            {
                sr.sharedMaterial = ghostMaterial;
            }

            // 添加淡出脚本
            var fade = ghost.AddComponent<GhoseTrailFader>();
            fade.life = ghostLife;

            return ghost;
        }

        void SpawnGhost()
        {
            if (target == null || _targetSrList.Count == 0) return; // 目标已销毁或缺组件
            var ghost = GetGhostFromPool();
            if (!ghost) ghost = CreateGhostInstance();

            var sr = ghost.GetComponent<SpriteRenderer>();
            if (sr == null) return;

            SpriteRenderer activeSr = null;
            foreach(var targetSr in _targetSrList)
            {
                if(targetSr.gameObject.activeSelf)
                {
                    activeSr = targetSr;
                    break;
                }
            }

            if (activeSr == null) return;
            // 拷贝属性
            sr.sprite = activeSr.sprite;
            sr.flipX = activeSr.flipX;
            sr.flipY = activeSr.flipY;
            sr.sortingLayerID = activeSr.sortingLayerID;
            sr.sortingOrder = activeSr.sortingOrder - 1;
            sr.color = activeSr.color;

            // 位置与朝向
            ghost.transform.SetPositionAndRotation(target.transform.position, target.transform.rotation);
            ghost.transform.position += activeSr.transform.localPosition;
            ghost.transform.localScale = activeSr.transform.localScale;

            if(target.transform.localScale.x < 0)
            {
                ghost.transform.localScale = new Vector3(-ghost.transform.localScale.x, ghost.transform.localScale.y, ghost.transform.localScale.z);
            }
            ghost.SetActive(true);

            // 重置淡出
            var fader = ghost.GetComponent<GhoseTrailFader>();
            if (fader != null) fader.ResetLife(ghostLife, sr.color);

            ghosts.Add(ghost);
            // 启动回收
            StartCoroutine(RecycleAfter(ghost, ghostLife));
        }

        System.Collections.IEnumerator RecycleAfter(GameObject ghost, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!this) yield break; // 管理器被销毁
            if (!ghost) yield break; // 幽灵已销毁
            ghost.SetActive(false);
            _pool.Enqueue(ghost);

            if (ghost)
            {
                ghosts.Remove(ghost);
                Destroy(ghost);
            }
        }

        void OnDisable() { StopAllCoroutines(); }

        GameObject GetGhostFromPool() { while (_pool.Count > 0) { var g = _pool.Dequeue(); if (g) return g; } return CreateGhostInstance(); }

        public void DestroyAllGhosts()
        {
            StopAllCoroutines();
            foreach (var g in ghosts) { if (g) Destroy(g); }
            ghosts.Clear();
        }

        void OnDestroy() { DestroyAllGhosts(); }
    }
}
