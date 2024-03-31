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
                    __result = 2U;
                    return false;
                case GameBootMode.ForceSatellite:
                    __result = 4U;
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

    }
}
