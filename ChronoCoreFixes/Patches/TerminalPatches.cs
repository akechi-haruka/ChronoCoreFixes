using HarmonyLib;
using HkbCom;
using HKBSys;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

        [HarmonyPostfix, HarmonyPatch(typeof(ReplayDataManager), "FetchPersonalReplayData", typeof(ReplaySelectMenuData))]
        static IEnumerator FetchPersonalReplayData(IEnumerator __result, ReplaySelectMenuData replay, ReplayDataManager __instance) {
            ComDownloadUserReplay com = new ComDownloadUserReplay(new ComDownloadUserReplay.RequestData {
                replayId = replay.id
            });
            SingletonMonoBehaviour<ComIo>.Instance.Request(com);
            yield return new WaitWhile(() => com.IsBusy);
            if (com.IsError) {
                Plugin.Log.LogError("Network error downloading replay");
                yield break;
            }
            if (replay.checkSum != Util.GetHashString(com.ResData.replayData)) {
                if (!Plugin.ConfigIgnoreReplayVersion.Value) {
                    Plugin.Log.LogError("Checksum error: " + replay.checkSum + " (server), " + Util.GetHashString(com.ResData.replayData) + " (local)");
                    yield break;
                }
            }
            __instance.personalReplayParams.Add(replay.id, com.ResData.replayData);
            yield break;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ReplayDataManager), "FetchRankerReplayData", typeof(ReplaySelectMenuData))]
        static IEnumerator FetchRankerReplayData(IEnumerator __result, ReplaySelectMenuData replay, ReplayDataManager __instance) {
            ComDownloadRankerReplay com = new ComDownloadRankerReplay(new ComDownloadRankerReplay.RequestData {
                replayId = replay.id
            });
            SingletonMonoBehaviour<ComIo>.Instance.Request(com);
            yield return new WaitWhile(() => com.IsBusy);
            if (com.IsError) {
                Plugin.Log.LogError("Network error downloading replay");
                yield break;
            }
            if (replay.checkSum != Util.GetHashString(com.ResData.replayData)) {
                if (!Plugin.ConfigIgnoreReplayVersion.Value) {
                    Plugin.Log.LogError("Checksum error: " + replay.checkSum + " (server), " + Util.GetHashString(com.ResData.replayData) + " (local)");
                    yield break;
                }
            }
            __instance.replayDataRepository.Persist(replay, com.ResData.replayData);
            yield break;
        }

    }
}
