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
        public delegate bool OnAcceptFilterEvent(Socket socket);
        public delegate void OnAcceptSuccessEvent(int id);
        public delegate bool OnReceiveOriginalDataEvent(int id, byte[] buffer, int offest, int count);
        public delegate void OnReceivePackageEvent(int id, int signal, byte[] buffer);
        public delegate void OnLeaveEvent(int id);
        private Socket socket;
        private readonly SocketList socketList = new SocketList();
        public OnAcceptFilterEvent onAcceptFilterEvent = null;
        public OnAcceptSuccessEvent onAcceptSuccessEvent = null;
        public OnReceiveOriginalDataEvent onReceiveOriginalDataEvent = null;
        public OnReceivePackageEvent onReceivePackageEvent = null;
        public OnLeaveEvent onLeaveEvent = null;
        public SocketServer(int port) {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(10);
            new Task(() => {
                while (socket != null) {
                    Socket acceptSocket = socket.Accept();
                    bool? b = onAcceptFilterEvent?.Invoke(acceptSocket);
                    if (b.HasValue && b.Value == false) {
                        acceptSocket.Close();
                    } else {
                        new Task((id) => {
                            onAcceptSuccessEvent?.Invoke((int)id);
                            Socket thisSocket = socketList.Get(id);
                            byte[] buffer = new byte[512];
                            while (true) {
                                try {
                                    int len = thisSocket.Receive(buffer);
                                    if (thisSocket.Connected && len > 0) {
                                        bool? deal = onReceiveOriginalDataEvent?.Invoke((int)id, buffer, 0, len);
                                        if (!(deal.HasValue && deal.Value) && socketList.AddData(id, buffer, 0, len)) {
                                            (int signal, byte[] temp) = socketList.GetDataAndDelete(id);
                                            do {
                                                onReceivePackageEvent?.Invoke((int)id, signal, temp);
                                                (signal, temp) = socketList.GetDataAndDelete(id);
                                            } while (temp != null);
                                        }
                                    } else {
                                        socketList.Delete(id);
                                        onLeaveEvent((int)id);
                                        thisSocket.Close();
                                        break;
                                    }
                                } catch (Exception) {
                                    socketList.Delete(id);
                                    onLeaveEvent((int)id);
                                    thisSocket.Close();
                                    break;
                                }
                            }
                        }, socketList.Add(acceptSocket)).Start();
                    }
                }
            }).Start();
        }
        public void SendPackage(int id, int signal, byte[] data, int offset, int count) {
            Socket s = socketList.Get(id);
            if (s != null) {
                byte[] v = BitConverter.GetBytes(signal);
                byte[] l = BitConverter.GetBytes(count + v.Length);
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
                s.BeginSend(package, 0, index, SocketFlags.None, new AsyncCallback(SendCallback), id);
            }
        }
        private void SendCallback(IAsyncResult ar) {
            Socket s = socketList.Get(ar.AsyncState);
            try {
                s.EndSend(ar);
            } catch (Exception) {
                s.Close();
                onLeaveEvent?.Invoke((int)ar.AsyncState);
            }
        }
        ~SocketServer() {
            socket.Close();
            socket = null;
        }
    }
}
