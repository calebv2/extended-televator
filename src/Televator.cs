using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Alta.Caves;
using Alta.Networking;
using Alta.Networking.Scripts.Player;
using Alta.Serialization;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Televator200.TelevatorMod), "Extended Televator", "1.0.7", "VMan")]

namespace Televator200
{
    public sealed class TelevatorMod : MelonMod
    {
        internal const int OriginalStops = 10;
        internal const int Stops = 20;
        internal const int FloorsPerStop = 10;

        private static readonly FieldInfo StopCount = Field<LevelSelector>("levelCount");
        private static readonly FieldInfo Travel = Field<LevelSelector>("linearDistance");
        private static readonly FieldInfo Progress = Field<LevelSelector>("currentProgress");
        private static readonly FieldInfo Selector = Field<LevelSelectCaveTeleporter>("levelSelector");
        private static readonly FieldInfo Origin = Field<CaveTeleporter>("layer");
        private static readonly FieldInfo Connection = Field<CaveTeleporter>("currentConnection");
        private static readonly FieldInfo Connections = Field<LevelSelectCaveTeleporter>("connections");
        private static readonly MethodInfo UpdateProgress = Method<LevelSelector>("UpdateProgress");
        private static readonly MethodInfo UpdateLevel = Method<LevelSelector>("UpdateLevel");
        private static readonly MethodInfo SetPipFuel = Method<CaveTeleporterPip>("IsHasFuelToChanged");
        private static readonly ConditionalWeakTable<LevelSelector, object> Extended = new ConditionalWeakTable<LevelSelector, object>();

        private bool installed;
        internal static MelonLogger.Instance Log { get; private set; }

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            try
            {
                Patch(Method<LevelSelector>("Awake"), nameof(BeforeAwake), nameof(AfterAwake));
                Patch(InterfaceMethod<LevelSelector, INetworkSerialized>(nameof(INetworkSerialized.Serialize)), nameof(SerializeSelector));
                Patch(InterfaceMethod<LevelSelector, INetworkSerialized>("get_MaximumBits"), postfix: nameof(MaximumBits));
                Patch(Method<LevelSelectCaveTeleporter>("UpdateLevel"), nameof(ClampInitialLevel), nameof(UpdateReadout));
                Patch(Method<LevelSelectCaveTeleporter>("InitializeConnection"), postfix: nameof(UpdatePipFuel));
                Patch(Method<CaveTeleporter>("Teleport"), nameof(AllowTeleport));
                Session.Install(HarmonyInstance);
                installed = true;
                LoggerInstance.Msg("Extended Televator ready. Surface stops 10 through 200.");
            }
            catch (Exception error)
            {
                HarmonyInstance.UnpatchSelf();
                LoggerInstance.Error("Extended Televator could not install for this game build. " + error.GetBaseException());
            }
        }

        public override void OnUpdate()
        {
            if (installed && !ApplicationManager.IsHeadless && !NetworkSceneManager.IsServer)
                Session.UpdateClient();
        }

        private void Patch(MethodBase target, string prefix = null, string postfix = null)
        {
            HarmonyInstance.Patch(target,
                prefix == null ? null : new HarmonyMethod(typeof(TelevatorMod), prefix),
                postfix == null ? null : new HarmonyMethod(typeof(TelevatorMod), postfix));
        }

        private static void BeforeAwake(LevelSelector __instance)
        {
            if (__instance.LevelCount != OriginalStops ||
                !__instance.TryGetComponent<LevelSelectCaveTeleporter>(out var owner) ||
                (int)Origin.GetValue(owner) != 0 || !ReferenceEquals(Selector.GetValue(owner), __instance))
                return;

            StopCount.SetValue(__instance, Stops);
            Travel.SetValue(__instance, (float)Travel.GetValue(__instance) * (Stops - 1) / (OriginalStops - 1));
            if (!ApplicationManager.IsHeadless)
                Gauge.Extend(__instance);
            Extended.Add(__instance, new object());
        }

        private static void AfterAwake(LevelSelector __instance)
        {
            if (IsExtended(__instance) && !ApplicationManager.IsHeadless)
                Gauge.AddReadout(__instance.GetComponent<LevelSelectCaveTeleporter>());
        }

        private static bool SerializeSelector(LevelSelector __instance, Stream stream)
        {
            if (!IsExtended(__instance)) return true;

            if (stream.IsWriting)
            {
                SelectorWire.Write(stream, __instance.CurrentLevel, (float)Progress.GetValue(__instance));
            }
            else
            {
                int level = SelectorWire.Read(stream, out float progress);
                if (stream.IsReading && !NetworkSceneManager.IsServer)
                {
                    Progress.SetValue(__instance, progress);
                    UpdateProgress.Invoke(__instance, null);
                    UpdateLevel.Invoke(__instance, new object[] { level });
                }
            }
            return false;
        }

