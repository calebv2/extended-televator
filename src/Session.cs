using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alta.Networking;
using Alta.Networking.Scripts.Player;
using Alta.Serialization;
using ATT.Character.QuickAccessMenu;
using HarmonyLib;
using UnityEngine;

namespace Televator200
{
    internal static class Session
    {
        private const string Hello = "televator200.client.v1";
        private const string Ready = "televator200.server.v1";
        private static readonly FieldInfo Command = TelevatorMod.Field<CommandSync>("commandToSync");
        private static readonly FieldInfo Sync = TelevatorMod.Field<CommandSync>("syncCommand");
        private static readonly ConditionalWeakTable<IPlayer, object> Clients = new ConditionalWeakTable<IPlayer, object>();
        private static IPlayer localPlayer;
        private static int attempts;
        private static float nextAttempt;
        private static bool ready;

        internal static void Install(HarmonyLib.Harmony harmony)
        {
            harmony.Patch(TelevatorMod.Method<CommandSync>("SyncCommand"),
                postfix: new HarmonyMethod(typeof(Session), nameof(ReceiveHello)));
            harmony.Patch(AccessTools.DeclaredMethod(typeof(PlayerMessageDisplay), "Display",
                new[] { typeof(string), typeof(float), typeof(DisplayMessageType) }),
                prefix: new HarmonyMethod(typeof(Session), nameof(ReceiveReady)));
            Player.OnPlayerRemoved += Forget;
        }

        internal static bool SupportsDeepStops(IPlayer player) =>
            player != null && (Clients.TryGetValue(player, out _) ||
                (NetworkSceneManager.IsLocalTest && ReferenceEquals(player, Player.Current)));

        private static void Forget(Player player)
        {
            Clients.Remove(player);
            if (!ReferenceEquals(localPlayer, player)) return;
            localPlayer = null;
            ready = false;
            attempts = 0;
        }

        internal static void UpdateClient()
        {
            var player = Player.Current;
            if (!ReferenceEquals(player, localPlayer))
            {
                localPlayer = player;
                ready = false;
                attempts = 0;
                nextAttempt = 0;
            }
            if (ready || attempts >= 4 || player?.PlayerController == null || Time.unscaledTime < nextAttempt)
                return;

            var commandSync = CommandSync.Instance;
            if (commandSync == null || !(Sync.GetValue(commandSync) is MethodSync sync)) return;
            nextAttempt = Time.unscaledTime + 5;
            attempts++;
            try
            {
                // The native Quick Access transport supplies the connection's IPlayer to the server.
                Command.SetValue(commandSync, Hello);
                sync.SendToChunks(Array.Empty<IPlayer>());
            }
            catch (Exception error)
            {
                if (attempts == 4) TelevatorMod.Log.Warning("Extended Televator handshake failed. " + error.GetBaseException().Message);
            }
        }

        private static void ReceiveHello(CommandSync __instance, IPlayer player, Stream stream)
        {
            if (!NetworkSceneManager.IsServer || !stream.IsReading || player?.PlayerController == null ||
                !ReferenceEquals(player.PlayerController.NetworkPlayer, player) || (string)Command.GetValue(__instance) != Hello)
                return;

            Clients.GetValue(player, _ => new object());
            PlayerCommunicationManager.Instance?.SendMessageToPlayer(player, Ready, 0f);
        }

        private static bool ReceiveReady(string message, float duration)
        {
            if (NetworkSceneManager.IsServer || message != Ready || duration != 0f) return true;
            ready = true;
            return false;
        }
    }
}
