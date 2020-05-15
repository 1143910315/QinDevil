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
        private struct ClientStruct {
            public byte[] recvBuffer;
            public byte[] sendBuffer;
            public object userToken;
            public List<byte> recvData;
            public List<byte> sendData;
            public Socket s;
            public int id;
            public byte state;
            public SocketAsyncEventArgs[] sendEventArgs;
        }
        public delegate object OnAcceptSuccess(int id);
        public delegate void OnLeave(int id, object userToken);
        public delegate void OnReceivePackage(int id, int signal, byte[] buffer, object userToken);
        public event OnAcceptSuccess OnAcceptSuccessEvent;
        public event OnLeave OnLeaveEvent;
        public event OnReceivePackage OnReceivePackageEvent;
        private Socket socket;
        private readonly Hashtable socketHashtable = new Hashtable();
        private int connectNum = 1;
        private bool repeat = false;
        public SocketServer() {
            if (!Socket.OSSupportsIPv4) {
                throw new NotSupportedException("系统不支持IPv4网络！");
            }
        }
        public void Start(int port) {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(10);
            SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += AcceptEventArgs_Completed;
            if (!socket.AcceptAsync(acceptEventArgs)) {
                AcceptEventArgs_Completed(socket, acceptEventArgs);
            }
        }
        private void AcceptEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            SocketAsyncEventArgs acceptEventArgs;
            SocketAsyncEventArgs receiveEventArgs;
            switch (e.SocketError) {
                case SocketError.Success:
                    if (repeat) {
                        lock (socketHashtable) {
                            while (socketHashtable.ContainsKey(connectNum)) {
                                connectNum++;
                            }
                        }
                    } else {
                        if (connectNum < 0) {
                            repeat = true;
                        }
                    }
                    ClientStruct client = new ClientStruct {
                        id = connectNum,
                        s = e.AcceptSocket,
                        sendBuffer = new byte[255],
                        recvBuffer = new byte[255],
                        sendData = new List<byte>(),
                        recvData = new List<byte>(),
                        userToken = OnAcceptSuccessEvent?.Invoke(connectNum),
                        state = 0,
                        sendEventArgs = new SocketAsyncEventArgs[2]
                    };
                    for (int i = 0; i < 2; i++) {
                        SocketAsyncEventArgs sendEventArgs = new SocketAsyncEventArgs();
                        sendEventArgs.Completed += SendEventArgs_Completed;
                        client.sendEventArgs[i] = sendEventArgs;
                    }
                    lock (socketHashtable) {
                        socketHashtable.Add(connectNum++, client);
                    }
                    receiveEventArgs = new SocketAsyncEventArgs();
                    receiveEventArgs.Completed += ReceiveEventArgs_Completed;
                    receiveEventArgs.UserToken = client;
                    receiveEventArgs.SetBuffer(client.recvBuffer, 0, client.recvBuffer.Length);
                    if (!e.AcceptSocket.ReceiveAsync(receiveEventArgs)) {
                        ReceiveEventArgs_Completed(e.AcceptSocket, receiveEventArgs);
                    }
                    acceptEventArgs = new SocketAsyncEventArgs();
                    acceptEventArgs.Completed += AcceptEventArgs_Completed;
                    if (!socket.AcceptAsync(acceptEventArgs)) {
                        AcceptEventArgs_Completed(socket, acceptEventArgs);
                    }
                    break;
                case SocketError.ConnectionReset:
                    acceptEventArgs = new SocketAsyncEventArgs();
                    acceptEventArgs.Completed += AcceptEventArgs_Completed;
                    if (!socket.AcceptAsync(acceptEventArgs)) {
                        AcceptEventArgs_Completed(socket, acceptEventArgs);
                    }
                    break;
            }
        }
        internal void CloseClient(int id) {
            lock (socketHashtable) {
                if (socketHashtable.ContainsKey(id)) {
                    ClientStruct client = (ClientStruct)socketHashtable[id];
                    socketHashtable.Remove(id);
                    client.s.Close();
                    OnLeaveEvent?.Invoke(client.id, client.userToken);
                }
            }
        }
        private void ReceiveEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (e.UserToken is ClientStruct client) {
                try {
                    int len = e.BytesTransferred;
                    if (e.SocketError == SocketError.Success && len > 0) {
                        if (client.recvData.Capacity < client.recvData.Count + len) {
                            client.recvData.Capacity = client.recvData.Count + len;
                        }
                        for (int i = 0; i < len; i++) {
                            client.recvData.Add(client.recvBuffer[i + e.Offset]);
                        }
                        while (client.recvData.Count >= 8) {
                            int dataLen = client.recvData[0] | (client.recvData[1] << 8) | (client.recvData[2] << 16) | (client.recvData[3] << 24);
                            if (client.recvData.Count - 4 >= dataLen) {
                                int signal = client.recvData[4] | (client.recvData[5] << 8) | (client.recvData[6] << 16) | (client.recvData[7] << 24);
                                OnReceivePackageEvent?.Invoke(client.id, signal, client.recvData.GetRange(8, dataLen - 4).ToArray(), client.userToken);
                                client.recvData.RemoveRange(0, dataLen + 4);
                            } else {
                                break;
                            }
                        }
                        SocketAsyncEventArgs receiveEventArgs = new SocketAsyncEventArgs();
                        receiveEventArgs.Completed += ReceiveEventArgs_Completed;
                        receiveEventArgs.UserToken = client;
                        receiveEventArgs.SetBuffer(client.recvBuffer, 0, client.recvBuffer.Length);
                        if (!client.s.ReceiveAsync(receiveEventArgs)) {
                            ReceiveEventArgs_Completed(sender, receiveEventArgs);
                        }
                    } else {
                        throw new Exception("套接字发生错误！");
                    }
                } catch (Exception) {
                    lock (socketHashtable) {
                        if (socketHashtable.ContainsKey(client.id)) {
                            socketHashtable.Remove(client.id);
                            client.s.Close();
                            OnLeaveEvent?.Invoke(client.id, client.userToken);
                        }
                    }
                }
            }
        }
        private void SendEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (e.UserToken is ClientStruct client) {
                lock (client.sendData) {
                    try {
                        if (e.SocketError == SocketError.Success) {
                            int len = client.sendData.Count;
                            if (len > 0) {
                                if ((client.state & 0b1) == 0) {
                                    client.state |= 0b1;
                                } else {
                                    client.state &= 0b10;
                                }
                                int count = len > client.sendBuffer.Length ? client.sendBuffer.Length : len;
                                client.sendData.CopyTo(0, client.sendBuffer, 0, count);
                                client.sendData.RemoveRange(0, count);
                                client.sendEventArgs[client.state & 1].SetBuffer(client.sendBuffer, 0, count);
                                if (!client.s.SendAsync(client.sendEventArgs[client.state & 1])) {
                                    SendEventArgs_Completed(client.s, client.sendEventArgs[client.state & 1]);
                                }
                            } else {
                                client.state &= 1;
                            }
                        } else {
                            throw new Exception("套接字发生错误！");
                        }
                    } catch (Exception) {
                        lock (socketHashtable) {
                            if (socketHashtable.ContainsKey(client.id)) {
                                socketHashtable.Remove(client.id);
                                client.s.Close();
                                OnLeaveEvent?.Invoke(client.id, client.userToken);
                            }
                        }
                    }
                }
            }
        }
        public void SendPackage(int id, int signal, byte[] data) {
            if (data != null) {
                SendPackage(id, signal, data, 0, data.Length);
            } else {
                SendPackage(id, signal, null, 0, 0);
            }
        }
        public void SendPackage(int id, int signal, byte[] data, int offset, int count) {
            object o;
            lock (socketHashtable) {
                o = socketHashtable[id];
            }
            if (o is ClientStruct client) {
                lock (client.sendData) {
                    try {
                        int len = count + 4;
                        if ((client.state & 0b10) == 0) {
                            client.state |= 0b10;
                            client.sendBuffer[0] = (byte)len;
                            client.sendBuffer[1] = (byte)(len >> 8);
                            client.sendBuffer[2] = (byte)(len >> 16);
                            client.sendBuffer[3] = (byte)(len >> 24);
                            client.sendBuffer[4] = (byte)signal;
                            client.sendBuffer[5] = (byte)(signal >> 8);
                            client.sendBuffer[6] = (byte)(signal >> 16);
                            client.sendBuffer[7] = (byte)(signal >> 24);
                            if (client.sendData.Capacity < count - client.sendBuffer.Length + 8) {
                                client.sendData.Capacity = count - client.sendBuffer.Length + 8;
                            }
                            for (int i = 0; i < count; i++) {
                                if (i + 8 < client.sendBuffer.Length) {
                                    client.sendBuffer[i + 8] = data[i + offset];
                                } else {
                                    client.sendData.Add(data[i + offset]);
                                }
                            }
                            client.sendEventArgs[client.state & 1].SetBuffer(client.sendBuffer, 0, count + 8);
                            client.sendEventArgs[client.state & 1].UserToken = client;
                            if (!client.s.SendAsync(client.sendEventArgs[client.state & 1])) {
                                SendEventArgs_Completed(client.s, client.sendEventArgs[client.state & 1]);
                            }
                        } else {
                            if (client.sendData.Capacity < count - client.sendBuffer.Length + 8 + client.sendData.Count) {
                                client.sendData.Capacity = count - client.sendBuffer.Length + 8 + client.sendData.Count;
                            }
                            client.sendData.Add((byte)len);
                            client.sendData.Add((byte)(len >> 8));
                            client.sendData.Add((byte)(len >> 16));
                            client.sendData.Add((byte)(len >> 24));
                            client.sendData.Add((byte)signal);
                            client.sendData.Add((byte)(signal >> 8));
                            client.sendData.Add((byte)(signal >> 16));
                            client.sendData.Add((byte)(signal >> 24));
                            for (int i = 0; i < count; i++) {
                                client.sendData.Add(data[i + offset]);
                            }
                        }
                    } catch (Exception) {
                        lock (socketHashtable) {
                            if (socketHashtable.ContainsKey(client.id)) {
                                socketHashtable.Remove(client.id);
                                client.s.Close();
                                OnLeaveEvent?.Invoke(client.id, client.userToken);
                            }
                        }
                    }
                }
            }
        }
        ~SocketServer() {
            socket.Close();
        }
    }
}