using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;
    public float smoothTime = 0.2f;
    public Vector3 offset = new Vector3(0f, 0f, -15f); // 正交相机通常Z=-10
    private Vector3 velocity;

    void LateUpdate()
    {
        if (!Target) return;
        Vector3 desired = Target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}