        private static void MaximumBits(LevelSelector __instance, ref int __result)
        {
            if (IsExtended(__instance)) __result = SelectorWire.BitCount;
        }

        private static void ClampInitialLevel(LevelSelectCaveTeleporter __instance, ref int level)
        {
            if (level < 0 && IsExtended(__instance)) level = 0;
        }

        private static void UpdateReadout(LevelSelectCaveTeleporter __instance, int level)
        {
            if (!ApplicationManager.IsHeadless && IsExtended(__instance))
                Gauge.UpdateReadout(__instance, (level + 1) * FloorsPerStop);
        }

        private static void UpdatePipFuel(LevelSelectCaveTeleporter __instance)
        {
            if (ApplicationManager.IsHeadless || !IsExtended(__instance)) return;
            var pips = ((LevelSelector)Selector.GetValue(__instance)).Pips;
            var connections = (CaveTeleportConnection[])Connections.GetValue(__instance);
            for (int i = 0; i < pips.Length; i++)
                SetPipFuel.Invoke(pips[i], new object[] { connections[i].HasFuelTo });
        }

        private static bool AllowTeleport(CaveTeleporter __instance, IPlayer player)
        {
            if (!(__instance is LevelSelectCaveTeleporter owner) || !IsExtended(owner)) return true;
            var connection = (CaveTeleportConnection)Connection.GetValue(owner);
            if (connection == null || connection.ToLayer != owner.TargetLayer || !connection.IsValid) return false;
            if (owner.TargetLayer < OriginalStops * FloorsPerStop || !NetworkSceneManager.IsServer) return true;
            if (Session.SupportsDeepStops(player)) return true;

            if (player?.PlayerController != null)
                PlayerCommunicationManager.Instance?.SendMessageToPlayer(player,
                    "Install Extended Televator to travel past floor 100, or turn the wheel back to a shallower stop.");
            return false;
        }

        private static bool IsExtended(LevelSelectCaveTeleporter owner) =>
            IsExtended((LevelSelector)Selector.GetValue(owner));

        private static bool IsExtended(LevelSelector selector) => selector != null &&
            Extended.TryGetValue(selector, out _);

        internal static FieldInfo Field<T>(string name) => AccessTools.DeclaredField(typeof(T), name) ??
            throw new MissingFieldException(typeof(T).FullName, name);

        internal static MethodInfo Method<T>(string name) => AccessTools.DeclaredMethod(typeof(T), name) ??
            throw new MissingMethodException(typeof(T).FullName, name);

        private static MethodInfo InterfaceMethod<T, TInterface>(string name)
        {
            var map = typeof(T).GetInterfaceMap(typeof(TInterface));
            int index = Array.FindIndex(map.InterfaceMethods, method => method.Name == name);
            if (index < 0) throw new MissingMethodException(typeof(TInterface).FullName, name);
            return map.TargetMethods[index];
        }
    }

    internal static class SelectorWire
    {
        internal const int BitCount = 11;
        private const int ProgressBits = 7;
        private const int LastVanillaProgress = 101;
        private const int LastVanillaIndex = TelevatorMod.OriginalStops - 1;
        private const int LastIndex = TelevatorMod.Stops - 1;

        // Native format: 7 bits of progress followed by a 4-bit index. Values above 101
        // are unused by the native writer. The index stays within the vanilla pip array.
        internal static void Write(Stream stream, int index, float progress)
        {
            index = Mathf.Clamp(index, 0, LastIndex);
            int legacyIndex = Mathf.Min(index, LastVanillaIndex);
            if (index <= LastVanillaIndex)
            {
                float legacyProgress = Mathf.Clamp01(progress * LastIndex / LastVanillaIndex);
                stream.SerializeFloat01(ref legacyProgress);
            }
            else
            {
                uint code = (uint)(LastVanillaProgress + index - LastVanillaIndex);
                stream.SerializeBits(ref code, ProgressBits);
            }
            stream.SerializeInteger(ref legacyIndex, 0, TelevatorMod.OriginalStops);
        }

        internal static int Read(Stream stream, out float progress)
        {
            uint code = 0;
            int index = 0;
            stream.SerializeBits(ref code, ProgressBits);
            stream.SerializeInteger(ref index, 0, TelevatorMod.OriginalStops);

            int deepIndex = LastVanillaIndex + (int)code - LastVanillaProgress;
            if (index == LastVanillaIndex && deepIndex > LastVanillaIndex && deepIndex <= LastIndex)
            {
                progress = (float)deepIndex / LastIndex;
                return deepIndex;
            }

            progress = Math.Min(code, (uint)LastVanillaProgress) / (float)LastVanillaProgress * LastVanillaIndex / LastIndex;
            return Mathf.Clamp(index, 0, LastVanillaIndex);
        }
    }
}
