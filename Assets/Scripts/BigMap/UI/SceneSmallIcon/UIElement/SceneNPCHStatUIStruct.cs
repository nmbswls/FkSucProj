

using My.Map;
using My.Map.Entity;
using My.Map.Scene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{


    public class SceneNPCHStatUIStruct : MonoBehaviour
    {
        public GameObject Go;
        //public TextMeshProUGUI Val;
        public SceneNpcPresenter bindingNpc;

        public Image FaQingFireHint;
        public Image SJProgressBar;
        public TextMeshProUGUI NpcWillText;
        public TextMeshProUGUI SJProgressText;

        public void Bind(SceneNpcPresenter npcPresenter)
        {
            this.bindingNpc = npcPresenter;
            gameObject.SetActive(true);
        }


        public void Unbind()
        {
            this.bindingNpc = null;
            gameObject.SetActive(false);
        }


        public void UpdateView()
        {
            if (bindingNpc == null) return;
            var sjProgress = bindingNpc.NpcEntity.GetAttr(AttrIdConsts.NPCSJProgress);
            SJProgressBar.fillAmount = sjProgress * 1.0f / 100_000;

            var hVal = bindingNpc.NpcEntity.GetAttr(AttrIdConsts.NPCHVal);
            if(hVal < 20_000)
            {
                FaQingFireHint.color = Color.white;
                FaQingFireHint.transform.localScale = Vector3.one * 0.6f;
            }
            else if(hVal < 40_000)
            {
                FaQingFireHint.color = Color.white;
                FaQingFireHint.transform.localScale = Vector3.one * 0.7f;
            }
            else if (hVal < 60_000)
            {
                FaQingFireHint.color = Color.red;
                FaQingFireHint.transform.localScale = Vector3.one * 0.8f;
            }
            else if (hVal < 80_000)
            {
                FaQingFireHint.color = Color.red;
                FaQingFireHint.transform.localScale = Vector3.one * 0.9f;
            }
            else
            {
                FaQingFireHint.color = Color.red;
                FaQingFireHint.transform.localScale = Vector3.one * 1.1f;
            }

            var hShield = bindingNpc.NpcEntity.GetAttr(AttrIdConsts.UnitHShield);
            if(hShield > 0)
            {
                NpcWillText.text = ((int)(Mathf.Ceil(hShield * 1.0f / 1000))).ToString();
            }
            else
            {
                NpcWillText.text = string.Empty;
            }
        }
    }

}