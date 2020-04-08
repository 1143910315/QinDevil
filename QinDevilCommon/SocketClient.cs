using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace QinDevilCommon {
    public class SocketClient {
        private Socket socket;
        public SocketClient() {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public void Connect(string host,int port) {
            IPHostEntry entry = Dns.GetHostEntry(host);
            if (entry != null && entry.AddressList != null && entry.AddressList.Length > 0) {
                SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs {
                    RemoteEndPoint = new IPEndPoint(entry.AddressList[0], port)
                };
                socketAsyncEventArgs.Completed += (t, e) => {
                    Debug.WriteLine(t.GetType().FullName);
                };
                socket.ConnectAsync(socketAsyncEventArgs);
            }

            
        }
    }
}
