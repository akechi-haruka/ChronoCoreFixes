using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace ChronoCoreFixes.Patches {

    // The reconnect system.

    internal class ReconnectSystem {

        private static bool InReconnectState;
        private static int ReconnectCount;
        private static readonly List<byte[]> ReconnectOnResumptionNetworkPackets = new List<byte[]>();

        public static bool IsReconnecting() {
            return InReconnectState;
        }

        public static void Reset() {
            InReconnectState = false;
            ReconnectCount = 0;
            ReconnectOnResumptionNetworkPackets.Clear();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetBtlMgr), "IsBattleStop")]
        static bool IsBattleStop(NetBtlMgr __instance, ref bool __result) {
            __result = (DateTime.Now - __instance.m_UDP.m_ticker).TotalSeconds > Plugin.ConfigNetworkTimeout.Value || (DateTime.Now - __instance.m_ticker).TotalSeconds > Plugin.ConfigNetworkTimeout.Value;
            //Plugin.Log.LogDebug("Ticker1: " + (DateTime.Now - __instance.m_ticker).TotalSeconds);
            //Plugin.Log.LogDebug("Ticker2: " + (DateTime.Now - __instance.m_UDP.m_ticker).TotalSeconds);
            //Plugin.Log.LogDebug("Queues: " + __instance.m_UDP.m_sendQueue.m_offsetList.Count + "," + __instance.m_UDP.m_recvQueue.m_offsetList.Count);
            if (__result) {
                Plugin.Log.LogInfo("Timeout detected");
                if (TriggerReconnect()) {
                    __instance.m_ticker = DateTime.Now;
                    __result = false;
                } else {
                    __result = false;
                }
            }
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetBtlMgr), "OnEventHandling")]
        static bool OnEventHandling(NetEventState state) {
            if (state.type != NetEventType.Connect && InReconnectState) {
                Plugin.Log.LogDebug("Blocking event " + state.type + ", due to being in reconnection state");
                return false;
            }
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TransportTCP), "Create")]
        static bool Create(TransportTCP __instance, ref bool __result, string address, int port) {
            Plugin.Log.LogInfo("Connecting to matching server at " + address + ":" + port);
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TransportUDP), "IsConnected")]
        static bool IsConnected(TransportUDP __instance, ref bool __result) {
            __result = __instance.m_isConnected || InReconnectState;
            return false;
        }

        private static bool TriggerReconnect(bool terminateInstantlyAndDelay = false) {
            if (!InReconnectState && Plugin.ConfigAutoReconnect.Value) {
                Plugin.Log.LogInfo("TriggerReconnect");
                if (++ReconnectCount <= Plugin.ConfigAutoReconnectRetries.Value) {
                    Plugin.Log.LogInfo("Auto-Reconnect started");
                    InReconnectState = true;
                    if (terminateInstantlyAndDelay) {
                        TransportUDP udp = NetworkManager.Instance.m_BtlMgr.m_UDP;
                        udp.m_thread = null;
                        udp.Disconnect();
                        udp.StopServer();
                        GlobalUIManager.ShowDialog("Reconnection", "The connection has been interrupted.\nPlease wait for your opponent to re-synchronize...\nThis may take up to 30 seconds.", TMPro.TextAlignmentOptions.Center, "Cancel", CancelReconnect, Plugin.ConfigNetworkTimeout.Value + 5F, DoReconnect, 2F);
                    } else {
                        GlobalUIManager.ShowDialog("Reconnection", "The connection has been interrupted.\nAttempting to reconnect... (" + ReconnectCount + "/" + Plugin.ConfigAutoReconnectRetries.Value + ")", TMPro.TextAlignmentOptions.Center, "Cancel", CancelReconnect, 20F, null, 2F);
                        DoReconnect();
                    }
                    return true;
                } else {
                    Plugin.Log.LogError("Maximum Auto-Reconnects exceeded");
                    return false;
                }
            }
            return false;
        }

        private static void DoReconnect() {
            Plugin.Log.LogInfo("Reconnect started");
            new Thread(DoReconnectT).Start();
        }

        private static void CancelReconnect() {
            InReconnectState = false;
            NetworkManager.Instance.m_BtlMgr.m_UDP.Disconnect();
        }

        private static void DoReconnectT() {
            Plugin.Log.LogInfo("Reconnect thread started");
            TransportUDP udp = NetworkManager.Instance.m_BtlMgr.m_UDP;
            udp.Disconnect();
            udp.StopServer();
            Thread.Sleep(1000);
            udp.StartServer(NetworkManager.Instance.ReflectorServerPort);
            udp.Connect(NetworkManager.Instance.ReflectorServerAddress, NetworkManager.Instance.ReflectorServerPort);
            Plugin.Log.LogInfo("Reconnect executed, connected=" + udp.m_isConnected + ",started=" + udp.m_isStarted);
            udp.m_ticker = DateTime.Now;
            while (!udp.m_isConnected && udp.m_isStarted && (DateTime.Now - udp.m_ticker).TotalSeconds < 20 && InReconnectState) {
                Thread.Sleep(1000);
            }
            if (!InReconnectState) {
                return;
            }
            Plugin.Log.LogInfo("Reconnect finished, connected=" + udp.m_isConnected + ",started=" + udp.m_isStarted);
            udp.m_ticker = DateTime.Now;
            if (!udp.m_isConnected) {
                Plugin.Log.LogWarning("Not connected, retry");
                udp.m_isConnected = true; // prevent disconnect
                udp.m_ticker = new DateTime(0);
            } else {
                GlobalUIManager.ShowDialog("Reconnection", "Reconnected successfully.\nResuming gameplay...", TMPro.TextAlignmentOptions.Center, 2F, null);
                Thread.Sleep(2000);
                Plugin.Log.LogInfo("Re-sending " + ReconnectOnResumptionNetworkPackets.Count + " packets");
                object sendQueue = udp.m_sendQueue;
                lock (sendQueue) {
                    foreach (byte[] arr in ReconnectOnResumptionNetworkPackets) {
                        udp.m_sendQueue.Enqueue(arr, arr.Length);
                    }
                }
                NetworkManager.Instance.m_BtlMgr.m_AddInputDataFlag[(int)NetworkManager.Instance.m_BtlMgr.m_HostType] = true;
            }
            InReconnectState = false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TransportUDP), "DispatchSend")]
        private static bool DispatchSend(TransportUDP __instance) {
            if (__instance.m_socket == null) {
                return false;
            }
            try {
                if (__instance.m_socket.Poll(0, SelectMode.SelectWrite)) {
                    byte[] array = new byte[__instance.m_packetSize];
                    object sendQueue = __instance.m_sendQueue;
                    lock (sendQueue) {
                        try {
                            int num = 0;
                            int i = __instance.m_sendQueue.Dequeue(ref array, array.Length);
                            while (i > 0) {
                                int num2 = __instance.m_socket.Send(array, 0, i, SocketFlags.None, out SocketError socketError);
                                i = __instance.m_sendQueue.Dequeue(ref array, array.Length);
                                if (socketError != SocketError.Success) {
                                    Debug.Log("[UDP]TransportUdp::DispatchSend Send Error (Corefixes)");
                                    if (!InReconnectState) {
                                        Plugin.Log.LogWarning("DispatchSend failed, forcing reconnect");
                                        ReconnectOnResumptionNetworkPackets.Add(array);
                                        while (__instance.m_sendQueue.Dequeue(ref array, array.Length) > 0) {
                                            ReconnectOnResumptionNetworkPackets.Add(array);
                                        }
                                        if (!TriggerReconnect(true)) {
                                            __instance.m_ticker = new DateTime(0); // force disconnect
                                        }
                                    }
                                    break;
                                }
                                num++;
                                if (num > CT.NET_LIMIT_LOOP_COUNT) {
                                    break;
                                }
                            }
                        } catch (Exception ex) {
                            Debug.Log("DispatchSend m_sendQueue");
                            Plugin.Log.LogError("DispatchSend exception: " + ex);
                        }
                    }
                }
            } catch (Exception ex2) {
                Debug.Log(string.Concat(new object[]
                {
                "[UDP]DispatchSend Error ",
                ex2,
                " -- ",
                DateTime.Now
                }));
            }
            return false;
        }



        [HarmonyPrefix, HarmonyPatch(typeof(Battle), "CheckNotEndEvent")]
        static bool CheckNotEndEvent(Battle __instance, ref bool __result) {
            for (int i = 0; i < __instance.m_ExeEventParam.Length; i++) {
                if (!__instance.m_ExeEventParam[i].IsEmpty() && !__instance.m_ExeEventParam[i].m_ExeHostType[(int)__instance.m_HostType] && __instance.m_ExeEventParam[i].m_ExeHostType[(int)__instance.m_HostTypeEnemy] && __instance.m_BtlFrame - __instance.m_ExeEventParam[i].m_EndFrame > 600) {
                    Plugin.Log.LogError("CheckNotEndEvent triggered: " + (__instance.m_BtlFrame - __instance.m_ExeEventParam[i].m_EndFrame));
                    Plugin.Log.LogMessage("Desynchronization detected ("+ (__instance.m_BtlFrame - __instance.m_ExeEventParam[i].m_EndFrame) + " frames). This is not salvageable.");
                    Plugin.Log.LogMessage("Make sure you are using an up-to-date CoreFixes version.");
                    Plugin.Log.LogMessage("Should this occur repeatedly, set FPS to 30 and tick delay to 1.");
                }
            }
            return true;
        }
    }
}