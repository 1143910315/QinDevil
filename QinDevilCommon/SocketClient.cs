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
        public delegate void OnConnected(bool connected);
        public delegate void OnReceivePackage(int signal, byte[] buffer);
        public delegate void OnConnectionBreak(string reason);
        public event OnConnected OnConnectedEvent;
        public event OnReceivePackage OnReceivePackageEvent;
        public event OnConnectionBreak OnConnectionBreakEvent;
        private Socket socket;
        private readonly object socketLock = new object();
        private readonly List<byte> recvData = new List<byte>();
        public SocketClient() {
            if (!Socket.OSSupportsIPv4) {
                throw new NotSupportedException("系统不支持IPv4网络！");
            }

        }
        public void Connect(string host, int port, bool i) {
            Socket tempSocket;
            lock (socketLock) {
                socket?.Close();
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                tempSocket = socket;
            }
            SocketAsyncEventArgs receiveEventArgs;
            receiveEventArgs = new SocketAsyncEventArgs();
            byte[] recvBuffer = new byte[255];
            receiveEventArgs.SetBuffer(recvBuffer, 0, recvBuffer.Length);
            receiveEventArgs.Completed += ReceiveEventArgs_Completed;
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
            connectEventArgs.Completed += ConnectEventArgs_Completed;
            connectEventArgs.UserToken = receiveEventArgs;
            connectEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            if (!tempSocket.ConnectAsync(connectEventArgs)) {
                ConnectEventArgs_Completed(tempSocket, connectEventArgs);
            }
        }
        public void Connect(string host, int port) {
            IPHostEntry entry = Dns.GetHostEntry(host);
            if (entry != null && entry.AddressList != null) {
                for (int AddressListIndex = 0; AddressListIndex < entry.AddressList.Length; AddressListIndex++) {
                    if (entry.AddressList[AddressListIndex].AddressFamily == AddressFamily.InterNetwork) {
                        Socket tempSocket;
                        lock (socketLock) {
                            socket?.Close();
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            tempSocket = socket;
                        }
                        SocketAsyncEventArgs receiveEventArgs;
                        receiveEventArgs = new SocketAsyncEventArgs();
                        byte[] recvBuffer = new byte[255];
                        receiveEventArgs.SetBuffer(recvBuffer, 0, recvBuffer.Length);
                        receiveEventArgs.Completed += ReceiveEventArgs_Completed;
                        SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
                        connectEventArgs.Completed += ConnectEventArgs_Completed;
                        connectEventArgs.UserToken = receiveEventArgs;
                        connectEventArgs.RemoteEndPoint = new IPEndPoint(entry.AddressList[AddressListIndex], port);
                        if (!tempSocket.ConnectAsync(connectEventArgs)) {
                            ConnectEventArgs_Completed(tempSocket, connectEventArgs);
                        }
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
            byte[] sendBuffer = new byte[count + 8];
            int len = count + 4;
            sendBuffer[0] = (byte)len;
            sendBuffer[1] = (byte)(len >> 8);
            sendBuffer[2] = (byte)(len >> 16);
            sendBuffer[3] = (byte)(len >> 24);
            sendBuffer[4] = (byte)signal;
            sendBuffer[5] = (byte)(signal >> 8);
            sendBuffer[6] = (byte)(signal >> 16);
            sendBuffer[7] = (byte)(signal >> 24);
            for (int i = 0; i < count; i++) {
                sendBuffer[i + 8] = data[i + offset];
            }
            SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
            sendEventArgs.Completed += SendEventArgs_Completed;
            sendEventArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            Socket tempSocket;
            lock (socketLock) {
                tempSocket = socket;
            }
            if (!tempSocket.SendAsync(sendEventArgs)) {
                SendEventArgs_Completed(tempSocket, sendEventArgs);
            }
        }
        private void ConnectEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (sender is Socket s && e.UserToken is SocketAsyncEventArgs receiveEventArgs) {
                switch (e.SocketError) {
                    case SocketError.Success:
                        OnConnectedEvent(s.Connected);
                        if (s.Connected) {
                            if (!s.ReceiveAsync(receiveEventArgs)) {
                                ReceiveEventArgs_Completed(s, receiveEventArgs);
                            }
                        }
                        break;
                    default:
                        OnConnectedEvent(false);
                        break;
                }
            }
        }
        private void ReceiveEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (sender is Socket s) {
                switch (e.SocketError) {
                    case SocketError.Success:
                        int len = e.BytesTransferred;
                        if (len > 0) {
                            if (recvData.Capacity < recvData.Count + len) {
                                recvData.Capacity = recvData.Count + len;
                            }
                            for (int i = 0; i < len; i++) {
                                recvData.Add(e.Buffer[i + e.Offset]);
                            }
                            while (recvData.Count >= 8) {
                                int dataLen = recvData[0] | (recvData[1] << 8) | (recvData[2] << 16) | (recvData[3] << 24);
                                if (recvData.Count - 4 >= dataLen) {
                                    int signal = recvData[4] | (recvData[5] << 8) | (recvData[6] << 16) | (recvData[7] << 24);
                                    OnReceivePackageEvent?.Invoke(signal, recvData.GetRange(8, dataLen - 4).ToArray());
                                    recvData.RemoveRange(0, dataLen + 4);
                                } else {
                                    break;
                                }
                            }
                            if (!s.ReceiveAsync(e)) {
                                ReceiveEventArgs_Completed(s, e);
                            }
                        } else {
                            OnConnectionBreakEvent?.Invoke("len==0");
                        }
                        break;
                    default:
                        OnConnectionBreakEvent?.Invoke(e.SocketError.ToString());
                        break;
                }
            }
        }
        private void SendEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
        }
        ~SocketClient() {
            socket?.Close();
        }
    }
}