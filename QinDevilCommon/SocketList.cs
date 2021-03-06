﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace QinDevilCommon {
    internal class SocketList {
        private int connectNum = 1;
        private readonly Hashtable hashtable = new Hashtable();
        private readonly Hashtable data = new Hashtable();
        internal int Add(Socket socket) {
            hashtable.Add(connectNum, socket);
            data.Add(connectNum, new List<byte>());
            return connectNum++;
        }
        internal bool AddData(object id, byte[] buffer, int offest, int length) {
            if (data[id] is List<byte>) {
                List<byte> dataList = data[id] as List<byte>;
                for (int j = offest; j < length; j++) {
                    dataList.Add(buffer[j]);
                }
                if (dataList.Count >= 4) {
                    int v = BitConverter.ToInt32(dataList.GetRange(0, 4).ToArray(), 0);
                    if (dataList.Count - 4 >= v) {
                        return true;
                    }
                }
            }
            return false;
        }
        internal Socket Get(object id) {
            return hashtable[id] as Socket;
        }
        internal IDictionaryEnumerator GetAll() {
            return hashtable.GetEnumerator();
        }
        internal void Delete(object id) {
            hashtable.Remove(id);
            data.Remove(id);
            Debug.WriteLine("delete " + id.ToString());
        }
        internal (int, byte[]) GetDataAndDelete(object id) {
            byte[] buffer = null;
            int i = 0;
            if (data[id] is List<byte>) {
                List<byte> dataList = data[id] as List<byte>;
                if (dataList.Count >= 8) {
                    byte[] v1 = dataList.GetRange(0, 8).ToArray();
                    int v = BitConverter.ToInt32(v1, 0);
                    if (dataList.Count - 4 >= v) {
                        i = BitConverter.ToInt32(v1, 4);
                        buffer = dataList.GetRange(8, v - 4).ToArray();
                        dataList.RemoveRange(0, v + 4);
                    }
                }
            }
            return (i, buffer);
        }
    }
}
