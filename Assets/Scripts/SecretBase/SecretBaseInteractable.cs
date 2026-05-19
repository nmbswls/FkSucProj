using My.UI;
using UnityEngine;

namespace My.SecretBase
{
    public class SecretBaseInteractable : MonoBehaviour
    {
        [SerializeField] private string panelId;
        [SerializeField] private Rect worldBounds = new Rect(-1f, -1f, 2f, 2f);

        public bool ContainsWorldPoint(Vector2 worldPos)
        {
            var c = (Vector2)transform.position + worldBounds.position;
            var r = new Rect(c, worldBounds.size);
            return r.Contains(worldPos);
        }

        public void TryOpenPanel()
        {
            if (string.IsNullOrEmpty(panelId))
            {
                return;
            }

            UIManager.Instance.ShowPanel(panelId);
        }
    }
}
