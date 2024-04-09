using AMDaemon;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ChronoCoreFixes.Patches;
using HarmonyLib;
using HKBSys;
using RD1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking.Match;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChronoCoreFixes {

    public enum GameBootMode {
        Auto, ForceTerminal, ForceSatellite
    }

    [BepInPlugin("eu.haruka.gmg.chrono.fixes", "Chrono Regalia Core Fixes", "2.2.1")]
    public class Plugin : BaseUnityPlugin {

        private static Plugin Instance;
        public static ManualLogSource Log { get; private set; }

        public static ConfigEntry<bool> ConfigShowMouse;
        public static ConfigEntry<int> ConfigMenuTimer;
        public static ConfigEntry<int> ConfigMatchingTimer;
        public static ConfigEntry<int> ConfigNetworkTimeout;
        public static ConfigEntry<bool> ConfigAutoReconnect;
        public static ConfigEntry<int> ConfigAutoReconnectRetries;
        public static ConfigEntry<string> ConfigMatchingNetmask;
        public static ConfigEntry<bool> ConfigMatchingQuickSync;
        public static String LocalNetworkIPOverride;
        public static ConfigEntry<bool> GraphicShowSandglass;
        public static ConfigEntry<bool> GraphicShowVolumetricClouds;
        public static ConfigEntry<bool> GraphicShowSimpleClouds;
        public static ConfigEntry<bool> GraphicShowLighting;
        public static ConfigEntry<bool> GraphicShowLighting2;
        public static ConfigEntry<bool> GraphicForceDisableVsync;
        public static ConfigEntry<int> GraphicFPS;
        public static ConfigEntry<int> BattleEngineLockStepModifier;
        public static ConfigEntry<ShadowResolution> GraphicShadowResolution;
        public static ConfigEntry<ShadowQuality> GraphicShadowQuality;
        public static ConfigEntry<AnisotropicFiltering> GraphicAnisotropicFiltering;
        public static ConfigEntry<int> GraphicAntiAliasing;
        public static ConfigEntry<bool> ConfigShowDeckName;
        public static ConfigEntry<bool> ConfigHardTranslations;
        public static ConfigEntry<int> ConfigMaxCP;
        public static ConfigEntry<bool> ConfigPathFixes;
        public static ConfigEntry<bool> ConfigForceTerminalKeyboardEmulation;

        public static String YDrivePath = null;
        public static String OptionPath = null;
        public static List<OptionImageInfo> optiondata = null;

        public static GameBootMode WantedBootMode { get; private set; }

        public static void FireMatchingSettingChanged() {
            Instance.ConfigMatchingNetmask_SettingChanged(null, null);
        }

        private void Awake() {

            Instance = this;
            Log = Logger;

            #region Set up config
            ConfigPathFixes = Config.Bind("General", "Y: Drive Fix", true, "Redirects the Y:\\ drive (and the option folder) to the path specified in segatools.ini");
            ConfigMenuTimer = Config.Bind("General", "Menu Timer", 300, new ConfigDescription("Changes the main menu timer", new AcceptableValueRange<int>(300, 999)));
            ConfigMatchingTimer = Config.Bind("General", "Matching Timer", 30, new ConfigDescription("Changes the matching timer", new AcceptableValueRange<int>(30, 500)));

            ConfigNetworkTimeout = Config.Bind("Network", "Network Timeout", 30, new ConfigDescription("Changes the amount of seconds until online gameplay will time out due to disconnection", new AcceptableValueRange<int>(5, 60)));
            ConfigAutoReconnect = Config.Bind("Network", "Auto-Reconnect", true, new ConfigDescription("If timeout is hit, attempts to reconnect."));
            ConfigAutoReconnectRetries = Config.Bind("Network", "Auto-Reconnect Retries", 1, new ConfigDescription("How often reconnection is attempted.", new AcceptableValueRange<int>(1, 999)));
            ConfigMatchingNetmask = Config.Bind("Network", "Matching Network Adapter (*)", "", "The subnet mask of the network adapter to use.\nThe player you are trying to match with must also be in this subnet.\nFor example, if you have an IP address of \"5.7.3.15\", then enter \"5.7.3.255\" here.\n\n(*) Leave blank to use default game behaviour (157.109.255.255)");
            ConfigMatchingNetmask.SettingChanged += ConfigMatchingNetmask_SettingChanged;
            ConfigMatchingQuickSync = Config.Bind("Network", "Matching Sync Patch", false, "Forces matching P2P connections to be \"ready\" earlier. Enable on slow computers, and only on those. Having this falsely enabled will cause a matching connection timeout. This setting does not need to match up on both players.");

            ConfigShowMouse = Config.Bind("Input", "Show Mouse", true, "Shows the mouse cursor");
            ConfigForceTerminalKeyboardEmulation = Config.Bind("Input", "Terminal Keyboard Emulation", true, "Uses the built-in keyboard emulation if using the terminal.\n\nConfirm: Enter\nCancel: Backspace\nArrows: Arrow Keys\nPage Keys: Numpad4/6");

            GraphicShowSandglass = Config.Bind("Graphics", "Sandglass", true, "Renders the \"sandglass\" on both player's zones. (~10% rendering time)");
            GraphicShowVolumetricClouds = Config.Bind("Graphics", "Volumetric Clouds", true, "Renders volumetric clouds with light rays. (~20% rendering time)");
            GraphicShowSimpleClouds = Config.Bind("Graphics", "Simple Clouds", true, "Renders simple clouds. (~5% rendering time)");
            GraphicShowLighting = Config.Bind("Graphics", "Lighting (Main)", true, "Renders light. (~15% rendering time)");
            GraphicShowLighting2 = Config.Bind("Graphics", "Lighting (Probe)", true, "Renders light. (~5% rendering time)");
            GraphicForceDisableVsync = Config.Bind("Graphics", "VSync Fix", true, "Disables forced VSync. Breaks animation speed on >60Hz monitors. See advanced settings for further usage.");
            GraphicFPS = Config.Bind("Graphics", "FPS cap (A)(!)(*)(*)", 30, new ConfigDescription("Changes the combat engine's expected refresh rate.\n\n(A) Advanced setting\n(!) For online play, combat speed must match or the faster player will time out! Do not enter public lobbies with this setting changed!\n(*) V-Sync Fix must be enabled for this to work\n(*) Changes require game restart", new AcceptableValueRange<int>(30, 144), new ConfigurationManagerAttributes() {
                IsAdvanced = true
            }));
            GraphicShadowResolution = Config.Bind("Graphics", "Shadow Resolution", QualitySettings.shadowResolution, "Set the unity shadow resolution");
            GraphicShadowQuality = Config.Bind("Graphics", "Shadow Quality", QualitySettings.shadows, "Set the unity shadow quality");
            GraphicAnisotropicFiltering = Config.Bind("Graphics", "Anisotropic Filtering", QualitySettings.anisotropicFiltering, "Set the unity anisotropic filtering");
            GraphicAntiAliasing = Config.Bind("Graphics", "Anti-Aliasing", QualitySettings.antiAliasing, new ConfigDescription("Set the unity anti-aliasing", new AcceptableValueRange<int>(0, 8)));

            ConfigShowDeckName = Config.Bind("General", "Show Deck Names", true, "Show deck names instead of character names.");
            ConfigMaxCP = Config.Bind("General", "Max CP", 500, new ConfigDescription("Sets the maximum amount of CP a player may have stored.", new AcceptableValueRange<int>(100, 900)));
            ConfigHardTranslations = Config.Bind("General", "Load Hard Translations (*)", true, "Loads hardcoded translations from CCF_* files in the translation directory, plus some hardcoded strings.\n\n(*) Requires game restart");

            BattleEngineLockStepModifier = Config.Bind("Gameplay", "Combat Engine Tick Delay (A)(!)(*)", 1, new ConfigDescription("Changes the combat engine's tick delay. The higher this number, the longer the game will be synchronized for. Change this slowly upwards on higher FPS rates. Can also be modified seperately with no changes to FPS to slow down game speed.\n\n(A) Advanced setting\n(!) For online play, combat speed must match or the faster player will time out! Do not enter public lobbies with this setting changed!\n(*) Changes require game restart", new AcceptableValueRange<int>(1, 5), new ConfigurationManagerAttributes() {
                IsAdvanced = true
            }));

            GraphicShowSandglass.SettingChanged += UpdateGraphicSettings;
            GraphicShowVolumetricClouds.SettingChanged += UpdateGraphicSettings;
            GraphicShowLighting.SettingChanged += UpdateGraphicSettings;
            GraphicShowLighting2.SettingChanged += UpdateGraphicSettings;
            GraphicForceDisableVsync.SettingChanged += UpdateGraphicSettings;
            GraphicShowSimpleClouds.SettingChanged += UpdateGraphicSettings;
            GraphicShadowResolution.SettingChanged += UpdateGraphicSettings;
            GraphicShadowQuality.SettingChanged += UpdateGraphicSettings;
            GraphicAnisotropicFiltering.SettingChanged += UpdateGraphicSettings;
            GraphicAntiAliasing.SettingChanged += UpdateGraphicSettings;
            ConfigMaxCP.SettingChanged += UpdateCTConstants;
            #endregion

            Harmony h = new Harmony("eu.haruka.chrono.corefixes");
            h.PatchAll(typeof(AMDaemonPatches));
            h.PatchAll(typeof(DebugPatches));
            h.PatchAll(typeof(GameEnhancementPatches));
            h.PatchAll(typeof(GraphicPatches));
            h.PatchAll(typeof(MiscPatches));
            h.PatchAll(typeof(TerminalPatches));
            h.PatchAll(typeof(NetworkPatches));
            h.PatchAll(typeof(ServerExtensionPatches));
            h.PatchAll(typeof(TranslationPatches));
            h.PatchAll(typeof(ReconnectSystem));

            if (Environment.GetCommandLineArgs().Contains("-mod-chronocorefixes-force-terminal")) {
                WantedBootMode = GameBootMode.ForceTerminal;
            } else if (Environment.GetCommandLineArgs().Contains("-mod-chronocorefixes-force-satellite")) {
                WantedBootMode = GameBootMode.ForceSatellite;
            } else {
                WantedBootMode = GameBootMode.Auto;
            }


            FireMatchingSettingChanged();
            ApplyStringMods();
            UpdateCTConstants(null, null);
            ReconnectSystem.Reset();

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            if (GraphicFPS.Value != 30) {
                AccessTools.DeclaredField(typeof(CT), "FPS").SetValue(null, GraphicFPS.Value);
                QualitySettings.vSyncCount = 0;
            }
            Application.targetFrameRate = GraphicFPS.Value;
            if (BattleEngineLockStepModifier.Value != 1) {
                AccessTools.DeclaredField(typeof(CT), "TURN_STEP_SECOND").SetValue(null, BattleEngineLockStepModifier.Value);
            }

            if (CT.FPS * CT.TURN_STEP_SECOND != 30) {
                Logger.LogMessage("CAUTION: Combat engine update speed modified to " + (CT.FPS * CT.TURN_STEP_SECOND));
            }

            Logger.LogInfo("Plugin is loaded!");
        }

        private void ApplyStringMods() {
            if (ConfigHardTranslations.Value) {
                string basedir = "BepInEx/Translation/en/Text/";
                AccessTools.DeclaredField(typeof(CT), "COLOR_NAME").SetValue(null, File.ReadAllLines(basedir + "CCF_COLOR_NAME.txt"));
                AccessTools.DeclaredField(typeof(CT), "KEYWORD_STR").SetValue(null, File.ReadAllLines(basedir + "CCF_KEYWORD_STR.txt"));
                AccessTools.DeclaredField(typeof(CT), "KEYWORD_EFF_STR").SetValue(null, File.ReadAllLines(basedir + "CCF_KEYWORD_EFF_STR.txt"));
                AccessTools.DeclaredField(typeof(CT), "SUPPORT_ABILITY_STR").SetValue(null, File.ReadAllLines(basedir + "CCF_SUPPORT_ABILITY_STR.txt"));
                AccessTools.DeclaredField(typeof(CT), "SUPPORT_ABILITY_STR_EXE").SetValue(null, File.ReadAllLines(basedir + "CCF_SUPPORT_ABILITY_STR_EXE.txt"));
                AccessTools.DeclaredField(typeof(CT), "BAD_STATUS_STR").SetValue(null, File.ReadAllLines(basedir + "CCF_BAD_STATUS_STR.txt"));
                AccessTools.DeclaredField(typeof(CT), "BAD_STATUS_TEXT").SetValue(null, File.ReadAllLines(basedir + "CCF_BAD_STATUS_TEXT.txt"));

                AccessTools.DeclaredField(typeof(CT), "SP_SKILL_NAME").SetValue(null, "Regalia");
                AccessTools.DeclaredField(typeof(CT), "NEED_SPSKILL_COST").SetValue(null, "Activation Gauge");
                AccessTools.DeclaredField(typeof(CT), "MaximumOverText").SetValue(null, "Limit reached");
                AccessTools.DeclaredField(typeof(CT), "GameMoneyName").SetValue(null, "Sands of Time");

                // these break xunity
                //AccessTools.DeclaredField(typeof(CT), "CreditName").SetValue(null, "Credit");
                //AccessTools.DeclaredField(typeof(CT), "DotNetCoinName").SetValue(null, "Chrorega Coin");

                // these are consts
                //AccessTools.DeclaredField(typeof(CT), "CP_CONVERT_FORMAT").SetValue(null, "<br>※ Remaining CP are converted to {0}.");
                //AccessTools.DeclaredField(typeof(CT), "SCHEDULE_SHOP_NOTICE_TITLE").SetValue(null, "Sales are suspended.");

                Logger.LogDebug("CT hardcoded strings updated");
            }
        }

        private void UpdateGraphicSettings(object sender, EventArgs e) {
            Logger.LogDebug("UpdateGraphicsSettings");
            FindObjectAndSwitch("Stage/EnemyField/null_all/Sandglass", GraphicShowSandglass.Value);
            FindObjectAndSwitch("Stage/PlayerField/null_all/Sandglass", GraphicShowSandglass.Value);
            FindObjectAndSwitch("Stage/Lights/Light/Day", GraphicShowLighting.Value);
            FindObjectAndSwitch("Stage/Lights/Probe", GraphicShowLighting2.Value);
            FindObjectAndSwitch("Stage/Environment/bg_com_distantview00(Clone)/root", GraphicShowVolumetricClouds.Value);
            FindObjectAndSwitch("Stage/Environment/bg_com_distantview00(Clone)/base_cloud", GraphicShowSimpleClouds.Value);

            QualitySettings.vSyncCount = GraphicForceDisableVsync.Value ? 0 : 1;
            Log.LogDebug("VSync: " + QualitySettings.vSyncCount);
            QualitySettings.shadowResolution = GraphicShadowResolution.Value;
            QualitySettings.shadows = GraphicShadowQuality.Value;
            QualitySettings.anisotropicFiltering = GraphicAnisotropicFiltering.Value;
            QualitySettings.antiAliasing = GraphicAntiAliasing.Value;
        }

        private void UpdateCTConstants(object sender, EventArgs e) {
            AccessTools.DeclaredField(typeof(CT), "CP_MAX").SetValue(null, ConfigMaxCP.Value);
            Logger.LogDebug("CT constants updated");
        }

        private void FindObjectAndSwitch(string name, bool state) {
            var obj = GameObject.Find(name);
            if (obj != null) {
                obj.SetActive(state);
            } else {
                //Logger.LogWarning("Object " + name + " not (yet) found, can't change active status!");
            }
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1) {
            Logger.LogDebug("Scene: " + arg0.name);
            if (arg0.name == "SegaLogo") {
                Logger.LogMessage("Chrono Regalia Core Fixes " + GetSelfVersion() + " by Haruka");
                if (File.Exists("BepInEx/plugins/ConfigurationManager.dll")) {
                    Logger.LogMessage("Press F1 to open the Configuration Manager.");
                }
            }
            UpdateGraphicSettings(null, null);
        }

        private string GetSelfVersion() {
            return GetType().GetCustomAttributes(typeof(BepInPlugin), true).OfType<BepInPlugin>().FirstOrDefault().Version.ToString();
        }

        public void ConfigMatchingNetmask_SettingChanged(object sender, EventArgs e) {

            string mask = ConfigMatchingNetmask.Value;
            string[] maskparts = mask.Split('.');
            if (maskparts.Length != 4) {
                LocalNetworkIPOverride = null;
                Logger.LogWarning("IP override is invalid: malformed IP address");
                return;
            }
            if (maskparts[0] == "255") {
                LocalNetworkIPOverride = null;
                Logger.LogWarning("IP override is invalid: first octet must be != 255");
                return;
            }

            string checkstr = maskparts[0] + ".";
            if (maskparts[1] != "255") {
                checkstr += maskparts[1] + ".";
                if (maskparts[2] != "255") {
                    checkstr += maskparts[2] + ".";
                    if (maskparts[3] != "255") {
                        checkstr += maskparts[3];
                    }
                }
            }
            Logger.LogDebug("Finding adapter with IP: " + checkstr);


            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces()) {
                var ipp = ni.GetIPProperties();
                foreach (var uip in ipp.UnicastAddresses) {
                    var aip = uip.Address.ToString();
                    Logger.LogDebug("Found adapter: " + aip);
                    if (aip.StartsWith(checkstr)) {
                        LocalNetworkIPOverride = aip;
                        Logger.LogMessage("Network adapter for matching: " + LocalNetworkIPOverride);
                        return;
                    }
                }
            }
            LocalNetworkIPOverride = null;
            Logger.LogError("No adapter found with subnet: " + ConfigMatchingNetmask.Value);
        }

        private void Update() {
            if (ConfigShowMouse.Value && !Cursor.visible) {
                Cursor.visible = true;
            } else if (!ConfigShowMouse.Value && Cursor.visible) {
                Cursor.visible = false;
            }
        }

    }

}
