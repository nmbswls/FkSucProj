using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace My.Map.View
{
    public class PlayerRumorTextSpawner : MonoBehaviour
    {
        [Tooltip("文本生成的初始位置相对玩家头顶的偏移")]
        public Vector3 spawnOffset = new Vector3(0f, 0.1f, 0f);

        [Header("Spawn Settings")]
        [Tooltip("每秒生成的文本数量")]
        public float spawnRate = 1.0f;
        [Tooltip("同时存在的最大文本数量")]
        public int maxActiveTexts = 12;
        [Tooltip("随机水平散布范围")]
        public float horizontalSpread = 1.2f;
        [Tooltip("随机垂直初始偏移范围")]
        public float verticalJitter = 0.5f;
        [Tooltip("文本从词库中随机挑选")]
        public List<string> rumorLines = new List<string>()
        {
            "what？",
            "why！",
            "whhhh",
        };

        [Header("Prefab & Pool")]
        public GameObject textBubblePrefab;
        [Tooltip("是否使用对象池提高性能")]
        public bool usePool = true;
        public int poolSize = 24;


        public bool IsActive = false;

        private float _timer;
        private readonly List<GameObject> _active = new List<GameObject>();
        private TextBubblePool _pool;

        private void Awake()
        {
            if (usePool && textBubblePrefab != null)
            {
                _pool = new TextBubblePool(textBubblePrefab, poolSize, this.transform);
            }

            if(textBubblePrefab)
            {
                textBubblePrefab.SetActive(false);
            }
        }

        private void Update()
        {
            var playerPresenter = MainGameManager.Instance.playerScenePresenter;

            if (playerPresenter == null || textBubblePrefab == null || rumorLines.Count == 0) return;

            _timer += Time.deltaTime;
            float interval = 1f / Mathf.Max(0.0001f, spawnRate);

            // 按固定频率生成
            while (_timer >= interval)
            {
                _timer -= interval;
                TrySpawn();
            }

            // 清理已被回收/销毁的引用
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i] == null || !_active[i].activeSelf)
                    _active.RemoveAt(i);
            }
        }

        private void TrySpawn()
        {
            if (!IsActive) return;

            if (_active.Count >= maxActiveTexts) return;

            var playerPresenter = MainGameManager.Instance.playerScenePresenter;
            Vector3 basePos = playerPresenter.transform.position + spawnOffset;
            Vector3 randomOffset = new Vector3(
                Random.Range(-horizontalSpread, horizontalSpread),
                Random.Range(-verticalJitter, verticalJitter),
                0f
            );
            Vector3 spawnPos = basePos + randomOffset;

            GameObject go = usePool ? _pool.Get() : Instantiate(textBubblePrefab, spawnPos, Quaternion.identity, playerPresenter.transform);
            go.transform.SetParent(playerPresenter.transform);
            go.transform.position = spawnPos;
            textBubblePrefab.SetActive(true);

            TMP_Text tmp = go.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = rumorLines[Random.Range(0, rumorLines.Count)];
            }

            FloatingRumorText anim = go.GetComponent<FloatingRumorText>();
            if (anim != null)
            {
                anim.Play(
                    lifetime: Random.Range(1.2f, 2.4f),
                    floatSpeed: Random.Range(0.6f, 1.2f),
                    fadeInTime: 0.1f,
                    fadeOutTime: 0.4f,
                    swayAmplitude: Random.Range(0.05f, 0.12f),
                    swayFrequency: Random.Range(1.2f, 2.2f),
                    initialScale: Vector3.one * Random.Range(0.9f, 1.1f),
                    lookAtCamera: true
                );
                anim.onFinished = () =>
                {
                    if (usePool) _pool.Release(go);
                    else Destroy(go);

                    _active.Remove(go);
                };
            }

            _active.Add(go);
        }
    }

}

