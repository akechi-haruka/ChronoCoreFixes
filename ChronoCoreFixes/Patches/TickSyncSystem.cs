using HarmonyLib;
using RD1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChronoCoreFixes.Patches {

    public class RequestMatchingDataCCF : RequestMatchingData {
        public String ccf_version = Plugin.VER;
        public int ccf_fps = CT.FPS;
        public int ccf_ctd = CT.TURN_STEP_SECOND;
        public String tvc = Plugin.Md5("hkb_Data/Managed/Assembly-CSharp.dll") + "," + Plugin.Md5("hkb.exe") + "," + Plugin.Md5("LocalOptionData/Params/battle/hkb_btlsys.csv") + Plugin.Md5("BepInEx/Plugins/ChronoCoreFixes.dll");
    }

    public class ResponseMatchingDataCCF : ResponseMatchingData {
        public String opp_ccf_ver;
        public int opp_ccf_fps;
        public int opp_ccf_ctd;
        public String tvc;
    }

    internal class TickSyncSystem {

        public static void Reset() {
            Plugin.ApplyTurnStepMod(Plugin.BattleEngineLockStepModifier.Value);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(JsonUtility), "ToJson", typeof(object))]
        static bool ToJson(object obj, ref string __result) {
            if (obj is RequestMatchingData rmd1) {
                Plugin.Log.LogDebug("Replacing RequestMatchingData");
                RequestMatchingDataCCF rmd2 = new RequestMatchingDataCCF {
                    battle_revision = rmd1.battle_revision,
                    battle_type = rmd1.battle_type,
                    command = rmd1.command,
                    game_id = rmd1.game_id,
                    keychip_id = rmd1.keychip_id,
                    last_matched_user_id = rmd1.last_matched_user_id,
                    matching_code = rmd1.matching_code,
                    national_rank = rmd1.national_rank,
                    place_id = rmd1.place_id,
                    rom_version = rmd1.rom_version,
                    user_id = rmd1.user_id,
                    winning_streak_num = rmd1.winning_streak_num
                };
                __result = JsonUtility.ToJson(rmd2, false);
                return false;
            } else {
                return true;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetMatchingMgr), "ExecuteWaitMatching")]
        static void ExecuteWaitMatching(NetMatchingMgr __instance) {
            if (__instance.m_TCP.IsConnected()) {
                byte[] array = new byte[CT.NET_RECV_MATCHING_DATA_SIZE];
                if (__instance.m_TCP.Receive(ref array, array.Length) > 0) {
                    string @string = Encoding.UTF8.GetString(array);
                    if (@string.Contains("KeepAlive")) {
                        return;
                    }
                    ResponseMatchingDataCCF resp = JsonUtility.FromJson<ResponseMatchingDataCCF>(@string);

                    Plugin.Log.LogDebug(resp.tvc);

                    if (resp.opp_ccf_ver != null) {
                        Plugin.Log.LogDebug("Opponent's CCF version: " + resp.opp_ccf_ver);

                        int myTickSpeed = CT.FPS * CT.TURN_STEP_SECOND;
                        int oppTickSpeed = resp.opp_ccf_fps * resp.opp_ccf_ctd;

                        if (myTickSpeed == oppTickSpeed) {
                            Plugin.Log.LogInfo("Tick speed is synchronized to " + myTickSpeed);
                        } else if (oppTickSpeed > myTickSpeed) {
                            int delay = oppTickSpeed / myTickSpeed;
                            Plugin.Log.LogMessage("Tick delay synchronized to " + delay);
                            Plugin.ApplyTurnStepMod(delay);
                        } else if (oppTickSpeed < myTickSpeed) {
                            int delay = myTickSpeed / oppTickSpeed;
                            Plugin.Log.LogInfo("Opponent must synchonize tick speed to " + delay);
                        }

                    } else {
                        Plugin.Log.LogWarning("Opponent not using CCF, can't apply frame sync");
                    }

                    __instance.response = resp;
                    SingletonMonoBehaviour<NetworkManager>.Instance.Channel = __instance.response.channel;
                    SingletonMonoBehaviour<NetworkManager>.Instance.ReflectorServerAddress = __instance.response.host;
                    SingletonMonoBehaviour<NetworkManager>.Instance.ReflectorServerPort = __instance.response.port;

                    __instance.LeaveMatching();
                    string text = string.Format("マッチング成立:チャンネル:{0}", SingletonMonoBehaviour<NetworkManager>.Instance.Channel);
                    LoggerGenerics<HKBDebug.Network>.Log(text);
                    string[] array2 = @string.Split(',');
                    for (int i = 0; i < array2.Length; i++) {
                        LoggerGenerics<HKBDebug.Network>.Log(" " + array2[i]);
                    }
                    __instance.GoNext(NetMatchingMgr.State.SyncUserData);
                }
            }
        }
    }
}
