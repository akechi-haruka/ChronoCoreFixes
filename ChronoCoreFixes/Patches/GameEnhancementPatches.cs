using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChronoCoreFixes.Patches {
    internal class GameEnhancementPatches {

        // timer modification and reconnect reset on entering matching
        [HarmonyPrefix, HarmonyPatch(typeof(HeaderInformation), "GetSceneTime")]
        static bool GetSceneTime(ref int __result, HeaderInformation.SceneName scene) {
            switch (scene) {
                case HeaderInformation.SceneName.Login:
                    __result = 30;
                    break;
                case HeaderInformation.SceneName.Home:
                    __result = Plugin.ConfigMenuTimer.Value;
                    break;
                case HeaderInformation.SceneName.Matching:
                    ReconnectSystem.Reset();
                    Plugin.FireMatchingSettingChanged(); // force apply any interim changes to matching adapter
                    __result = Plugin.ConfigMatchingTimer.Value;
                    break;
                case HeaderInformation.SceneName.BattleResult:
                    __result = 5;
                    break;
                default:
                    __result = -1;
                    break;
            }
            return false;
        }
    }
}
