using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugHitBoxIndicator : MonoBehaviour
{
    public enum Shape { Rect, Circle, Capsule }

    // 统一入口：XY平面，Z=0
    public static void Draw(Shape shape, Vector2 center, Vector2 size, Color color, float duration = 0.25f, int segments = 20, Vector2? dir = null)
    {
        switch (shape)
        {
            case Shape.Rect:
                // 画矩形（size=宽高）
                float angleDeg = 0f;
                if (dir != null)
                {
                    angleDeg = Mathf.Atan2(dir.Value.y, dir.Value.x) * Mathf.Rad2Deg;
                }
                DrawRect(center, size, color, duration, angleDeg);
                break;
            case Shape.Circle:
                DrawCircle(center, size.x, color, duration, segments);
                break;
            case Shape.Capsule:
                DrawCapsule(center, size.x, size.y, color, duration, segments);
                break;
        }
    }

    // 矩形线框：size=(w,h)
    static void DrawRect(Vector2 c, Vector2 size, Color col, float dur, float angleDeg = 0f)
    {
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;

        // 本地未旋转的四角（以中心为原点）
        Vector2[] local = new Vector2[]
        {
        new Vector2(-hx, -hy),
        new Vector2( hx, -hy),
        new Vector2( hx,  hy),
        new Vector2(-hx,  hy)
        };

        // 旋转（绕Z轴）
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        Vector3[] pts = new Vector3[5];
        for (int i = 0; i < 4; i++)
        {
            var p = local[i];
            // 应用旋转并平移到中心
            float rx = p.x * cos - p.y * sin;
            float ry = p.x * sin + p.y * cos;
            pts[i] = new Vector3(c.x + rx, c.y + ry, 0f);
        }
        pts[4] = pts[0]; // 闭合

        var go = NewLine(col);
        var lr = go.GetComponent<LineRenderer>();
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
        Object.Destroy(go, dur);
    }

    // 圆线框：radius=size.x
    static void DrawCircle(Vector2 c, float radius, Color col, float dur, int seg)
    {
        var go = NewLine(col);
        var lr = go.GetComponent<LineRenderer>();
        lr.positionCount = seg + 1;
        for (int i = 0; i <= seg; i++)
        {
            float t = (float)i / seg * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(c.x + Mathf.Cos(t) * radius, c.y + Mathf.Sin(t) * radius, 0));
        }
        Object.Destroy(go, dur);
    }

    // 胶囊：radius=size.x，高度=size.y（总长度），沿Y轴延伸
    static void DrawCapsule(Vector2 c, float radius, float height, Color col, float dur, int seg)
    {
        height = Mathf.Max(height, radius * 2f);
        float straight = height - radius * 2f;
        float half = straight * 0.5f;

        // 上下半圆
        var top = new Vector2(c.x, c.y + half);
        var bot = new Vector2(c.x, c.y - half);

        // 组合线：左侧竖线、右侧竖线、上半圆、下半圆
        // 左竖线
        DrawLine(new Vector3[] {
            new Vector3(c.x - radius, bot.y, 0),
            new Vector3(c.x - radius, top.y, 0)
        }, col, dur);

        // 右竖线
        DrawLine(new Vector3[] {
            new Vector3(c.x + radius, bot.y, 0),
            new Vector3(c.x + radius, top.y, 0)
        }, col, dur);

        // 上半圆
        DrawArc(top, radius, 0, Mathf.PI, col, dur, seg);
        // 下半圆
        DrawArc(bot, radius, Mathf.PI, Mathf.PI * 2f, col, dur, seg);
    }

    // 工具：创建LineRenderer（统一样式）
    static GameObject NewLine(Color col)
    {
        var go = new GameObject("[Debug2DLine]");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lr.startColor = col;
        lr.endColor = col;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;
        lr.alignment = LineAlignment.View;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingLayerName = "Normal";
        return go;
    }

    static void DrawLine(Vector3[] pts, Color col, float dur)
    {
        var go = NewLine(col);
        var lr = go.GetComponent<LineRenderer>();
        lr.positionCount = pts.Length;
        lr.SetPositions(pts);
        Object.Destroy(go, dur);
    }

    static void DrawArc(Vector2 center, float radius, float startRad, float endRad, Color col, float dur, int seg)
    {
        var go = NewLine(col);
        var lr = go.GetComponent<LineRenderer>();
        lr.positionCount = seg + 1;
        for (int i = 0; i <= seg; i++)
        {
            float t = Mathf.Lerp(startRad, endRad, (float)i / seg);
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(t) * radius, center.y + Mathf.Sin(t) * radius, 0));
        }
        Object.Destroy(go, dur);
    }
}
