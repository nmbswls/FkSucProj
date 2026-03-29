

using My.Map;
using My.Map.Scene;
using UnityEngine;

namespace My.UI
{
    public class SceneEvilAlertUIItem : MonoBehaviour
    {
        public SceneNpcPresenter BindingNpc { get; private set; }

        public void Bind(SceneNpcPresenter npc)
        {
            BindingNpc = npc;
            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            BindingNpc = null;
            gameObject.SetActive(false);
        }
    }
}
