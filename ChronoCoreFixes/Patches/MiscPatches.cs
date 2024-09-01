using Comio;
using Comio.BD15093_6;
using HarmonyLib;
using MU3.Mecha;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChronoCoreFixes.Patches {
    internal class MiscPatches {

        // I THINK I fixed a bug where the wrong caution is displayed but this may actually be obsolete?
        [HarmonyPrefix, HarmonyPatch(typeof(BattleCard), "CheckUsable")]
        static bool CheckUsable(ref CT.BATTLE_CAUTION __result, BattleCard __instance, bool check_mp_disp = true) {
            Force force = Battle.Instance.GetForce(__instance.m_HostType);
            Chara chara = Battle.Instance.GetChara(__instance.m_HostType, __instance.m_CharIndex);
            CT.BATTLE_CAUTION battle_CAUTION;
            if (force != null && chara != null) {
                if (__instance.m_Param != null) {
                    if (__instance.m_Status == CT.CARD_STATUS.HAND_0 || __instance.m_Status == CT.CARD_STATUS.HAND_1 || __instance.m_Status == CT.CARD_STATUS.HAND_2) {
                        if (__instance.m_Param.m_Type == CT.CARD_TYPE.MGC && chara.m_Flag2[7]) {
                            battle_CAUTION = CT.BATTLE_CAUTION.SEAL_MAGIC;
                        } else if (__instance.CheckUsableLevel(chara.m_P[0].m_Level)) {
                            int manaCost = __instance.GetManaCost();
                            int rsvEntryCardCost = force.GetRsvEntryCardCost(__instance);
                            int num = (int)force.m_Mana - rsvEntryCardCost;
                            int num2 = (int)force.m_ManaDisp - rsvEntryCardCost;
                            if (manaCost <= num && (!check_mp_disp || manaCost <= num2)) {
                                battle_CAUTION = CT.BATTLE_CAUTION.NON;
                            } else {
                                battle_CAUTION = CT.BATTLE_CAUTION.LESS_MP;
                            }
                        } else {
                            battle_CAUTION = CT.BATTLE_CAUTION.LESS_LEVEL;
                        }
                    } else {
                        battle_CAUTION = CT.BATTLE_CAUTION.NOT_USE;
                    }
                } else {
                    battle_CAUTION = CT.BATTLE_CAUTION.NOT_USE;
                }
                if (battle_CAUTION == CT.BATTLE_CAUTION.NON) {
                    if (Battle.Instance.CheckChangeHandCard(__instance.m_HostType, __instance.m_CharIndex)) {
                        battle_CAUTION = CT.BATTLE_CAUTION.NOT_USE_TMP;
                    } else if (chara.m_SubStatus == CT.CHAR_SUB_STATUS.EVENT_ATTACK || chara.m_SubStatus == CT.CHAR_SUB_STATUS.EVENT_ENTRY_MAGIC || chara.m_SubStatus == CT.CHAR_SUB_STATUS.EVENT_MAGIC) {
                        battle_CAUTION = CT.BATTLE_CAUTION.NOT_USE_TIME_IN_EVENT;
                    }
                }
            } else {
                battle_CAUTION = CT.BATTLE_CAUTION.NOT_USE;
            }
            __result = battle_CAUTION;
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Battle), "Start")]
        static void Start(Battle __instance) {
            AccessTools.DeclaredPropertySetter(typeof(Battle), "m_TurnStepFrameMax").Invoke(__instance, new object[] { CT.TURN_STEP_SECOND * 30 });
            Plugin.Log.LogDebug("Battle m_TurnStepFrameMax readjusted to " + __instance.m_TurnStepFrameMax);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Force), "Init")]
        static void Init(Force __instance) {
            AccessTools.DeclaredPropertySetter(typeof(Force), "m_TurnStepFrameMax").Invoke(__instance, new object[] { CT.TURN_STEP_SECOND * 30 });
            Plugin.Log.LogDebug("Force m_TurnStepFrameMax readjusted to " + __instance.m_TurnStepFrameMax);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(MechaManager), "initialize")]
        static bool initialize(ref MechaManager.InitParam initParam) {
            if (Plugin.LEDPort.Value > 0) {
                Plugin.Log.LogInfo("Redirecting LED COM port to " + Plugin.LEDPort.Value);
                initParam.ledParam.comName = "COM" + Plugin.LEDPort.Value;
            }
            BoardCtrl15093_6.BoardNo.text = "15093-06";
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(BoardCtrl15093_6), "_md_initBoard_GetFirmSum")]
        static bool _md_initBoard_GetFirmSum(BoardCtrl15093_6 __instance) {
            global::UnityEngine.Debug.Log("BoardCtrl._md_initBoard_GetFirmSum()");
            if (__instance.execCommand(__instance._getFirmSumCommand)) {
                __instance._boardSpecInfo.firmInfo.sum = __instance._getFirmSumCommand.getSum();
                __instance._isBoardSpecInfoRecv = true;
                if (!__instance._boardSpecInfo.firmInfo.isAppliMode()) {
                    Plugin.Log.LogError("Appli mode is off");
                    __instance._setError(ErrorNo.FirmError);
                    return false;
                }
                if (!__instance._boardSpecInfo.customChipNo.isEqual(__instance._initParam.customChipNo)) {
                    Plugin.Log.LogError("board chip no = \""+ __instance._boardSpecInfo.customChipNo.text + "\" vs expected \""+__instance._initParam.customChipNo.text + "\"");
                    __instance._setError(ErrorNo.FirmVersionError);
                    return false;
                }
                if (!__instance.checkFirmVersion(__instance._boardSpecInfo.firmInfo.revision, __instance._initParam.firmVersion)) {
                    Plugin.Log.LogError("board rev = \"" + __instance._boardSpecInfo.firmInfo.revision + "\" vs expected \"" + __instance._initParam.firmVersion + "\"");
                    __instance._setError(ErrorNo.FirmVersionError);
                    return false;
                }
                if (__instance._boardSpecInfo.firmInfo.sum != __instance._initParam.firmSum) {
                    Plugin.Log.LogError("board sum = \"" + __instance._boardSpecInfo.firmInfo.sum + "\" vs expected \"" + __instance._initParam.firmSum + "\"");
                    __instance._setError(ErrorNo.FirmVersionError);
                    return false;
                }
                __instance._setTimeoutCommand.setTimeout(0);
                __instance._mode = BoardCtrl15093_6.Mode.MD_InitBoard_SetTimeoutInfinite;
            }
            return false;
        }

    }
}
