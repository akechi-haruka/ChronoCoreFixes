using AMDaemon;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ChronoCoreFixes.Patches {
    internal class AMDaemonPatches {
        
        // Now AKSHUALLY we could just inject minihook into the unity process for the whole path redirection shenanigans but nah fuck that
        [HarmonyPrefix, HarmonyPatch(typeof(AMDaemon.System), "AppRootPath", MethodType.Getter)]
        static bool AppRootPath(ref string __result) {
            if (Plugin.ConfigPathFixes.Value) {
                if (Plugin.YDrivePath == null) {
                    IniFile segatools = new IniFile("segatools.ini");
                    string appdata = segatools.Read("appdata", "vfs");
                    if (!String.IsNullOrEmpty(appdata)) {
                        Plugin.YDrivePath = appdata + "\\";
                    } else {
                        Plugin.YDrivePath = ".\\";
                    }
                    Plugin.Log.LogDebug("AppRootPath initialized to: " + Plugin.YDrivePath);
                }
                __result = Plugin.YDrivePath;
                return false;
            } else {
                return true;
            }
        }

        // same here
        [HarmonyPrefix, HarmonyPatch(typeof(AMDaemon.AppImage), "OptionMountRootPath", MethodType.Getter)]
        static bool OptionMountRootPath(ref string __result) {
            if (Plugin.ConfigPathFixes.Value) {
                if (Plugin.OptionPath == null) {
                    IniFile segatools = new IniFile("segatools.ini");
                    string option = segatools.Read("option", "vfs");
                    if (!String.IsNullOrEmpty(option)) {
                        Plugin.OptionPath = option + "\\";
                    } else {
                        Plugin.OptionPath = ".\\option\\";
                    }
                    Plugin.Log.LogDebug("OptionMountRootPath initialized to: " + Plugin.OptionPath);
                    if (Directory.Exists(AppImage.OptionMountRootPath)) {
                        foreach (string dirname in Directory.GetDirectories(AppImage.OptionMountRootPath).Reverse()) {
                            Plugin.Log.LogDebug("Available Option data: " + dirname);
                        }
                    }
                }
                __result = Plugin.OptionPath;
                return false;
            } else {
                return true;
            }
        }

        // the game natively reads multiple options in the wrong order (it stops at the FIRST found option rather than the MOST RECENT option), so let's just reimplement this. Did the game never have more than one option per version??
        [HarmonyPrefix, HarmonyPatch(typeof(Util), "GetOptionFilePath")]
        public static bool GetOptionFilePath(ref string __result, string file_name) {
            __result = null;
            if (Directory.Exists(AppImage.OptionMountRootPath)) {
                foreach (string dirname in Directory.GetDirectories(AppImage.OptionMountRootPath).Reverse()) {
                    string text = string.Format("{0}\\{1}", dirname, file_name);
                    if (File.Exists(text)) {
                        __result = text;
                        break;
                    }
                }
            }
            return false;
        }

        // https://stackoverflow.com/a/14906422
        public class IniFile   // revision 11
        {
            private readonly string Path;
            private readonly string EXE = Assembly.GetExecutingAssembly().GetName().Name;

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

            [DllImport("kernel32", CharSet = CharSet.Unicode)]
            static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

            public IniFile(string IniPath = null) {
                Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
            }

            public string Read(string Key, string Section = null) {
                var RetVal = new StringBuilder(255);
                GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
                return RetVal.ToString();
            }

            public void Write(string Key, string Value, string Section = null) {
                WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
            }

            public void DeleteKey(string Key, string Section = null) {
                Write(Key, null, Section ?? EXE);
            }

            public void DeleteSection(string Section = null) {
                Write(null, null, Section ?? EXE);
            }

            public bool KeyExists(string Key, string Section = null) {
                return Read(Key, Section).Length > 0;
            }
        }

    }
}
