using Map.Logic;
using My.Map;
using UnityEngine;

public class FlyToPlayerMover : MonoBehaviour
{
    public float speed = 8f;          // 飞行速度（米/秒）
    public float arriveDistance = 0.2f; // 距离小于此值认为到达

    private Transform _target;
    private Vector3 _fixedPosition;
    private System.Action _onArrived;

    public void Init(Transform target, System.Action onArrived = null)
    {
        _target = target;
        _onArrived = onArrived;
    }

    void Update()
    {
        if (_target == null) return;

        Vector3 dir = (_target.position - transform.position);
        float dist = dir.magnitude;
        if (dist <= arriveDistance)
        {
            _onArrived?.Invoke();
            enabled = false; // 停止移动（上层可能会销毁）
            return;
        }

        Vector3 step = dir.normalized * speed * LogicTime.deltaTime;
        // 简单匀速追踪；需要更丝滑可用插值或加速曲线
        transform.position += step;
    }
}