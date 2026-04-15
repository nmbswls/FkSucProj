
using System.Threading.Tasks;
using Animancer;
using My;
using My.Saving;
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
            BtnOne.onClick.AddListener(BtnNewGameClick);
            if (BtnTwo != null)
            {
                BtnTwo.onClick.AddListener(BtnLoadGameClick);
            }

            if (BtnThird != null)
            {
                BtnThird.onClick.AddListener(BtnBundledTestSaveClick);
            }
        }

        private void OnEnable()
        {
            RefreshLoadButtonState();
        }

        private void Start()
        {
            StartupAnimancer.Play(StartupClip);
        }

        private void RefreshLoadButtonState()
        {
            if (BtnTwo == null) return;
            BtnTwo.interactable = SaveSystem.SaveFileLooksValid(SaveSystem.DefaultSaveFileName);
        }

        private void BtnNewGameClick()
        {
            _ = EnterGameAsync(GameStartSaveSource.NewGame).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception?.GetBaseException());
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void BtnLoadGameClick()
        {
            _ = EnterGameAsync(GameStartSaveSource.UserPersistentFile).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception?.GetBaseException());
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void BtnBundledTestSaveClick()
        {
            _ = EnterGameAsync(GameStartSaveSource.BundledTestSave).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError("exception " + t.Exception?.GetBaseException());
                }
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private async Task EnterGameAsync(GameStartSaveSource saveSource)
        {
            bool ok = await MainGameManager.Instance.InitStartGame("a", () =>
            {
                Debug.Log("InitializeGame finished");
            }, saveSource);

            if (!ok && saveSource == GameStartSaveSource.UserPersistentFile)
            {
                Debug.LogWarning("[StartupMenuPanel] No valid user save to load.");
                RefreshLoadButtonState();
            }
        }
    }


}
