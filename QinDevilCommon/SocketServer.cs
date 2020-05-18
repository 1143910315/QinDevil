using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace QinDevilCommon {
    public class SocketServer {
        public class ClientInfo {
            public Socket s;
            public List<byte> recvData;
            public object userToken;
        }
        public delegate void OnAcceptSuccess(ClientInfo client);
        public delegate void OnLeave(ClientInfo client);
        public delegate void OnReceivePackage(ClientInfo client, int signal, byte[] buffer);
        public event OnAcceptSuccess OnAcceptSuccessEvent;
        public event OnLeave OnLeaveEvent;
        public event OnReceivePackage OnReceivePackageEvent;
        private Socket socket;
        private readonly SocketAsyncEventArgs acceptEventArgs = new SocketAsyncEventArgs();
        private readonly Stack<SocketAsyncEventArgs> socketAsyncEventArgs = new Stack<SocketAsyncEventArgs>();
        public SocketServer() {
            if (!Socket.OSSupportsIPv4) {
                throw new NotSupportedException("系统不支持IPv4网络！");
            }
        }
        public void Start(int port) {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(10);
            acceptEventArgs.Completed += AcceptEventArgs_Completed;
            if (!socket.AcceptAsync(acceptEventArgs)) {
                AcceptEventArgs_Completed(socket, acceptEventArgs);
            }
        }
        private void AcceptEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            switch (e.SocketError) {
                case SocketError.Success:
                    ClientInfo client = new ClientInfo {
                        s = e.AcceptSocket,
                        recvData = new List<byte>(),
                    };
                    OnAcceptSuccessEvent?.Invoke(client);
                    SocketAsyncEventArgs receiveEventArgs = PopSocketAsyncEventArgs(client);
                    if (!e.AcceptSocket.ReceiveAsync(receiveEventArgs)) {
                        ReceiveEventArgs_Completed(e.AcceptSocket, receiveEventArgs);
                    }
                    acceptEventArgs.AcceptSocket = null;
                    if (!socket.AcceptAsync(acceptEventArgs)) {
                        AcceptEventArgs_Completed(socket, acceptEventArgs);
                    }
                    break;
                case SocketError.ConnectionReset:
                    acceptEventArgs.Completed += AcceptEventArgs_Completed;
                    if (!socket.AcceptAsync(acceptEventArgs)) {
                        AcceptEventArgs_Completed(socket, acceptEventArgs);
                    }
                    break;
            }
        }
        private void SocketEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            switch (e.LastOperation) {
                case SocketAsyncOperation.Receive:
                    ReceiveEventArgs_Completed(sender, e);
                    break;
                case SocketAsyncOperation.Send:
                    SendEventArgs_Completed(sender, e);
                    break;
            }
        }
        private void ReceiveEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            if (sender is Socket s && e.UserToken is ClientInfo client) {
                switch (e.SocketError) {
                    case SocketError.Success:
                        int len = e.BytesTransferred;
                        if (len > 0) {
                            if (client.recvData.Capacity < client.recvData.Count + len) {
                                client.recvData.Capacity = client.recvData.Count + len;
                            }
                            for (int i = 0; i < len; i++) {
                                client.recvData.Add(e.Buffer[i + e.Offset]);
                            }
                            while (client.recvData.Count >= 8) {
                                int dataLen = client.recvData[0] | (client.recvData[1] << 8) | (client.recvData[2] << 16) | (client.recvData[3] << 24);
                                if (client.recvData.Count - 4 >= dataLen) {
                                    int signal = client.recvData[4] | (client.recvData[5] << 8) | (client.recvData[6] << 16) | (client.recvData[7] << 24);
                                    byte[] tempBuffer = client.recvData.GetRange(8, dataLen - 4).ToArray();
                                    OnReceivePackageEvent?.Invoke(client, signal, tempBuffer);
                                    client.recvData.RemoveRange(0, dataLen + 4);
                                } else {
                                    break;
                                }
                            }
                            if (!s.ReceiveAsync(e)) {
                                ReceiveEventArgs_Completed(s, e);
                            }
                        }
                        break;
                    default:
                        s.Close();
                        lock (socketAsyncEventArgs) {
                            socketAsyncEventArgs.Push(e);
                        }
                        OnLeaveEvent?.Invoke(client);
                        break;
                }
            }
        }
        private void SendEventArgs_Completed(object sender, SocketAsyncEventArgs e) {
            e.SetBuffer(0, e.Buffer.Length);
            lock (socketAsyncEventArgs) {
                socketAsyncEventArgs.Push(e);
            }
        }
        private SocketAsyncEventArgs PopSocketAsyncEventArgs(ClientInfo client) {
            SocketAsyncEventArgs socketEventArgs = null;
            lock (socketAsyncEventArgs) {
                if (socketAsyncEventArgs.Count > 0) {
                    socketEventArgs = socketAsyncEventArgs.Pop();
                }
            }
            if (socketEventArgs == null) {
                socketEventArgs = new SocketAsyncEventArgs();
                socketEventArgs.Completed += SocketEventArgs_Completed;
                byte[] tempBuffer = new byte[255];
                socketEventArgs.SetBuffer(tempBuffer, 0, tempBuffer.Length);
            }
            socketEventArgs.UserToken = client;
            return socketEventArgs;
        }
        public void SendPackage(ClientInfo client, int signal, byte[] data) {
            if (data != null) {
                SendPackage(client, signal, data, 0, data.Length);
            } else {
                SendPackage(client, signal, null, 0, 0);
            }
        }
        public void SendPackage(ClientInfo client, int signal, byte[] data, int offset, int count) {
            lock (client.s) {
                SocketAsyncEventArgs sendEventArgs = PopSocketAsyncEventArgs(client);
                int dataLen = count + 4;
                sendEventArgs.Buffer[0] = (byte)dataLen;
                sendEventArgs.Buffer[1] = (byte)(dataLen >> 8);
                sendEventArgs.Buffer[2] = (byte)(dataLen >> 16);
                sendEventArgs.Buffer[3] = (byte)(dataLen >> 24);
                sendEventArgs.Buffer[4] = (byte)signal;
                sendEventArgs.Buffer[5] = (byte)(signal >> 8);
                sendEventArgs.Buffer[6] = (byte)(signal >> 16);
                sendEventArgs.Buffer[7] = (byte)(signal >> 24);
                int bufferOffset = 8;
                for (int i = 0; i < count; i++) {
                    if (bufferOffset == sendEventArgs.Buffer.Length) {
                        if (!client.s.SendAsync(sendEventArgs)) {
                            Debug.WriteLineIf(sendEventArgs.SocketError != SocketError.Success, string.Format("Server:SendPackage时，同步Send，SocketError={0},", sendEventArgs.SocketError));
                            SendEventArgs_Completed(client.s, sendEventArgs);
                        }
                        sendEventArgs = PopSocketAsyncEventArgs(client);
                        bufferOffset = 0;
                    }
                    sendEventArgs.Buffer[bufferOffset++] = data[i + offset];
                }
                sendEventArgs.SetBuffer(0, bufferOffset);
                if (!client.s.SendAsync(sendEventArgs)) {
                    SendEventArgs_Completed(client.s, sendEventArgs);
                }
            }
        }
        public void CloseClient(ClientInfo client) {
            client.s.Close();
        }
        ~SocketServer() {
            socket.Close();
        }
    }
}