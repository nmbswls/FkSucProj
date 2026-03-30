

using UnityEngine.Playables;
using UnityEngine;

namespace My
{

    [RequireComponent(typeof(PlayableDirector))]
    public class TimelineDialogueBridge : MonoBehaviour
    {
        private PlayableDirector director;

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
        }

        /// <summary>
        /// 在 Timeline 的 Signal Receiver 中绑定这个方法，并填入自定义的字符串参数
        /// </summary>
        public void TriggerDialogueSignal(string signalName)
        {
            // 找到你场景中的 DialoguePlayer 实例
            var player = FindObjectOfType<DialoguePlayer>();
            if (player != null)
            {
                // 确保 DialoguePlayer 知道当前的 Director 是谁
                player.SetActiveDirector(director);

                // 发送信号并触发暂停
                player.ReceiveTimelineSignal(signalName);
            }
            else
            {
                Debug.LogWarning("未找到 DialoguePlayer，无法触发 Timeline 信号。");
            }
        }
    }

}