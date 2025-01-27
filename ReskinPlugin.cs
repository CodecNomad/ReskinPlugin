using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ReskinPlugin", "CodecNomad", "1.0.0")]
    public class ReskinPlugin : RustPlugin
    {
        #region Configuration
        private const float MaxInteractionRange = 5f;
        private static readonly TimeSpan ReskinCooldown = TimeSpan.FromSeconds(3);
        #endregion

        #region Data Structures
        private readonly Dictionary<ulong, DateTime> _playerCooldowns = new();

        private static readonly Dictionary<BuildingGrade.Enum, ulong> BuildingSkins = new()
        {
            { BuildingGrade.Enum.Stone, 10225 },
            { BuildingGrade.Enum.Metal, 10221 }
        };
        #endregion

        #region Initialization
        private void Init()
        {
            _playerCooldowns.Clear();
        }
        #endregion

        #region Command Handling
        [ChatCommand("reskin")]
        private void HandleReskinCommand(BasePlayer player, string command, string[] args)
        {
            if (!CanPlayerReskin(player))
            {
                var remainingCooldown = GetRemainingCooldown(player);
                SendReply(player, $"Please wait {remainingCooldown.TotalSeconds:F1} seconds before skinning again.");
                return;
            }

            var targetBlock = GetTargetBuildingBlock(player);
            if (targetBlock == null)
            {
                SendReply(player, "No valid building block found in range.");
                return;
            }

            if (!HasReskinPermission(player, targetBlock))
            {
                SendReply(player, "You don't have permission to reskin this building block.");
                return;
            }

            ApplyReskin(targetBlock);
            UpdatePlayerCooldown(player);
        }
        #endregion

        #region Building Block Operations
        private static BuildingBlock GetTargetBuildingBlock(BasePlayer player)
        {
            var eyePosition = player.eyes.position;
            var lookDirection = player.eyes.HeadForward();

            return Physics.Raycast(eyePosition, lookDirection, out var hit, MaxInteractionRange) 
                ? hit.GetEntity() as BuildingBlock 
                : null;
        }

        private static void ApplyReskin(BuildingBlock block)
        {
            var newSkinId = DetermineSkinId(block);

            block.skinID = newSkinId;
            block.UpdateSkin();
            block.SendNetworkUpdate();
        }

        private static ulong DetermineSkinId(BuildingBlock block)
        {
            return block.skinID == 0 && BuildingSkins.TryGetValue(block.grade, out var skinId) 
                ? skinId 
                : 0;
        }
        #endregion

        #region Permission Checks
        private static bool HasReskinPermission(BasePlayer player, BuildingBlock block)
        {
            return block.OwnerID == player.userID;
        }
        #endregion

        #region Cooldown Management
        private bool CanPlayerReskin(BasePlayer player)
        {
            return GetRemainingCooldown(player) >= ReskinCooldown;
        }

        private void UpdatePlayerCooldown(BasePlayer player)
        {
            _playerCooldowns[player.userID] = DateTime.Now;
        }

        private TimeSpan GetRemainingCooldown(BasePlayer player)
        {
            return _playerCooldowns.TryGetValue(player.userID, out var lastUsage)
                ? DateTime.Now.Subtract(lastUsage)
                : ReskinCooldown;
        }
        #endregion
    }
}