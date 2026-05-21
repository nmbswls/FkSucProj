using UnityEngine;

namespace My.SecretBase
{
    public interface ISecretBaseClickTarget
    {
        int SortOrder { get; }
        bool ContainsPoint(Vector2 worldPos);
        void SetHighlight(bool on);
        void OnClick();
    }
}
