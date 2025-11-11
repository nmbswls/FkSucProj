using System;
using UnityEngine;

namespace My.UI
{
    [Serializable]
    public class PanelResource
    {
        public string panelId;              // Из "OverworldHUD"
        public string resourcePath;         // Из "UI/Prefabs/OverworldHUD"
        public UILayer defaultLayer = UILayer.HUD;
        public bool pooled = true;
        public int poolSize = 1;
    }

    class PanelPool
    {
        public readonly System.Collections.Generic.Queue<IPanel> pool = new();
        public Transform parent;
    }
}