using HarmonyLib;
using HKBSys;
using RD1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChronoCoreFixes.Patches {
    internal class NetworkPatches {

        // LAN IP override for listening address
        [HarmonyPrefix, HarmonyPatch(typeof(NetworkManager), "SetAddress")]
        static bool SetAddress() {
            if (String.IsNullOrEmpty(Plugin.ConfigMatchingNetmask.Value)) {
                return true;
            }

            SingletonMonoBehaviour<NetworkManager>.Instance.m_Address[0] = Plugin.LocalNetworkIPOverride;
            SingletonMonoBehaviour<NetworkManager>.Instance.m_localUse = false;
            SingletonMonoBehaviour<NetworkManager>.Instance.m_matchingServerAddress = Singleton<SystemConfig>.Instance.GetMatchingServerAddress();
            SingletonMonoBehaviour<NetworkManager>.Instance.m_matchingServerPort = Singleton<SystemConfig>.Instance.GetMatchingServerPort();
            SingletonMonoBehaviour<NetworkManager>.Instance.m_placeId = Singleton<SystemConfig>.Instance.GetPlaceId();
            return false;
        }

        // Quick sync setting
        // This was needed on my laptop running this game at <5 FPS, so the other client would already time out while this thing was trying to get it's 60 update ticks in.
        [HarmonyPrefix, HarmonyPatch(typeof(NetMatchingMgr), "RecvUserData")]
        static bool RecvUserData(NetMatchingMgr __instance) {
            bool result = false;
            if (__instance.m_RcvEnemyRequest) {
                __instance.m_Counter++;
                LoggerGenerics<HKBDebug.Network>.Log("__instance.m_Counter: " + __instance.m_Counter);
                if (__instance.m_Counter > (Plugin.ConfigMatchingQuickSync.Value ? 2 : 60)) {
                    result = true;
                }
            }
            return result;
        }

    }
}
