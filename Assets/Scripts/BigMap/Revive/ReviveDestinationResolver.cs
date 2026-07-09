using System;
using cfg.demo;
using My.Config;
using UnityEngine;

namespace My
{
    public enum EReviveReason
    {
        PlayerDeath,
        EncounterDefeat,
        BigMapFinish,
    }

    public struct ReviveDestination
    {
        public string MapOverlayId;
        public string TargetPoint;
        public Vector2? TargetPos;
        public bool ResetMap;
        public bool ForceHumanMode;
        public bool EnterSecretBaseContext;
    }

    public static class ReviveDestinationResolver
    {
        const string AnyReason = "Any";
        const string TargetModeCurrentMap = "CurrentMap";
        const string TargetModeFixedMap = "FixedMap";
        const string TargetModeSecretBase = "SecretBase";

        public static ReviveDestination Resolve(GameLogicManager glm, EReviveReason reason)
        {
            var bestRule = FindBestRule(glm, reason);
            return bestRule != null ? BuildFromRule(glm, bestRule) : BuildFallback(glm);
        }

        static ReviveRule FindBestRule(GameLogicManager glm, EReviveReason reason)
        {
            var rows = CfgMgr.Cfgs?.TbReviveRule?.DataList;
            if (rows == null || rows.Count == 0)
            {
                return null;
            }

            ReviveRule best = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null || !Matches(glm, row, reason))
                {
                    continue;
                }

                if (best == null || row.Priority > best.Priority)
                {
                    best = row;
                }
            }

            return best;
        }

        static bool Matches(GameLogicManager glm, ReviveRule rule, EReviveReason reason)
        {
            if (!string.IsNullOrWhiteSpace(rule.Reason)
                && !string.Equals(rule.Reason, AnyReason, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(rule.Reason, reason.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var currentOverlayId = glm?.AreaManager?.AreaOverlayId;
            if (!string.IsNullOrWhiteSpace(rule.SourceOverlayId)
                && !string.Equals(rule.SourceOverlayId, currentOverlayId, StringComparison.Ordinal))
            {
                return false;
            }

            var currentLogicAreaId = LogicAreaHomesteadUtil.ResolveCurrentLogicAreaId(glm?.AreaManager);
            if (!string.IsNullOrWhiteSpace(rule.SourceLogicAreaId)
                && !string.Equals(rule.SourceLogicAreaId, currentLogicAreaId, StringComparison.Ordinal))
            {
                return false;
            }

            return glm == null || glm.CheckCommonCondsAll(rule.MatchConds);
        }

        static ReviveDestination BuildFromRule(GameLogicManager glm, ReviveRule rule)
        {
            var mode = string.IsNullOrWhiteSpace(rule.TargetMode)
                ? TargetModeFixedMap
                : rule.TargetMode.Trim();

            var currentMap = glm?.AreaManager?.AreaOverlayId;
            var map = rule.TargetOverlayId;
            var enterSecretBase = false;

            if (string.Equals(mode, TargetModeCurrentMap, StringComparison.OrdinalIgnoreCase))
            {
                map = string.IsNullOrWhiteSpace(currentMap) ? "game_init" : currentMap;
            }
            else if (string.Equals(mode, TargetModeSecretBase, StringComparison.OrdinalIgnoreCase))
            {
                map = string.IsNullOrWhiteSpace(rule.TargetOverlayId)
                    ? GameLogicManager.SecretBaseMapId
                    : rule.TargetOverlayId;
                enterSecretBase = true;
            }
            else if (string.IsNullOrWhiteSpace(map))
            {
                map = string.IsNullOrWhiteSpace(currentMap) ? "game_init" : currentMap;
            }

            return new ReviveDestination
            {
                MapOverlayId = map,
                TargetPoint = string.IsNullOrWhiteSpace(rule.TargetNamedPoint) ? null : rule.TargetNamedPoint,
                TargetPos = null,
                ResetMap = rule.ResetMap,
                ForceHumanMode = rule.ForceHumanMode,
                EnterSecretBaseContext = enterSecretBase,
            };
        }

        static ReviveDestination BuildFallback(GameLogicManager glm)
        {
            var currentMap = glm?.AreaManager?.AreaOverlayId;
            return new ReviveDestination
            {
                MapOverlayId = string.IsNullOrWhiteSpace(currentMap) ? "game_init" : currentMap,
                TargetPoint = "BornPos",
                TargetPos = null,
                ResetMap = true,
                ForceHumanMode = true,
                EnterSecretBaseContext = false,
            };
        }
    }
}
