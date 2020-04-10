using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QinDevilCommon {
    public class SocketClient {
        private Socket socket;
        private byte[] buffer = new byte[512];
        private readonly List<byte> list = new List<byte>();
        private readonly List<byte> sendData = new List<byte>();
        public delegate bool OnConnectedEvent(bool connected);
        public delegate void OnReceivePackageEvent(int signal, byte[] buffer);
        public delegate void OnConnectionBreakEvent();
        public OnConnectedEvent onConnectedEvent;
        public OnConnectionBreakEvent onConnectionBreakEvent;
        public OnReceivePackageEvent onReceivePackageEvent;
        private SocketAsyncEventArgs SendAsyncEventArgs = null;
        public SocketClient() {
        }
        public void Connect(string host, int port) {
            IPHostEntry entry = Dns.GetHostEntry(host);
            if (entry != null && entry.AddressList != null && entry.AddressList.Length > 0) {
                socket?.Close();
                socket = new Socket(entry.AddressList[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs {
                    UserToken = this,
                    RemoteEndPoint = new IPEndPoint(entry.AddressList[0], port)
                };
                connectEventArgs.Completed += (t, e) => {
                    SocketClient thisclass = e.UserToken as SocketClient;
                    if (t.Equals(thisclass.socket)) {
                        Socket s = t as Socket;
                        onConnectedEvent?.Invoke(s.Connected);
                        if (s.Connected) {
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
                                        if (list.Count >= 8) {
                                            byte[] v1 = list.GetRange(0, 4).ToArray();
                                            int v = BitConverter.ToInt32(v1, 0);
                                            if (list.Count - 4 >= v) {
                                                onReceivePackageEvent?.Invoke(BitConverter.ToInt32(v1, 0), list.GetRange(8, v - 4).ToArray());
                                                list.RemoveRange(0, v + 4);
                                            }
                                        }
                                        ((Socket)t1).ReceiveAsync(receiveEventArgs);
                                    } else {
                                        onConnectionBreakEvent?.Invoke();
                                        thisclass1.socket.Close();
                                    }
                                }
                            };
                            socket.ReceiveAsync(receiveEventArgs);
                        }
                    }
                };
                socket.ConnectAsync(connectEventArgs);
            }
        }
        /*public void Send(byte [] data) {

        }*/
        public void SendPackage(int signal, byte[] data, int offset, int count) {
            byte[] v = BitConverter.GetBytes(signal);
            byte[] l = BitConverter.GetBytes(count + v.Length);
            if (SendAsyncEventArgs == null) {
                byte[] package = new byte[count + v.Length+l.Length];
                int index = 0;
                for (int i=0; i < l.Length; i++) {
                    package[index++] = l[i];
                }
                for (int i=0; i < v.Length; i++) {
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
                            int len = sendData.Count;
                            if (len > 0) {
                                byte[] temp = sendData.GetRange(0, len).ToArray();
                                SendAsyncEventArgs.SetBuffer(temp, 0, len);
                                socket.SendAsync(SendAsyncEventArgs);
                                return;
                            }
                        } else {
                            onConnectionBreakEvent?.Invoke();
                            socket.Close();
                        }
                    }
                    SendAsyncEventArgs = null;
                };
                socket.SendAsync(SendAsyncEventArgs);
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
            }
        }
        ~SocketClient() {
            socket.Close();
            socket = null;
        }
    }
}
