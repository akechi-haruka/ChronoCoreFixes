using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChronoCoreFixes.Patches {
    internal class TranslationPatches {

        // Hard translation for Battle error messages
        [HarmonyPostfix, HarmonyPatch(typeof(BattleCaution), "Init")]
        static void Init(BattleCaution __instance) {
            if (Plugin.ConfigHardTranslations.Value) {
                AccessTools.DeclaredField(typeof(BattleCaution), "m_CautionString").SetValue(__instance, File.ReadAllLines("BepInEx/Translation/en/Text/CFF_BATTLE_CAUTION.txt"));
            }
        }

        // String length fixes (will crash otherwise if CFF_EFF_*.txt are edited) for the keyword parser plus extra debugging
        [HarmonyPrefix, HarmonyPatch(typeof(CardParam), "StrToLinkInfo")]
        static bool StrToLinkInfo(string str, List<LinkInfo> link_info_list) {
            // adjust this based on translation length
            int max_keyword_length = 2;
            int max_status_length = 2;
            foreach (string eff in CT.KEYWORD_STR) {
                max_keyword_length = Math.Max(max_keyword_length, eff.Length);
            }
            foreach (string eff in CT.BAD_STATUS_STR) {
                max_status_length = Math.Max(max_status_length, eff.Length);
            }

            link_info_list.Clear();
            int length = str.Length;
            if (length >= 2) {
                List<string> tags = new List<string>();
                int start_index = str.IndexOf(LinkInfo.LINK_STR_SRC_0);
                while (start_index >= 0 && start_index < length) {
                    int end_index = str.IndexOf(LinkInfo.LINK_STR_SRC_1, start_index);
                    if (end_index < 0) {
                        break;
                    }
                    int string_length = end_index - start_index - 1; // adds the I/II/III UTF character
                    if (string_length > 0) {
                        tags.Add(str.Substring(start_index + 1, string_length));
                    }
                    start_index = str.IndexOf(LinkInfo.LINK_STR_SRC_0, end_index);
                }
                if (tags.Count > 0) {
                    for (int i = 0; i < tags.Count; i++) {
                        int tag_length = tags[i].Length;
                        bool found_keyword = false;
                        for (int j = 1; j < 13; j++) { // skip the empty keyword
                            int keyword_length = CT.KEYWORD_STR[j].Length;
                            if (tag_length >= keyword_length) {
                                string text = tags[i].Substring(0, keyword_length);
                                if (text == CT.KEYWORD_STR[j]) {
                                    LinkInfo linkInfo = new LinkInfo {
                                        m_Keyword = (CT.KEYWORD)j
                                    };
                                    if (tags[i].Length > keyword_length) {
                                        string text2 = tags[i].Substring(keyword_length, 1);
                                        if (text2 == " ") { // fix offset for space in English translation
                                            text2 = tags[i].Substring(keyword_length + 1, 1);
                                        }
                                        if (text2 == "Ⅰ") {
                                            linkInfo.m_Grade = 1;
                                        } else if (text2 == "Ⅱ") {
                                            linkInfo.m_Grade = 2;
                                        } else if (text2 == "Ⅲ") {
                                            linkInfo.m_Grade = 3;
                                        } else {
                                            Plugin.Log.LogError("Invalid grade indicator in keyword: " + tags[i]);
                                        }
                                    }
                                    link_info_list.Add(linkInfo);
                                    found_keyword = true;
                                    break;
                                }
                            }
                        }
                        if (!found_keyword) {
                            for (int k = 1; k < 6; k++) { // skip the empty keyword
                                int keyword_length = CT.BAD_STATUS_STR[k].Length;
                                if (tag_length >= keyword_length) {
                                    string text3 = tags[i].Substring(0, keyword_length);
                                    if (text3 == CT.BAD_STATUS_STR[k]) {
                                        LinkInfo linkInfo2 = new LinkInfo {
                                            m_BadStatus = (CT.BAD_STATUS)k
                                        };
                                        if (tags[i].Length > keyword_length) {
                                            string text4 = tags[i].Substring(keyword_length, 1);
                                            if (text4 == " ") { // fix offset for space in English translation
                                                text4 = tags[i].Substring(keyword_length + 1, 1);
                                            }
                                            if (text4 == "Ⅰ") {
                                                linkInfo2.m_Grade = 1;
                                            } else if (text4 == "Ⅱ") {
                                                linkInfo2.m_Grade = 2;
                                            } else if (text4 == "Ⅲ") {
                                                linkInfo2.m_Grade = 3;
                                            } else {
                                                Plugin.Log.LogError("Invalid grade indicator in keyword: " + tags[i]);
                                            }
                                        }
                                        link_info_list.Add(linkInfo2);
                                        found_keyword = true;
                                        break;
                                    }
                                }
                            }
                        }
                        if (!found_keyword) {
                            List<CardParam> allCardParams = CardParamMgr.GetAllCardParams();
                            if (allCardParams != null) {
                                for (int l = 0; l < allCardParams.Count; l++) {
                                    if (allCardParams[l].IsToken()) {
                                        int cardname_length = allCardParams[l].m_Name.Length;
                                        if (cardname_length <= tags[i].Length) {
                                            string text5 = tags[i].Substring(0, cardname_length);
                                            if (text5 == allCardParams[l].m_Name) {
                                                link_info_list.Add(new LinkInfo {
                                                    m_TokenCardID = allCardParams[l].m_ID
                                                });
                                                found_keyword = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                                if (!found_keyword) {
                                    Plugin.Log.LogError("No token found by name: " + tags[i]);
                                }
                            }
                        }
                        if (!found_keyword) {
                            Plugin.Log.LogError("Keyword/Effect/Token not found: " + tags[i]);
                        }
                    }
                }
            }
            return false;
        }
    }
}
