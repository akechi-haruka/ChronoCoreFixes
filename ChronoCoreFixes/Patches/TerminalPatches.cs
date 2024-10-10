using AMDaemon;
using HarmonyLib;
using HkbCom;
using HKBSys;
using System.Collections;
using System.Collections.Generic;
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

        [HarmonyPrefix, HarmonyPatch(typeof(InputManager), "IsKeyOn")]
        static bool IsKeyOn(CT.TerminalKeyId keyId, ref bool __result) {
            if (Plugin.ConfigAMDAnalogInsteadOfButtons.Value) {
                if (keyId == CT.TerminalKeyId.Up || keyId == CT.TerminalKeyId.Right || keyId == CT.TerminalKeyId.Down || keyId == CT.TerminalKeyId.Left) {
                    __result = UpdateAnalog(keyId);
                    return false;
                }
            }
            return true;
        }

        private static Dictionary<CT.TerminalKeyId, bool> prevFrame = new Dictionary<CT.TerminalKeyId, bool>() {
            { CT.TerminalKeyId.Up, false },
            { CT.TerminalKeyId.Right, false },
            { CT.TerminalKeyId.Down, false },
            { CT.TerminalKeyId.Left, false },
        };

        [HarmonyPrefix, HarmonyPatch(typeof(InputManager), "IsKeyOnNow")]
        static bool IsKeyOnNow(CT.TerminalKeyId keyId, ref bool __result) {
            if (Plugin.ConfigAMDAnalogInsteadOfButtons.Value) {
                if (keyId == CT.TerminalKeyId.Up || keyId == CT.TerminalKeyId.Right || keyId == CT.TerminalKeyId.Down || keyId == CT.TerminalKeyId.Left) {
                    bool b = UpdateAnalog(keyId);
                    __result = b && !prevFrame[keyId];
                    prevFrame[keyId] = b;
                    return false;
                }
            }
            return true;
        }

        private static double map(double x, double in_min, double in_max, double out_min, double out_max) {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        private static bool UpdateAnalog(CT.TerminalKeyId keyId) {
            InputUnit unit = AMDaemon.Input.System;

            double deadzone = Plugin.ConfigIO4StickDeadzone.Value / 100F;
            var ax = unit.GetAnalog(Plugin.AnalogX).Value;
            var ay = unit.GetAnalog(Plugin.AnalogY).Value;
            double x = map(ax, 0, 1, -1, 1);
            double y = map(ay, 0, 1, -1, 1);
            
            if (Plugin.ConfigIO4AxisXInvert.Value) {
                x = -x;
            }
            if (Plugin.ConfigIO4AxisYInvert.Value) {
                y = -y;
            }

            bool on = (
                (keyId == CT.TerminalKeyId.Up && y > deadzone) ||
                (keyId == CT.TerminalKeyId.Right && x > deadzone) ||
                (keyId == CT.TerminalKeyId.Down && y < -deadzone) ||
                (keyId == CT.TerminalKeyId.Left && x < -deadzone)
            );

            return on;
        }

    }
}
