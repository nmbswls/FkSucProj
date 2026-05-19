using My.Config;
using My.SecretBase;
using My.Saving;
using UnityEngine;

namespace My
{
    public partial class GameLogicManager
    {
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

            TrySnapshotOpenWorldBeforeEnteringSecretBase(SecretBaseDefs.MapId);
            PreparePlayerSwitchArea(SecretBaseDefs.MapId, false, targetPoint);
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
            var cfg = AreaManager?.cacheMapCfg;
            if (cfg == null)
            {
                PlayerWorldLocation = EPlayerWorldLocation.OpenWorld;
                return;
            }

            if (cfg.IsSecretBase)
            {
                PlayerWorldLocation = EPlayerWorldLocation.SecretBase;
            }
            else if (cfg.IsHome)
            {
                PlayerWorldLocation = EPlayerWorldLocation.LegacyHome;
            }
            else
            {
                PlayerWorldLocation = EPlayerWorldLocation.OpenWorld;
            }
        }

        void OnSecretBasePostAreaLoaded()
        {
            RefreshPlayerWorldLocationFromMapCfg();
            if (IsInSecretBase)
            {
                SecretBase.OnWorldSceneLoaded();
            }
            else
            {
                SecretBase.Shutdown();
            }
        }

        void TrySnapshotOpenWorldBeforeEnteringSecretBase(string destinationMapName)
        {
            var destCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(destinationMapName);
            if (destCfg == null || !destCfg.IsSecretBase)
            {
                return;
            }

            string srcMap = AreaManager.MapName;
            if (string.IsNullOrEmpty(srcMap))
            {
                return;
            }

            var srcCfg = CfgMgr.Cfgs.TbMapAreaInfo.GetOrDefault(srcMap);
            if (srcCfg == null || srcCfg.IsSecretBase || srcCfg.IsHome)
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
    }
}
