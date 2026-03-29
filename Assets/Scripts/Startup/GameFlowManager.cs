using System.Collections;
using System.Collections.Generic;
using My.Config;
using My.Dialog;
using My.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace My
{
    public class GameFlowManager : MonoBehaviour
    {
        public enum EGameFlowState
        {
            Init,
            Menu,
            Main,
        }

        public EGameFlowState GameFlowStae = EGameFlowState.Init;

        public static GameFlowManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            CfgMgr.LoadGameConfigs();

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            var dialogData = JsonConvert.DeserializeObject<string>("", settings);

            EnterStateMenu();
        }

        /// <summary>
        /// 
        /// </summary>
        public void EnterStateMenu()
        {
            UIManager.Instance.ShowPanel("StartupMenuPanel");

            GameFlowStae = EGameFlowState.Menu;
        }
    }

}


