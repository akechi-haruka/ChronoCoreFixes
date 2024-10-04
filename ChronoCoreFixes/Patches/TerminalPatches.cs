using HarmonyLib;
using HKBSys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChronoCoreFixes.Patches {

    internal class TerminalPatches {

        // Toggle for terminal/satellite mode switch

        [HarmonyPrefix, HarmonyPatch(typeof(HKBSys.SystemConfig), "GetModelType")]
        static bool GetModelType(ref uint __result) {
            switch (Plugin.WantedBootMode) {
                case GameBootMode.ForceTerminal:
                    __result = 4U;
                    return false;
                case GameBootMode.ForceSatellite:
                    __result = 2U;
                    return false;
                default:
                    return true;
            }
        }

        // Toggle for forcing keyboard emulation, even if AMDaemon is present
        [HarmonyPrefix, HarmonyPatch(typeof(InputManager), "IsKeyOn")]
        public static bool IsKeyOn(ref bool __result, CT.TerminalKeyId keyId) {
            if (Plugin.ConfigForceTerminalKeyboardEmulation.Value) {
                __result = UnityEngine.Input.GetKey(InputManager.KeyBoardEmulation[(int)keyId]);
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(InputManager), "IsKeyOnNow")]
        public static bool IsKeyOnNow(ref bool __result, CT.TerminalKeyId keyId) {
            if (Plugin.ConfigForceTerminalKeyboardEmulation.Value) {
                __result = UnityEngine.Input.GetKeyDown(InputManager.KeyBoardEmulation[(int)keyId]);
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ReplayDataManager), "IsMatchCardRevision", typeof(ReplayInfo))]
        static bool IsMatchCardRevision(ReplayInfo replayInfo, ref bool __result) {
            Plugin.Log.LogDebug("Local cardRevision: " + VersionParamMgr.GetMatchingVerion() + ", Replay cardRevision: " + replayInfo.cardRevision);
            if (Plugin.ConfigIgnoreReplayVersion.Value) {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(ReplayDataManager), "IsMatchCardRevision", typeof(int))]
        static bool IsMatchCardRevision(int cardRevision, ref bool __result) {
            Plugin.Log.LogDebug("Local cardRevision: " + VersionParamMgr.GetMatchingVerion() + ", Replay cardRevision: " + cardRevision);
            if (Plugin.ConfigIgnoreReplayVersion.Value) {
                __result = true;
                return false;
            }
            return true;
        }

    }
}
