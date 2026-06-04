#if UNITY_EDITOR
using My.Map.DualGrid;
using UnityEditor;
using UnityEngine;

namespace My.Map.DualGrid.Editor
{
    [CustomEditor(typeof(DualGridTilePalette))]
    public class DualGridTilePaletteEditor : UnityEditor.Editor
    {
        const float DotCell = 11f;
        const float DotGap = 2f;
        const float DotBlockWidth = 36f;
        const float LabelColumnWidth = 52f;

        static readonly Color DotOn = new Color(0.25f, 0.85f, 0.45f, 1f);
        static readonly Color DotOff = new Color(0.35f, 0.35f, 0.35f, 0.55f);
        static readonly Color DotView = new Color(1f, 0.75f, 0.2f, 1f);
        static readonly Color DiagramBg = new Color(0.12f, 0.12f, 0.12f, 1f);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("TerrainId"));
            EditorGUILayout.Space(4);

            DrawReferenceSection();

            var cornersProp = serializedObject.FindProperty("Corners");
            if (cornersProp == null || !cornersProp.isArray)
            {
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EnsureCornerArray(cornersProp);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Corner Slots (mask 0 → 15)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("竖排；绿点 = 该角有 Data 地形", EditorStyles.miniLabel);

            for (int mask = 0; mask < DualGridTilePalette.SlotCount; mask++)
            {
                DrawSlotRow(cornersProp, mask);
            }

            serializedObject.ApplyModifiedProperties();
        }

        static void EnsureCornerArray(SerializedProperty cornersProp)
        {
            while (cornersProp.arraySize < DualGridTilePalette.SlotCount)
            {
                cornersProp.InsertArrayElementAtIndex(cornersProp.arraySize);
            }
        }

        void DrawReferenceSection()
        {
            EditorGUILayout.LabelField("Mask 示意（View 角点 + 周围 Data）", EditorStyles.boldLabel);

            var boxRect = GUILayoutUtility.GetRect(0f, 118f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(boxRect, DiagramBg);
            }

            float pad = 8f;
            float left = boxRect.x + pad;
            float top = boxRect.y + pad;
            float diagramW = DotCell * 2f + DotGap;
            float diagramH = DotCell * 2f + DotGap;

            var refRect = new Rect(left, top + 10f, diagramW, diagramH);
            DrawMaskDots(refRect, 0x0F, drawViewMarker: true, drawBitLabels: true);

            float textX = refRect.xMax + 12f;
            float textY = top + 4f;
            var style = EditorStyles.miniLabel;
            GUI.Label(new Rect(textX, textY, boxRect.width - (textX - boxRect.x) - pad, 16f),
                "View 角点落在四格交点（黄 +）", style);
            GUI.Label(new Rect(textX, textY + 18f, 220f, 16f), "上排 1左 0右 · 下排 3左 2右（对应 Data 四格）", style);
            GUI.Label(new Rect(textX, textY + 36f, 280f, 16f), "槽位按 mask 0–15 自上而下排列", style);
            GUI.Label(new Rect(textX, textY + 54f, 280f, 16f), "每槽 Variants：多张图时按格子坐标确定性随机", style);

            float legendX = textX;
            float legendY = refRect.yMax + 6f;
            DrawLegendDot(new Rect(legendX, legendY, 10f, 10f), DotOn);
            GUI.Label(new Rect(legendX + 14f, legendY - 2f, 60f, 14f), "有地形", style);
            DrawLegendDot(new Rect(legendX + 70f, legendY, 10f, 10f), DotOff);
            GUI.Label(new Rect(legendX + 84f, legendY - 2f, 60f, 14f), "无", style);
            DrawLegendDot(new Rect(legendX + 120f, legendY, 10f, 10f), DotView);
            GUI.Label(new Rect(legendX + 134f, legendY - 2f, 40f, 14f), "View", style);
        }

        static void DrawLegendDot(Rect rect, Color color)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, color);
        }

        static void DrawSlotRow(SerializedProperty cornersProp, int mask)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            var diagramRect = GUILayoutUtility.GetRect(DotBlockWidth, 28f, GUILayout.Width(DotBlockWidth));
            DrawMaskDots(diagramRect, mask, drawViewMarker: false, drawBitLabels: false);

            EditorGUILayout.BeginVertical(GUILayout.Width(LabelColumnWidth));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"#{mask}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ToBinary(mask), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            var slotProp = cornersProp.GetArrayElementAtIndex(mask);
            var variantsProp = slotProp.FindPropertyRelative("Variants");
            EditorGUILayout.PropertyField(variantsProp, GUIContent.none, true);

            EditorGUILayout.EndHorizontal();
        }

        static void DrawMaskDots(Rect outer, int mask, bool drawViewMarker, bool drawBitLabels)
        {
            float w = DotCell * 2f + DotGap;
            float h = DotCell * 2f + DotGap;
            var grid = new Rect(
                outer.x + (outer.width - w) * 0.5f,
                outer.y + (outer.height - h) * 0.5f,
                w,
                h);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(grid, new Color(0.08f, 0.08f, 0.08f, 1f));
            }

            for (int bit = 0; bit < 4; bit++)
            {
                bool on = (mask & (1 << bit)) != 0;
                var cell = GetBitCellRect(grid, bit);
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(cell, on ? DotOn : DotOff);
                }

                if (drawBitLabels)
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 9,
                        normal = { textColor = Color.white }
                    };
                    GUI.Label(cell, bit.ToString(), labelStyle);
                }
            }

            if (drawViewMarker && Event.current.type == EventType.Repaint)
            {
                float cx = grid.x + grid.width * 0.5f;
                float cy = grid.y + grid.height * 0.5f;
                float s = 4f;
                EditorGUI.DrawRect(new Rect(cx - s, cy - 1f, s * 2f, 2f), DotView);
                EditorGUI.DrawRect(new Rect(cx - 1f, cy - s, 2f, s * 2f), DotView);
            }
        }

        // 与 DualGridCore 一致：bit0=(vx,vy) bit1=(vx-1,vy) bit2=(vx,vy-1) bit3=(vx-1,vy-1)
        // GUI 上排 = Tilemap y 较大，左列 = x 较小
        static Rect GetBitCellRect(Rect grid, int bit)
        {
            int col = bit == 0 || bit == 2 ? 1 : 0;
            int row = bit == 0 || bit == 1 ? 0 : 1;
            float x = grid.x + col * (DotCell + DotGap);
            float y = grid.y + row * (DotCell + DotGap);
            return new Rect(x, y, DotCell, DotCell);
        }

        static string ToBinary(int mask)
        {
            return System.Convert.ToString(mask, 2).PadLeft(4, '0');
        }
    }
}
#endif
