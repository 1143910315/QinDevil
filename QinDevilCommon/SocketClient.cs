using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace QinDevilCommon {
    public class SocketClient {
        private Socket socket;
        private readonly byte[] buffer = new byte[512];
        private readonly List<byte> list = new List<byte>();
        private readonly List<byte> sendData = new List<byte>();
        public delegate void OnConnectedEvent(bool connected);
        public delegate void OnReceivePackageEvent(int signal, byte[] buffer);
        public delegate void OnConnectionBreakEvent();
        public delegate void OnSendCompletedEvent();
        public OnConnectedEvent onConnectedEvent;
        public OnConnectionBreakEvent onConnectionBreakEvent;
        public OnReceivePackageEvent onReceivePackageEvent;
        public OnSendCompletedEvent onSendCompletedEvent;
        private SocketAsyncEventArgs SendAsyncEventArgs = null;
        private bool socketIsOnline = false;
        private readonly object sendLock = new object();
        public SocketClient() {
        }
        public void Connect(string host, int port) {
            IPHostEntry entry = Dns.GetHostEntry(host);
            if (entry != null && entry.AddressList != null) {
                for (int AddressListIndex = 0; AddressListIndex < entry.AddressList.Length; AddressListIndex++) {
                    if (entry.AddressList[AddressListIndex].AddressFamily == AddressFamily.InterNetwork) {
                        socketIsOnline = false;
                        socket?.Close();
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs {
                            UserToken = this,
                            RemoteEndPoint = new IPEndPoint(entry.AddressList[AddressListIndex], port)
                        };
                        connectEventArgs.Completed += (t, e) => {
                            SocketClient thisclass = e.UserToken as SocketClient;
                            if (t.Equals(thisclass.socket)) {
                                Socket s = t as Socket;
                                onConnectedEvent?.Invoke(s.Connected);
                                if (s.Connected) {
                                    socketIsOnline = true;
                                    SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs {
                                        UserToken = e.UserToken
                                    };
                                    receiveEventArgs.SetBuffer(buffer, 0, 512);
                                    receiveEventArgs.Completed += (t1, e1) => {
                                        SocketClient thisclass1 = e1.UserToken as SocketClient;
                                        if (t1.Equals(thisclass1.socket)) {
                                            int len = e1.BytesTransferred;
                                            if (len > 0 && e1.SocketError == SocketError.Success) {
                                                for (int i = 0; i < len; i++) {
                                                    list.Add(buffer[i]);
                                                }
                                                while (list.Count >= 8) {
                                                    byte[] v1 = list.GetRange(0, 8).ToArray();
                                                    int v = BitConverter.ToInt32(v1, 0);
                                                    if (list.Count - 4 >= v) {
                                                        onReceivePackageEvent?.Invoke(BitConverter.ToInt32(v1, 4), list.GetRange(8, v - 4).ToArray());
                                                        list.RemoveRange(0, v + 4);
                                                    } else {
                                                        break;
                                                    }
                                                }
                                                ((Socket)t1).ReceiveAsync(receiveEventArgs);
                                            } else {
                                                socketIsOnline = false;
                                                onConnectionBreakEvent?.Invoke();
                                                thisclass1.socket.Close();
                                            }
                                        }
                                    };
                                    socket.ReceiveAsync(receiveEventArgs);
                                } else {
                                    socketIsOnline = false;
                                }
                            }
                        };
                        socket.ConnectAsync(connectEventArgs);
                        break;
                    }
                }
            }
        }
        public void SendPackage(int signal, byte[] data) {
            if (data != null) {
                SendPackage(signal, data, 0, data.Length);
            } else {
                SendPackage(signal, null, 0, 0);
            }
        }
        public void SendPackage(int signal, byte[] data, int offset, int count) {
            byte[] v = BitConverter.GetBytes(signal);
            byte[] l = BitConverter.GetBytes(count + v.Length);
            Monitor.Enter(sendLock);
            if (SendAsyncEventArgs == null) {
                Monitor.Exit(sendLock);
                byte[] package = new byte[count + v.Length + l.Length];
                int index = 0;
                for (int i = 0; i < l.Length; i++) {
                    package[index++] = l[i];
                }
                for (int i = 0; i < v.Length; i++) {
                    package[index++] = v[i];
                }
                for (int i = offset; i < count; i++) {
                    package[index++] = data[i];
                }
                SendAsyncEventArgs = new SocketAsyncEventArgs();
                SendAsyncEventArgs.SetBuffer(package, 0, index);
                SendAsyncEventArgs.Completed += (s, e) => {
                    if (s.Equals(socket)) {
                        if (e.SocketError == SocketError.Success) {
                            Monitor.Enter(sendLock);
                            int len = sendData.Count;
                            if (len > 0) {
                                byte[] temp = sendData.ToArray();
                                sendData.Clear();
                                SendAsyncEventArgs.SetBuffer(temp, 0, len);
                                if (socketIsOnline) {
                                    socket.SendAsync(SendAsyncEventArgs);
                                }
                            } else {
                                onSendCompletedEvent?.Invoke();
                            }
                            Monitor.Exit(sendLock);
                        } else {
                            onConnectionBreakEvent?.Invoke();
                            socket.Close();
                        }
                    }
                    SendAsyncEventArgs = null;
                };
                if (socketIsOnline) {
                    socket.SendAsync(SendAsyncEventArgs);
                }
            } else {
                for (int i = 0; i < l.Length; i++) {
                    sendData.Add(l[i]);
                }
                for (int i = 0; i < v.Length; i++) {
                    sendData.Add(v[i]);
                }
                for (int i = offset; i < count; i++) {
                    sendData.Add(data[i]);
                }
                Monitor.Exit(sendLock);
            }
        }
        ~SocketClient() {
            socket.Close();
            socket = null;
        }
    }
}
