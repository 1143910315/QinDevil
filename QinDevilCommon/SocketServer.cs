using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;

namespace QinDevilCommon {
    public class SocketServer {
        public delegate bool OnAcceptEvent(Socket socket);
        public delegate void OnReceiveEvent(int id, byte[] buffer);
        public delegate void OnLeaveEvent(int id);
        private Socket socket;
        private SocketList socketList = new SocketList();
        public SocketServer(int port, OnAcceptEvent onAcceptEvent = null, OnReceiveEvent onReceiveEvent = null, OnLeaveEvent onLeaveEvent = null) {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            socket.Listen(10);
            new Task(() => {
                while (socket != null) {
                    Socket acceptSocket = socket.Accept();
                    bool? b = onAcceptEvent?.Invoke(acceptSocket);
                    if (b.HasValue && b.Value == false) {
                        acceptSocket.Close();
                    } else {
                        new Task((id) => {
                            Socket thisSocket = socketList.Get(id);
                            byte[] buffer = new byte[512];
                            while (true) {
                                try {
                                    int len = thisSocket.Receive(buffer);
                                    if (thisSocket.Connected && len > 0) {
                                        if (socketList.AddData(id, buffer, 0, len)) {
                                            onReceiveEvent?.Invoke((int)id, socketList.GetDataAndDelete(id));
                                        }
                                    } else {
                                        socketList.Delete(id);
                                        onLeaveEvent((int)id);
                                        break;
                                    }
                                } catch (Exception) {
                                    socketList.Delete(id);
                                    onLeaveEvent((int)id);
                                    break;
                                }

                            }
                        }, socketList.Add(acceptSocket)).Start();
                    }

                }
            }).Start();
            //new Task(() => {
            //    while (socket != null) {
            //        IDictionaryEnumerator enumerator = socketList.GetAll();
            //        while (enumerator.MoveNext()) {
            //            try {
            //                Socket thisSocket = enumerator.Value as Socket;
            //                byte[] buffer = new byte[512];
            //                int len = thisSocket.Receive(buffer);
            //                if (thisSocket.Connected && len > 0) {
            //                    if (socketList.AddData(enumerator.Key, buffer, 0, len)) {
            //                        onReceiveEvent?.Invoke((int)enumerator.Key, socketList.GetDataAndDelete(enumerator.Key));
            //                    }
            //                } else {
            //                    socketList.Delete(enumerator.Key);
            //                    onLeaveEvent((int)enumerator.Key);
            //                }
            //            } catch (Exception) {
            //                socketList.Delete(enumerator.Key);
            //                onLeaveEvent((int)enumerator.Key);
            //            }

            //        }
            //        Thread.Sleep(0);
            //    }
            //}).Start();
        }
        ~SocketServer() {
            socket.Close();
            socket = null;
        }
    }
}
