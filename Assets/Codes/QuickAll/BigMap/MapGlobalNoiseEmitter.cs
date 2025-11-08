using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGlobalNoiseEmitter : MonoBehaviour
{
    [Header("引用")]
    public MapNoiseRing noiseRingPrefab;

    [Header("参数")]
    public float minNoise = 0.1f;
    public float maxNoise = 1f;

    // 示例：按键触发噪音
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EmitNoiseFixed(Random.Range(minNoise, maxNoise), Vector2.zero);
        }
    }

    public void EmitNoiseFixed(float noise01, Vector2 worldPos)
    {
        if (noiseRingPrefab == null) return;
        var ring = Instantiate(noiseRingPrefab, transform);
        ring.transform.position = worldPos;
        ring.gameObject.SetActive(true);
        ring.Play(Mathf.Clamp01(noise01), worldPos);
    }
}
