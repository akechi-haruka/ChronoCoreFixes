using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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

    }
}
