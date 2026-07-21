using System.Collections.Generic;
using Config.Map;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace My.EditorTools
{
    public static class EventGroupConfigValidator
    {
        [MenuItem("Tools/Validation/Validate Event Group Configs")]
        public static void ValidateFromMenu()
        {
            var errors = ValidateAll();
            if (errors.Count == 0)
            {
                Debug.Log("[EventGroupConfigValidator] All EventGroup configs are valid.");
                return;
            }

            foreach (var error in errors)
            {
                Debug.LogError(error);
            }
        }

        public static void ValidateAllOrThrow()
        {
            var errors = ValidateAll();
            if (errors.Count > 0)
            {
                throw new BuildFailedException(string.Join("\n", errors));
            }

            Debug.Log("[EventGroupConfigValidator] All EventGroup configs are valid.");
        }

        static List<string> ValidateAll()
        {
            var errors = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:MapEventGroupConfig"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<MapEventGroupConfig>(path);
                if (config == null || config.Stages == null || config.Stages.Count == 0)
                {
                    continue;
                }

                ValidateConfig(config, path, errors);
            }

            return errors;
        }

        static void ValidateConfig(MapEventGroupConfig config, string path, List<string> errors)
        {
            var members = new Dictionary<int, MapEventGroupConfig.MemberInfo>();
            foreach (var member in config.GroupMemberInfos)
            {
                if (member == null || member.MemberId <= 0 || member.InitInfo == null)
                {
                    errors.Add($"[EventGroup] {path}: invalid member definition.");
                    continue;
                }

                if (!members.TryAdd(member.MemberId, member))
                {
                    errors.Add($"[EventGroup] {path}: duplicate member id {member.MemberId}.");
                }
            }

            var stageIds = new HashSet<int>();
            var triggerIds = new HashSet<int>();
            foreach (var trigger in config.InnerTriggers)
            {
                if (trigger == null || !triggerIds.Add(trigger.TriggerId) || trigger.TriggerId <= 0)
                {
                    errors.Add($"[EventGroup] {path}: null, invalid or duplicate trigger id.");
                }
            }

            foreach (var stage in config.Stages)
            {
                if (stage == null || !stageIds.Add(stage.StageId))
                {
                    errors.Add($"[EventGroup] {path}: null or duplicate stage id.");
                    continue;
                }

                foreach (var memberId in stage.EnsureMemberIds)
                {
                    if (!members.ContainsKey(memberId))
                    {
                        errors.Add($"[EventGroup] {path}: stage {stage.StageId} references unknown member {memberId}.");
                    }
                }

                var activeTriggerIds = new HashSet<int>();
                foreach (var triggerId in stage.ActiveTriggerIds)
                {
                    if (!activeTriggerIds.Add(triggerId))
                    {
                        errors.Add($"[EventGroup] {path}: stage {stage.StageId} repeats trigger {triggerId}.");
                    }
                    if (!triggerIds.Contains(triggerId))
                    {
                        errors.Add($"[EventGroup] {path}: stage {stage.StageId} references unknown trigger {triggerId}.");
                    }
                }

                foreach (var condition in stage.CompletionConditions)
                {
                    if (!ConditionHasMembers(condition, members))
                    {
                        errors.Add($"[EventGroup] {path}: stage {stage.StageId} has a condition with no members.");
                    }
                }

                if (stage.CompleteInteractId > 0 && !HasInteract(config, stage.StageId, stage.CompleteInteractId))
                {
                    errors.Add(
                        $"[EventGroup] {path}: stage {stage.StageId} missing completion interact {stage.CompleteInteractId}.");
                }

                if (!stage.CompleteEvent
                    && stage.CompletionConditions.Count > 0
                    && stage.NextStageId < 0)
                {
                    errors.Add($"[EventGroup] {path}: stage {stage.StageId} completes without a next stage or terminal state.");
                }
            }

            foreach (var stage in config.Stages)
            {
                if (stage.NextStageId >= 0 && !stageIds.Contains(stage.NextStageId))
                {
                    errors.Add($"[EventGroup] {path}: stage {stage.StageId} targets unknown stage {stage.NextStageId}.");
                }
            }

            foreach (var trigger in config.InnerTriggers)
            {
                if (trigger == null || trigger.TriggerId <= 0)
                {
                    continue;
                }

                if (trigger.ActionInteractId <= 0)
                {
                    errors.Add($"[EventGroup] {path}: stage trigger {trigger.TriggerId} has no action interact.");
                }
                else if (!HasAnyInteract(config, trigger.ActionInteractId))
                {
                    errors.Add(
                        $"[EventGroup] {path}: stage trigger {trigger.TriggerId} references missing interact {trigger.ActionInteractId}.");
                }

                if (trigger.FirePolicy == MapEventGroupConfig.GroupInnerTrigger.EFirePolicy.Cooldown
                    && trigger.MinTriggerInterval <= 0)
                {
                    errors.Add($"[EventGroup] {path}: cooldown trigger {trigger.TriggerId} needs a positive interval.");
                }

                if (trigger.TriggerType == MapEventGroupConfig.GroupInnerTrigger.ETriggerType.MemberStatusChanged
                    || trigger.TriggerType == MapEventGroupConfig.GroupInnerTrigger.ETriggerType.MemberDefeated
                    || trigger.TriggerType == MapEventGroupConfig.GroupInnerTrigger.ETriggerType.AnyEnmity)
                {
                    if (!TriggerHasMembers(trigger, members))
                    {
                        errors.Add($"[EventGroup] {path}: member trigger {trigger.TriggerId} has no valid members.");
                    }
                }
            }

            ValidateOutcomeOutputs(config, path, errors);
        }

        static bool ConditionHasMembers(
            MapEventGroupConfig.StageCondition condition,
            Dictionary<int, MapEventGroupConfig.MemberInfo> members)
        {
            if (condition == null || condition.ConditionType == MapEventGroupConfig.EStageConditionType.None)
            {
                return false;
            }

            if (condition.MemberIds != null && condition.MemberIds.Count > 0)
            {
                foreach (var memberId in condition.MemberIds)
                {
                    if (!members.ContainsKey(memberId))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (string.IsNullOrEmpty(condition.MemberTag))
            {
                return false;
            }

            foreach (var member in members.Values)
            {
                if (member.Tags != null && member.Tags.Contains(condition.MemberTag))
                {
                    return true;
                }
            }

            return false;
        }

        static bool HasInteract(MapEventGroupConfig config, int stageId, int interactId)
        {
            var status = stageId == 0
                ? config.MainStatusInfo
                : config.ExtraStatusInfos.Find(item => item.StatusId == stageId);
            return status?.InteractInfos?.Exists(item => item.InteractId == interactId) == true;
        }

        static bool HasAnyInteract(MapEventGroupConfig config, int interactId)
        {
            if (config.MainStatusInfo?.InteractInfos?.Exists(item => item.InteractId == interactId) == true)
            {
                return true;
            }

            foreach (var status in config.ExtraStatusInfos)
            {
                if (status?.InteractInfos?.Exists(item => item.InteractId == interactId) == true)
                {
                    return true;
                }
            }

            return false;
        }

        static void ValidateOutcomeOutputs(MapEventGroupConfig config, string path, List<string> errors)
        {
            var statuses = new List<MapInteractPointConfig.StatusInfo>();
            if (config.MainStatusInfo != null)
            {
                statuses.Add(config.MainStatusInfo);
            }
            if (config.ExtraStatusInfos != null)
            {
                statuses.AddRange(config.ExtraStatusInfos);
            }

            foreach (var status in statuses)
            {
                if (status.InteractInfos == null)
                {
                    continue;
                }

                foreach (var interact in status.InteractInfos)
                {
                    foreach (var output in interact.Outputs ?? new List<My.Config.LogicInteractOutput>())
                    {
                        if (output.OutputType == My.Config.LogicInteractOutput.EOutputType.ResolveEventGroupOutcome
                            && string.IsNullOrEmpty(output.Param3))
                        {
                            errors.Add(
                                $"[EventGroup] {path}: interact {interact.InteractId} outcome output needs Param3 outcome kind.");
                        }
                    }
                }
            }
        }

        static bool TriggerHasMembers(
            MapEventGroupConfig.GroupInnerTrigger trigger,
            Dictionary<int, MapEventGroupConfig.MemberInfo> members)
        {
            if (trigger.MemberIds != null && trigger.MemberIds.Count > 0)
            {
                foreach (var memberId in trigger.MemberIds)
                {
                    if (!members.ContainsKey(memberId))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (string.IsNullOrEmpty(trigger.MemberTag))
            {
                return false;
            }

            foreach (var member in members.Values)
            {
                if (member.Tags != null && member.Tags.Contains(trigger.MemberTag))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
