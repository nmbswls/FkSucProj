
using System.Threading.Tasks;
using Animancer;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{

    public class StartupMenuPanel : PanelWithInput, IInputConsumer
    {

        public Button BtnOne;
        public Button BtnTwo;
        public Button BtnThird;


        public AnimancerComponent StartupAnimancer;
        public AnimationClip StartupClip;

        private void Awake()
        {
            BtnOne.onClick.AddListener(BtnOneClick);
        }

        private void Start()
        {
            StartupAnimancer.Play(StartupClip);
        }


        private void BtnOneClick()
        {

            _ = EnterGameAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception.InnerException.StackTrace);
                }
                //isSwitchingEncounter = false;
            }, TaskScheduler.FromCurrentSynchronizationContext()); ;


        }

        private async Task EnterGameAsync()
        {
            await MainGameManager.Instance.InitStartGame("a", () =>
            {
                Debug.Log("InitializeGame finished");
            });
        }
    }


}