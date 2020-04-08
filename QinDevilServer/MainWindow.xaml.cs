using QinDevilCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QinDevilServer {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        SocketServer server;
        int connectNum = 0;
        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            server = new SocketServer(13708, OnAccept, OnReceive, OnLeave);
        }
        private bool OnAccept(Socket socket) {
            connectNum++;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });
            return true;
        }
        private void OnReceive(int id, byte[] buffer) {
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = Encoding.ASCII.GetString(buffer);
            });
        }
        private void OnLeave(int id) {
            connectNum--;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });
        }
    }
}
