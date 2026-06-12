#if UNITY_EDITOR
using System.Collections.Generic;
using My.Map.Scene;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UnitPresentationVisualVolume))]
public class UnitPresentationVisualVolumeEditor : UnityEditor.Editor
{
    SerializedProperty _mode;
    SerializedProperty _boundsInset;
    SerializedProperty _halfExtentScale;
    SerializedProperty _centerOffsetLocal;
    SerializedProperty _hullPointsLocal;

    float _previewFacingDeg;
    int _selectedHullIndex = -1;
    bool _showHullPoints;

    void OnEnable()
    {
        _mode = serializedObject.FindProperty("mode");
        _boundsInset = serializedObject.FindProperty("boundsInset");
        _halfExtentScale = serializedObject.FindProperty("halfExtentScale");
        _centerOffsetLocal = serializedObject.FindProperty("centerOffsetLocal");
        _hullPointsLocal = serializedObject.FindProperty("hullPointsLocal");

        var presenter = ((UnitPresentationVisualVolume)target).GetComponent<SceneUnitPresenter>();
        if (presenter?.UnitEntity != null)
        {
            var look = presenter.UnitEntity.CurrentLook;
            if (look.sqrMagnitude >= 1e-4f)
            {
                _previewFacingDeg = Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_mode);
        EditorGUILayout.PropertyField(_boundsInset);
        EditorGUILayout.PropertyField(_halfExtentScale);
        EditorGUILayout.PropertyField(_centerOffsetLocal);

        var volume = (UnitPresentationVisualVolume)target;
        var mode = (EVisualVolumeMode)_mode.enumValueIndex;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Facing", EditorStyles.boldLabel);
        _previewFacingDeg = EditorGUILayout.Slider("Facing Angle (deg)", _previewFacingDeg, -180f, 180f);

        if (mode == EVisualVolumeMode.ManualConvexHull)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Manual Convex Hull", EditorStyles.boldLabel);
            _showHullPoints = EditorGUILayout.Foldout(_showHullPoints, "Hull Points", true);
            if (_showHullPoints)
            {
                EditorGUILayout.PropertyField(_hullPointsLocal, true);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Convex"))
                {
                    if (volume.ValidateHull(out var message))
                    {
                        EditorUtility.DisplayDialog("Visual Volume", message, "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Visual Volume", message, "OK");
                    }
                }

                if (GUILayout.Button("Fix To Convex Hull"))
                {
                    Undo.RecordObject(volume, "Fix Visual Volume Hull");
                    volume.FixHullToConvex();
                    EditorUtility.SetDirty(volume);
                    serializedObject.Update();
                }
            }

            if (GUILayout.Button("Generate From Auto AABB"))
            {
                Undo.RecordObject(volume, "Generate Visual Volume Hull");
                if (volume.GenerateHullFromAutoAabb(_previewFacingDeg))
                {
                    EditorUtility.SetDirty(volume);
                    serializedObject.Update();
                }
                else
                {
                    EditorUtility.DisplayDialog("Visual Volume", "Failed to generate hull from sprite bounds.", "OK");
                }
            }

            if (!volume.ValidateHull(out var validateMsg))
            {
                EditorGUILayout.HelpBox(validateMsg, MessageType.Warning);
            }
        }
        else if (volume.TryComputeAutoAabb(_previewFacingDeg, out var previewVol))
        {
            EditorGUILayout.HelpBox(
                $"Auto AABB center={previewVol.Center}, half={previewVol.HalfExtents}",
                MessageType.None);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void OnSceneGUI()
    {
        var volume = (UnitPresentationVisualVolume)target;
        if (volume == null)
        {
            return;
        }

        serializedObject.Update();
        var mode = (EVisualVolumeMode)_mode.enumValueIndex;
        var origin = volume.transform.position;
        var rot = Quaternion.Euler(0f, 0f, _previewFacingDeg);

        if (mode == EVisualVolumeMode.AutoAabb)
        {
            if (!volume.TryComputeAutoAabb(_previewFacingDeg, out var vol))
            {
                return;
            }

            DrawVolumeGizmo(origin, rot, vol, new Color(0.3f, 0.85f, 1f, 0.9f));
            return;
        }

        DrawManualHullSceneGui(volume, origin, rot);
    }

    void DrawManualHullSceneGui(UnitPresentationVisualVolume volume, Vector3 origin, Quaternion rot)
    {
        var hull = GetHullPoints(volume);
        if (hull.Count == 0)
        {
            return;
        }

        Handles.color = new Color(0.3f, 0.85f, 1f, 0.9f);
        for (int i = 0; i < hull.Count; i++)
        {
            int next = (i + 1) % hull.Count;
            Handles.DrawLine(
                FacingLocalToWorld(origin, rot, hull[i]),
                FacingLocalToWorld(origin, rot, hull[next]));
        }

        for (int i = 0; i < hull.Count; i++)
        {
            Vector3 world = FacingLocalToWorld(origin, rot, hull[i]);
            float handleSize = HandleUtility.GetHandleSize(world) * 0.05f;
            if (Handles.Button(world, Quaternion.identity, handleSize, handleSize, Handles.DotHandleCap))
            {
                _selectedHullIndex = i;
                Repaint();
            }

            if (_selectedHullIndex == i)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 moved = Handles.FreeMoveHandle(
                    world,
                    handleSize * 1.5f,
                    Vector3.zero,
                    Handles.CircleHandleCap);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(volume, "Move Visual Volume Hull Point");
                    Vector2 local = WorldToFacingLocal(origin, rot, moved);
                    SetHullPoint(i, local);
                    EditorUtility.SetDirty(volume);
                    serializedObject.Update();
                }
            }
        }

        if (!volume.ValidateHull(out _))
        {
            Handles.color = Color.red;
            Handles.Label(origin + Vector3.up * 0.4f, "Invalid hull: use Fix To Convex Hull");
        }
    }

