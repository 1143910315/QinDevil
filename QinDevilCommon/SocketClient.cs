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
        public delegate void OnConnectionBreak();
        public delegate void OnSocketException(SocketException socketException);
        public event OnConnected OnConnectedEvent;
        public event OnReceivePackage OnReceivePackageEvent;
        public event OnConnectionBreak OnConnectionBreakEvent;
        public event OnSocketException OnSocketExceptionEvent;
        private Socket socket;
        private readonly object socketLock = new object();
        private readonly SocketAsyncEventArgs receiveEventArgs;
        private readonly SocketAsyncEventArgs[] sendEventArgs = new SocketAsyncEventArgs[] { new SocketAsyncEventArgs(), new SocketAsyncEventArgs() };
        private readonly byte[] recvBuffer = new byte[255];
        private readonly byte[] sendBuffer = new byte[255];
        private readonly List<byte> recvData = new List<byte>();
        private readonly List<byte> sendData = new List<byte>();
        private byte state = 0;
        public SocketClient() {
            if (!Socket.OSSupportsIPv4) {
                throw new NotSupportedException("系统不支持IPv4网络！");
            }
            receiveEventArgs = new SocketAsyncEventArgs();
            receiveEventArgs.SetBuffer(recvBuffer, 0, recvBuffer.Length);
            receiveEventArgs.Completed += ReceiveEventArgs_Completed;
            sendEventArgs[0].Completed += SendEventArgs_Completed;
            sendEventArgs[1].Completed += SendEventArgs_Completed;
        }
        public void Connect(string host, int port) {
            try {
                IPHostEntry entry = Dns.GetHostEntry(host);
                if (entry != null && entry.AddressList != null) {
                    for (int AddressListIndex = 0; AddressListIndex < entry.AddressList.Length; AddressListIndex++) {
                        if (entry.AddressList[AddressListIndex].AddressFamily == AddressFamily.InterNetwork) {
                            lock (socketLock) {
                                state &= 1;
                                socket?.Close();
                                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
                                connectEventArgs.Completed += ConnectEventArgs_Completed;
                                connectEventArgs.RemoteEndPoint = new IPEndPoint(entry.AddressList[AddressListIndex], port);
                                if (!socket.ConnectAsync(connectEventArgs)) {
                                    ConnectEventArgs_Completed(socket, connectEventArgs);
                                }
                            }
                            break;
                        }
                    }
                }
            } catch (SocketException se) {
                OnConnectedEvent?.Invoke(false);
                OnSocketExceptionEvent?.Invoke(se);
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
            lock (sendData) {
                try {
                    int len = count + 4;
                    if ((state & 0b10) == 0) {
                        state |= 0b10;
                        sendBuffer[0] = (byte)len;
                        sendBuffer[1] = (byte)(len >> 8);
                        sendBuffer[2] = (byte)(len >> 16);
                        sendBuffer[3] = (byte)(len >> 24);
                        sendBuffer[4] = (byte)signal;
                        sendBuffer[5] = (byte)(signal >> 8);
                        sendBuffer[6] = (byte)(signal >> 16);
                        sendBuffer[7] = (byte)(signal >> 24);
                        if (sendData.Capacity < count - sendBuffer.Length + 8) {
                            sendData.Capacity = count - sendBuffer.Length + 8;
                        }
                        len = 8;
                        for (int i = 0; i < count; i++) {
                            if (i + 8 < sendBuffer.Length) {
                                sendBuffer[i + 8] = data[i + offset];
                                len++;
                            } else {
                                sendData.Add(data[i + offset]);
                            }
                        }
                        sendEventArgs[state & 1].SetBuffer(sendBuffer, 0, len);
                        lock (socketLock) {
                            if (socket != null) {
                                if (!socket.SendAsync(sendEventArgs[state & 1])) {
                                    SendEventArgs_Completed(socket, sendEventArgs[state & 1]);
                                }
                            } else {
                                state &= 1;
                                sendData.Clear();
                            }
                        }
                    } else {
                        if (sendData.Capacity < count - sendBuffer.Length + 8 + sendData.Count) {
                            sendData.Capacity = count - sendBuffer.Length + 8 + sendData.Count;
                        }
                        sendData.Add((byte)len);
                        sendData.Add((byte)(len >> 8));
                        sendData.Add((byte)(len >> 16));
                        sendData.Add((byte)(len >> 24));
                        sendData.Add((byte)signal);
                        sendData.Add((byte)(signal >> 8));
                        sendData.Add((byte)(signal >> 16));
                        sendData.Add((byte)(signal >> 24));
                        for (int i = 0; i < count; i++) {
                            sendData.Add(data[i + offset]);
                        }
                    }
                } catch (SocketException se) {
                    lock (socketLock) {
                        socket?.Close();
                        socket = null;
                    }
                    sendData.Clear();
                    OnConnectionBreakEvent?.Invoke();
                    OnSocketExceptionEvent?.Invoke(se);
                }
            }
        }
        private void ConnectEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (sender is Socket s) {
                try {
                    bool c = false;
                    lock (socketLock) {
                        if (s.Equals(socket)) {
                            c = true;
                            if (s.Connected) {
                                if (!socket.ReceiveAsync(receiveEventArgs)) {
                                    ReceiveEventArgs_Completed(socket, receiveEventArgs);
                                }
                            } else {
                                socket = null;
                            }
                        }
                    }
                    if (c) {
                        OnConnectedEvent?.Invoke(s.Connected);
                    }
                } catch (SocketException se) {
                    lock (socketLock) {
                        if (s.Equals(socket)) {
                            socket?.Close();
                            socket = null;
                        }
                    }
                    OnConnectionBreakEvent?.Invoke();
                    OnSocketExceptionEvent?.Invoke(se);
                }
            }
        }
        private void ReceiveEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (sender is Socket s) {
                try {
                    int len = e.BytesTransferred;
                    if (len > 0 && e.SocketError == SocketError.Success) {
                        if (recvData.Capacity < recvData.Count + len) {
                            recvData.Capacity = recvData.Count + len;
                        }
                        for (int i = 0; i < len; i++) {
                            recvData.Add(recvBuffer[i + e.Offset]);
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
                        lock (socketLock) {
                            if (s.Equals(socket)) {
                                if (!s.ReceiveAsync(receiveEventArgs)) {
                                    ReceiveEventArgs_Completed(socket, receiveEventArgs);
                                }
                            }
                        }
                    } else {
                        lock (socketLock) {
                            if (s.Equals(socket)) {
                                socket?.Close();
                                socket = null;
                            }
                        }
                        OnConnectionBreakEvent?.Invoke();
                    }
                } catch (SocketException se) {
                    lock (socketLock) {
                        if (s.Equals(socket)) {
                            socket?.Close();
                            socket = null;
                        }
                    }
                    OnConnectionBreakEvent?.Invoke();
                    OnSocketExceptionEvent?.Invoke(se);
                }
            }
        }
        private void SendEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            lock (sendData) {
                if (sender is Socket s) {
                    try {
                        if (e.SocketError == SocketError.Success) {
                            int len = sendData.Count;
                            if (len > 0) {
                                if ((state & 0b1) == 0) {
                                    state |= 0b1;
                                } else {
                                    state &= 0b10;
                                }
                                int count = len > sendBuffer.Length ? sendBuffer.Length : len;
                                sendData.CopyTo(0, sendBuffer, 0, count);
                                sendData.RemoveRange(0, count);
                                sendEventArgs[state & 1].SetBuffer(sendBuffer, 0, count);
                                lock (socketLock) {
                                    if (s.Equals(socket)) {
                                        if (!socket.SendAsync(sendEventArgs[state & 1])) {
                                            SendEventArgs_Completed(socket, sendEventArgs[state & 1]);
                                        }
                                    }
                                }
                            } else {
                                state &= 1;
                            }
                        } else {
                            lock (socketLock) {
                                if (s.Equals(socket)) {
                                    socket?.Close();
                                    socket = null;
                                }
                            }
                            state &= 1;
                            sendData.Clear();
                            OnConnectionBreakEvent?.Invoke();
                        }
                    } catch (SocketException se) {
                        lock (socketLock) {
                            if (s.Equals(socket)) {
                                socket?.Close();
                                socket = null;
                            }
                        }
                        state &= 1;
                        sendData.Clear();
                        OnConnectionBreakEvent?.Invoke();
                        OnSocketExceptionEvent?.Invoke(se);
                    }
                }
            }
        }
        ~SocketClient() {
            socket?.Close();
        }
    }
}