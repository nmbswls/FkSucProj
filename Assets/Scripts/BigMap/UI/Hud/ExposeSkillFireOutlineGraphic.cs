using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    // 不依赖 Sprite，仅绘制矩形网格供火光描边 shader 采样
    [RequireComponent(typeof(CanvasRenderer))]
    public class ExposeSkillFireOutlineGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            maskable = false;
            color = Color.white;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetMaterialDirty();
        }

        public void MarkMaterialDirty()
        {
            SetMaterialDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            var color32 = (Color32)color;

            vh.AddVert(new Vector3(rect.xMin, rect.yMin), color32, new Vector2(0f, 0f));
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), color32, new Vector2(0f, 1f));
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), color32, new Vector2(1f, 1f));
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), color32, new Vector2(1f, 0f));

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