    static void DrawVolumeGizmo(Vector3 origin, Quaternion rot, in FacingLocalVolume vol, Color color)
    {
        Handles.color = color;

        if (vol.Mode == EVisualVolumeMode.ManualConvexHull && vol.Hull != null && vol.Hull.Length >= VisualVolumeConvexMath.MinHullPoints)
        {
            for (int i = 0; i < vol.Hull.Length; i++)
            {
                int next = (i + 1) % vol.Hull.Length;
                Handles.DrawLine(
                    FacingLocalToWorld(origin, rot, vol.Hull[i]),
                    FacingLocalToWorld(origin, rot, vol.Hull[next]));
            }

            return;
        }

        Vector2 c = vol.Center;
        Vector2 h = vol.HalfExtents;
        Vector3 bl = FacingLocalToWorld(origin, rot, c + new Vector2(-h.x, -h.y));
        Vector3 br = FacingLocalToWorld(origin, rot, c + new Vector2(h.x, -h.y));
        Vector3 tr = FacingLocalToWorld(origin, rot, c + new Vector2(h.x, h.y));
        Vector3 tl = FacingLocalToWorld(origin, rot, c + new Vector2(-h.x, h.y));
        Handles.DrawLine(bl, br);
        Handles.DrawLine(br, tr);
        Handles.DrawLine(tr, tl);
        Handles.DrawLine(tl, bl);
    }

    List<Vector2> GetHullPoints(UnitPresentationVisualVolume volume)
    {
        var list = new List<Vector2>();
        for (int i = 0; i < _hullPointsLocal.arraySize; i++)
        {
            var element = _hullPointsLocal.GetArrayElementAtIndex(i);
            list.Add(element.vector2Value);
        }

        return list;
    }

    void SetHullPoint(int index, Vector2 value)
    {
        if (index < 0 || index >= _hullPointsLocal.arraySize)
        {
            return;
        }

        _hullPointsLocal.GetArrayElementAtIndex(index).vector2Value = value;
        serializedObject.ApplyModifiedProperties();
    }

    static Vector3 FacingLocalToWorld(Vector3 origin, Quaternion facingRot, Vector2 local)
    {
        return origin + facingRot * new Vector3(local.x, local.y, 0f);
    }

    static Vector2 WorldToFacingLocal(Vector3 origin, Quaternion facingRot, Vector3 world)
    {
        Vector3 rel = Quaternion.Inverse(facingRot) * (world - origin);
        return new Vector2(rel.x, rel.y);
    }
}
#endif
