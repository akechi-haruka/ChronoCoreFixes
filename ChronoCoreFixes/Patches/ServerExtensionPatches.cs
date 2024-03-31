using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

namespace ChronoCoreFixes.Patches {
    internal class ServerExtensionPatches {

        // These make the unused field m_Name in DeckParam actually used, so you can rename things on the web UI to know what they actually are.
        #region Deck name patches
        [HarmonyPostfix, HarmonyPatch(typeof(InfoPanel3DChara), "SetParam")]
        static void SetParam(DeckParam deck, InfoPanel3DChara __instance) {
            if (Plugin.ConfigShowDeckName.Value) {
                __instance.m_JobName.text = deck.m_Name;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(PartyInfo3DChara), "SetParam")]
        static void SetParam(DeckParam deck, PartyInfo3DChara __instance) {
            if (Plugin.ConfigShowDeckName.Value) {
                __instance.m_CharacteristicText.text = deck.m_Name;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(SelectPartyPanel), "CharaPanelUpdate")]
        static void CharaPanelUpdate(PartyParam party, SelectPartyPanel __instance) {
            if (Plugin.ConfigShowDeckName.Value) {
                for (int i = 0; i < party.m_DeckParam.Length; i++) {
                    __instance.m_CharaPanelInfos[i].m_CharaName.text = party.m_DeckParam[i].m_Name;
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaStatus), "SetUp", typeof(DeckParam))]
        static void SetUp(DeckParam param, CharaStatus __instance) {
            if (Plugin.ConfigShowDeckName.Value) {
                __instance.m_CharaName.text = param.m_Name;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CharaSelectListItem), "SetParam")]
        static void SetParam(RectTransform rect, DeckParam deckparam, ToggleGroup group, int index, UnityAction<int, bool> callback, UnityAction<int> ondragcheck, UnityAction<int> ondragstart, UnityAction<int> ondragend, UnityAction<int> details, CharaSelectListItem __instance) {
            if (Plugin.ConfigShowDeckName.Value) {
                if (__instance.m_JobName != null) {
                    __instance.m_JobName.text = deckparam.m_Name;
                }
                __instance.m_CharName.text = deckparam.m_Name;
            }
        }
        #endregion
    }
}
