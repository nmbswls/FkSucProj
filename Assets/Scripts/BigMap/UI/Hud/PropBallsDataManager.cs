using System.Collections.Generic;
using UnityEngine;

namespace My.UI
{
    // 每个属性球的数据定义，Inspector 中只需配置 AttrId 与 Root
    [System.Serializable]
    public class PropBallDef
    {
        // 对应 AttrIdConsts 中的属性 ID
        public string AttrId;
        // 球的根节点（CG 与 Bar 子节点会自动解析）
        public RectTransform Root;
    }

    // 挂载到 PropBalls 节点，允许在 Inspector 中直接管理属性球列表
    [RequireComponent(typeof(RectTransform))]
    public class PropBallsDataManager : MonoBehaviour
    {
        [SerializeField]
        public List<PropBallDef> Balls = new();

        public bool TryGetBall(string attrId, out PropBallDef ball)
        {
            foreach (var b in Balls)
            {
                if (b.AttrId == attrId)
                {
                    ball = b;
                    return true;
                }
            }
            ball = null;
            return false;
        }

        public IEnumerable<PropBallDef> AllBalls => Balls;
    }
}
