using System.Collections;
using My.Config;
using My.SecretBase;
using My.Saving;
using My.UI;
using UnityEngine;

namespace My
{
    public partial class GameLogicManager
    {
        public const string SecretBaseMapId = "secret_base_hub";

        public enum EPlayerWorldLocation
        {
            OpenWorld,
            LegacyHome,
            SecretBase,
        }

        public SecretBaseSession SecretBase { get; } = new SecretBaseSession();

        public EPlayerWorldLocation PlayerWorldLocation { get; private set; } = EPlayerWorldLocation.OpenWorld;

        public OpenWorldReturnBookmark LastOpenWorldBeforeSecretBase { get; private set; }

        public bool IsInSecretBase => PlayerWorldLocation == EPlayerWorldLocation.SecretBase;

        public void EnterSecretBase(string targetPoint = null)
        {
            if (IsInSecretBase || SwitchAreaIntent != null)
            {
                return;
            }

            PreparePlayerSwitchArea(SecretBaseMapId, false, targetPoint);
        }

        public void ExitSecretBase()
        {
            if (!IsInSecretBase || SwitchAreaIntent != null)
            {
                return;
            }

            var bm = LastOpenWorldBeforeSecretBase;
            if (bm == null || string.IsNullOrEmpty(bm.MapId))
            {
                Debug.LogWarning("ExitSecretBase: no return bookmark.");
                return;
            }

            SecretBase.Shutdown();
            PreparePlayerSwitchArea(bm.MapId, false, targetPos: bm.Pos);
        }

        void BindSecretBaseOnInit()
        {
            SecretBase.Bind(this);
        }

        void RefreshPlayerWorldLocationFromMapCfg()
        {
            var cfg = AreaManager?.cacheMapOverlayCfg;
            if (cfg == null)
            {
                PlayerWorldLocation = EPlayerWorldLocation.OpenWorld;
                return;
            }

            if (cfg.IsSecretBase)
            {
                PlayerWorldLocation = EPlayerWorldLocation.SecretBase;
            }
            // else if (cfg.IsHome)
            // {
            //     PlayerWorldLocation = EPlayerWorldLocation.LegacyHome;
            // }
            else
            {
                PlayerWorldLocation = EPlayerWorldLocation.OpenWorld;
            }
        }

        void OnSecretBasePostAreaLoaded()
        {
            RefreshPlayerWorldLocationFromMapCfg();
            if (!IsInSecretBase)
            {
                SecretBase.Shutdown();
            }
        }

        public void NotifySecretBasePresentationReady()
        {
            RefreshPlayerWorldLocationFromMapCfg();
            if (IsInSecretBase)
            {
                SecretBase.OnWorldSceneLoaded();
            }
        }

        void TrySnapshotOpenWorldBeforeEnteringSecretBase(string destinationMapName)
        {
            var destCfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(destinationMapName);
            if (destCfg == null || !destCfg.IsSecretBase)
            {
                return;
            }

            string srcMap = AreaManager.AreaOverlayId;
            if (string.IsNullOrEmpty(srcMap))
            {
                return;
            }

            var srcCfg = CfgMgr.Cfgs.TbAreaOverlayStateInfo.GetOrDefault(srcMap);
            if (srcCfg == null || srcCfg.IsSecretBase)
            {
                return;
            }

            if (playerLogicEntity == null)
            {
                return;
            }

            LastOpenWorldBeforeSecretBase = new OpenWorldReturnBookmark
            {
                MapId = srcMap,
                Pos = playerLogicEntity.Pos,
            };
        }

        void CaptureSecretBaseBookmarkFromSave(SaveData saveData)
        {
            if (saveData?.LastOpenWorldBeforeSecretBase != null)
            {
                LastOpenWorldBeforeSecretBase = new OpenWorldReturnBookmark
                {
                    MapId = saveData.LastOpenWorldBeforeSecretBase.MapId,
                    Pos = saveData.LastOpenWorldBeforeSecretBase.Pos,
                };
            }
            else
            {
                LastOpenWorldBeforeSecretBase = null;
            }
        }

        void AppendSecretBaseBookmarkToSave(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.LastOpenWorldBeforeSecretBase = LastOpenWorldBeforeSecretBase == null
                ? null
                : new OpenWorldReturnBookmark
                {
                    MapId = LastOpenWorldBeforeSecretBase.MapId,
                    Pos = LastOpenWorldBeforeSecretBase.Pos,
                };
        }

        void TickSecretBaseOnly(float dt)
        {
            playerDataManager?.Tick(dt);
            SecretBase.Tick(dt);
        }

        public int GetSecretBaseBuildLevel()
        {
            return worldPersistState?.GetSecretBaseBuildLevel() ?? 1;
        }

        public void SetSecretBaseBuildLevel(int level)
        {
            worldPersistState?.SetSecretBaseBuildLevel(level);
            SecretBaseSceneRoot.FindLoaded()?.RefreshScrollBounds();
        }
    }

    // 据点运行时：切图后绑定场景根，驱动卷轴与点击。
    public class SecretBaseSession
    {
        GameLogicManager _logic;
        bool _active;
        Coroutine _waitRootCo;

        public void Bind(GameLogicManager logic)
        {
            _logic = logic;
        }

        public void OnWorldSceneLoaded()
        {
            if (_active)
            {
                return;
            }

            var host = MainGameManager.Instance;
            if (host == null)
            {
                TryStart();
                return;
            }

            if (_waitRootCo != null)
            {
                host.StopCoroutine(_waitRootCo);
            }

            _waitRootCo = host.StartCoroutine(CoWaitRoot());
        }

        IEnumerator CoWaitRoot()
        {
            SecretBaseSceneRoot root = null;
            for (int i = 0; i < 30; i++)
            {
                root = SecretBaseSceneRoot.FindLoaded();
                if (root != null)
                {
                    break;
                }

                yield return null;
            }

            _waitRootCo = null;
            TryStart(root);
        }

        void TryStart(SecretBaseSceneRoot root = null)
        {
            if (_active)
            {
                return;
            }

            root ??= SecretBaseSceneRoot.FindLoaded();
            if (root == null)
            {
                Debug.LogError("SecretBaseSession: SecretBaseSceneRoot missing.");
                return;
            }

            _active = true;
            root.EnterMode();
            SecretBaseHudPanel.TryShow();
        }

        public void Shutdown()
        {
            if (!_active)
            {
                return;
            }

            _active = false;

            if (_waitRootCo != null && MainGameManager.Instance != null)
            {
                MainGameManager.Instance.StopCoroutine(_waitRootCo);
                _waitRootCo = null;
            }

            SecretBaseSceneRoot.FindLoaded()?.ExitMode();
            SecretBaseHudPanel.TryHide();
        }

        public void Tick(float dt)
        {
            if (!_active)
            {
                return;
            }

            SecretBaseSceneRoot.FindLoaded()?.Tick(dt);
        }

        public void OnScreenPointer(Vector2 screenPos, bool click)
        {
            SecretBaseSceneRoot.FindLoaded()?.HandleScreenPointer(screenPos, click);
        }
    }
}
