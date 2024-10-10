namespace ChronoCoreFixes.Patches {

    // No longer needed, were mostly logging statements from figuring out things
    internal class DebugPatches {

        /*[HarmonyPrefix, HarmonyPatch(typeof(NetMatchingMgr), "ExecuteWaitMatching")]
        static bool ExecuteWaitMatching(NetMatchingMgr __instance) {

            if (__instance.m_TCP.IsConnected()) {
                byte[] array = new byte[CT.NET_RECV_MATCHING_DATA_SIZE];
                if (__instance.m_TCP.Receive(ref array, array.Length) > 0) {
                    string @string = Encoding.UTF8.GetString(array);
                    LoggerGenerics<HKBDebug.Network>.Log("Received network [" + array.Length +"/"+@string.Length+"] :" + @string);
                    if (@string.Contains("KeepAlive")) {
                        return false;
                    }
                    __instance.response = JsonUtility.FromJson<ResponseMatchingData>(@string);
                    SingletonMonoBehaviour<NetworkManager>.Instance.Channel = __instance.response.channel;
                    SingletonMonoBehaviour<NetworkManager>.Instance.ReflectorServerAddress = __instance.response.host;
                    SingletonMonoBehaviour<NetworkManager>.Instance.ReflectorServerPort = __instance.response.port;
                    __instance.LeaveMatching();
                    string message = string.Format("マッチング成立:チャンネル:{0}", SingletonMonoBehaviour<NetworkManager>.Instance.Channel);
                    LoggerGenerics<HKBDebug.Network>.Log(message);
                    string[] array2 = @string.Split(new char[]
                    {
                ','
                    });
                    for (int i = 0; i < array2.Length; i++) {
                        LoggerGenerics<HKBDebug.Network>.Log(" " + array2[i]);
                    }
                    __instance.GoNext(NetMatchingMgr.State.SyncUserData);
                }
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetBtlMgr), "ReceiveInputData")]
        static bool ReceiveInputData(ref bool __result, NetBtlMgr __instance) {
            LoggerGenerics<HKBDebug.Network>.Log("[NetBtlMgr] --- START RECEIVEINPUTDATA ---");
            bool result = false;
            byte[] array = new byte[CT.NET_RECV_BATTLE_DATA_SIZE];
            int num = 0;
            while (!(__instance.m_UDP == null)) {
                int num2 = __instance.m_UDP.Receive(ref array, array.Length);
                LoggerGenerics<HKBDebug.Network>.Log("[NetBtlMgr] --- UDP Receive: " + num2 + "/" + array.Length);
                if (num2 <= 0) {
                    __result = result;
                    return false;
                }
                __instance.m_ticker = DateTime.Now;
                if (PacketCommandData.CheckCmd(PacketCommandData.CmdType.ChangeCard, array)) {
                    LoggerGenerics<HKBDebug.Network>.Log("[NetBtlMgr] --- CheckCmd = ChangeCard");
                    result = true;
                    int enemyHostType = (int)Util.GetEnemyHostType(__instance.m_HostType);
                    if (__instance.m_ChangedCardInfo[enemyHostType].changeData.IsEmpty()) {
                        __instance.m_ChangedCardInfo[enemyHostType].DeserializeSelf(array);
                    }
                } else if (PacketCommandData.CheckCmd(PacketCommandData.CmdType.Battle, array)) {
                    LoggerGenerics<HKBDebug.Network>.Log("[NetBtlMgr] --- CheckCmd = Battle");
                    result = true;
                    __instance.m_RcvData.battleData.Clear();
                    if (!__instance.m_RcvData.DeserializeSelf(array)) {
                        result = false;
                        LoggerGenerics<HKBDebug.Network>.Error("[NetBtlMger] ReceiveInputData() Invalid Recive Data : " + DateTime.Now);
                    }
                    CT.HOSTTYPE enemyHostType2 = Util.GetEnemyHostType(__instance.m_HostType);
                    __instance.AddBtlData(enemyHostType2, __instance.m_RcvData);
                    DebugLog.WritePacketLog(__instance.m_RcvData);
                } else {
                    LoggerGenerics<HKBDebug.Network>.Log("[NetBtlMgr] --- CheckCmd = Unknown");
                }
                num++;
                if (num > CT.NET_LIMIT_LOOP_COUNT) {
                    LoggerGenerics<HKBDebug.Network>.Log("[NetBtlMger] ReceiveInputData() Over Loop Count : " + DateTime.Now);
                    __result = result;
                    return false;
                }
            }
            __result = result;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetMatchingMgr), "RecvPacket")]
        static bool RecvPacket(ref bool __result, NetMatchingMgr __instance) {
            LoggerGenerics<HKBDebug.Network>.Log("[NetMatchingMgr] --- RecvPacket = START");
            bool result = false;
                byte[] array = new byte[CT.NET_RECV_USER_DATA_SIZE];
                for (; ; )
                {
                    int num = __instance.m_UDP.Receive(ref array, array.Length);
                LoggerGenerics<HKBDebug.Network>.Log("[NetMatchingMgr] --- Received: " + num+"/"+array.Length);
                if (num <= 0) {
                        break;
                    }
                LoggerGenerics<HKBDebug.Network>.Log("[NetMatchingMgr] --- m_RcvEnemyRequest = " + __instance.m_RcvEnemyRequest);
                byte[] array2 = new byte[PacketCommandData.SIZE];
                Buffer.BlockCopy(array, PacketCommandData.OFFSET, array2, 0, PacketCommandData.SIZE);
                LoggerGenerics<HKBDebug.Network>.Log("[NetMatchingMgr] --- CheckCmd Matching: " + PacketCommandData.CheckCmd(PacketCommandData.CmdType.Matching, array)+", got:" + array2[0] + "," + array2[1] + "," + array2[2] + "," + array2[3]);
                if (!__instance.m_RcvEnemyRequest && PacketCommandData.CheckCmd(PacketCommandData.CmdType.Matching, array)) {
                    LoggerGenerics<HKBDebug.Network>.Log("[NetMatchingMgr] --- CheckCmd Matching OK");
                    result = true;
                    __instance.m_TempMatchingInfo.serializer.Clear();
                    __instance.m_TempMatchingInfo.DeserializeSelf(array);
                        if (!__instance.m_RcvEnemyRequest) {
                        __instance.m_TempMatchingInfo.userData.CopyTo(ref __instance.m_MatchingInfo_Rcv.userData);
                        }
                        TemporaryDataManager.BattleInfo battleInfo = TemporaryDataManager.GetBattleInfo();
                        if (battleInfo != null) {
                            battleInfo.m_RandomSeed[1] = __instance.m_MatchingInfo_Rcv.userData.m_RandomSeed;
                        }
                    __instance.m_Counter = 0;
                    __instance.m_RcvEnemyRequest = true;
                    }
                }
            __result = result;
                return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NetMatchingMgr), "ExecuteSyncUserData")]
        static bool ExecuteSyncUserData(NetMatchingMgr __instance) {
            bool flag = true;
            flag &= __instance.SendUserData();
            LoggerGenerics<HKBDebug.Network>.Log("SendUserData: " + flag + "(" + __instance.IsConnected() + ", " + (PlayerParamMgr.GetParamBattle(CT.SIDE.PLAYER).m_PartyParam!=null) + ")");
            __instance.RecvPacket();
            flag &= __instance.RecvUserData();
            LoggerGenerics<HKBDebug.Network>.Log("RecvUserData: " + flag + "(" + __instance.m_RcvEnemyRequest + ")");
            if (flag) {
                LoggerGenerics<HKBDebug.Network>.Log("ユーザデータ同期完了");
                __instance.GoNext(NetMatchingMgr.State.SendMatchingInfo);
            }
            return false;
        }*/

    }
}
