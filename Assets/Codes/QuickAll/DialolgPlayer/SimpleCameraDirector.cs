using System;
using UnityEngine;

public class SimpleCameraDirector : MonoBehaviour
{
    public Camera cam;

    private void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    public void MoveTo(Vector3 worldPos, float dur, DialogueTimeDriver driver, Action onComplete)
    {
        if (!cam) { onComplete?.Invoke(); return; }
        Transform tr = cam.transform;
        Vector3 start = tr.position;
        driver.Run(dur, p => {
            tr.position = Vector3.Lerp(start, worldPos, p);
        }, onComplete);
    }

    public void ZoomTo(float fov, float dur, DialogueTimeDriver driver, Action onComplete)
    {
        if (!cam) { onComplete?.Invoke(); return; }
        float start = cam.fieldOfView;
        driver.Run(dur, p => {
            cam.fieldOfView = Mathf.Lerp(start, fov, p);
        }, onComplete);
    }

    public void Shake(float amp, float dur, DialogueTimeDriver driver, Action onComplete)
    {
        if (!cam) { onComplete?.Invoke(); return; }
        Transform tr = cam.transform;
        Vector3 origin = tr.localPosition;
        driver.Run(dur, p => {
            Vector2 rnd = UnityEngine.Random.insideUnitCircle * amp * 0.1f;
            tr.localPosition = origin + new Vector3(rnd.x, rnd.y, 0);
        }, () => {
            tr.localPosition = origin;
            onComplete?.Invoke();
        });
    }
}