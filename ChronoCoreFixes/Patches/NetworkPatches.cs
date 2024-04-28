using HarmonyLib;
using HKBSys;
using RD1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

        /*[HarmonyPrefix, HarmonyPatch(typeof(Battle), "CheckNotEndEvent")]
        static bool CheckNotEndEvent(Battle __instance, ref bool __result) {
            if (!Plugin.ConfigIgnoreCheckNotEndEvent.Value) {
                return true;
            }
            for (int i = 0; i < __instance.m_ExeEventParam.Length; i++) {
                if (!__instance.m_ExeEventParam[i].IsEmpty() && !__instance.m_ExeEventParam[i].m_ExeHostType[(int)__instance.m_HostType] && __instance.m_ExeEventParam[i].m_ExeHostType[(int)__instance.m_HostTypeEnemy] && __instance.m_BtlFrame - __instance.m_ExeEventParam[i].m_EndFrame > 600) {
                    Plugin.Log.LogWarning("CheckNotEndEvent triggered: " + (__instance.m_BtlFrame - __instance.m_ExeEventParam[i].m_EndFrame));
                    break;
                }
            }
            __result = false;
            return false;
        }*/

        [HarmonyPrefix, HarmonyPatch(typeof(PacketBattleUserData), "CopyTo")]
        static bool CopyTo(ref PacketBattleUserData dest, PacketBattleUserData __instance) {
            PacketBattleUserData i = __instance;
            if (i.m_Name == null) {
                Plugin.Log.LogWarning("Recived matching data: name was null");
                i.m_Name = "UNKNOWN";
            }
            if (i.m_Address == null) {
                Plugin.Log.LogWarning("Recived matching data: address was null");
                i.m_Address = "UNKNOWN";
            }
            if (i.m_PrefecturesName == null) {
                Plugin.Log.LogWarning("Recived matching data: prefecture name was null");
                i.m_PrefecturesName = "UNKNOWN";
            }
            if (i.m_LocationName == null) {
                Plugin.Log.LogWarning("Recived matching data: location name was null");
                i.m_LocationName = "UNKNOWN";
            }
            return true;
        }
    }
}
