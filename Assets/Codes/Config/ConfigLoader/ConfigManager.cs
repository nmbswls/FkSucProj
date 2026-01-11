using SimpleJSON;
using UnityEngine;

namespace My.Config
{

    public static class CfgMgr
    {

        private static cfg.Tables _gameConfigs = null;

        public static cfg.Tables Cfgs { get { return _gameConfigs; } }
        public static void LoadGameConfigs()
        {
            string gameConfDir = "Config/Json"; // 替换为gen.bat中outputDataDir指向的目录

            _gameConfigs = new cfg.Tables((file) => {
                var configAsset = Resources.Load<TextAsset>($"{gameConfDir}/{file}");
                return JSON.Parse(configAsset.text);
            });
        }

    }
}