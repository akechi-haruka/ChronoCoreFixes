using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Events;

namespace ChronoCoreFixes.Patches {
    internal class GraphicPatches {

        #region Sandglass related
        [HarmonyPrefix, HarmonyPatch(typeof(SandglassManager), "Appear")]
        static bool Appear(CT.SIDE side) {
            return Plugin.GraphicShowSandglass.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SandglassManager), "DisAppear")]
        static bool DisAppear(CT.SIDE side) {
            return Plugin.GraphicShowSandglass.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SandglassManager), "PlaySandglassAction")]
        static bool PlaySandglassAction(CT.SIDE side, int damage, int cure, UnityAction on_end) {
            return Plugin.GraphicShowSandglass.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SandglassManager), "PlaySandglassActionCore")]
        static bool PlaySandglassActionCore(CT.SIDE side, int damage, int cure) {
            return Plugin.GraphicShowSandglass.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SandglassManager), "SlideFrontAll")]
        static bool SlideFrontAll(int life, CT.SIDE side) {
            return Plugin.GraphicShowSandglass.Value;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SandglassManager), "PlayHitEffect")]
        static bool PlayHitEffect(CT.SIDE side, int line_index = 2) {
            return Plugin.GraphicShowSandglass.Value;
        }
        #endregion
    }
}
