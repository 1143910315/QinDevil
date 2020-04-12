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
            server = new SocketServer(13748) {
                onAcceptSuccessEvent = OnAcceptSuccess,
                onReceiveOriginalDataEvent = OnReceiveOriginalData,
                onReceivePackageEvent = OnReceivePackage,
                onLeaveEvent = OnLeave
            };
        }
        private void OnAcceptSuccess(int id) {
            connectNum++;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });
        }
        private bool OnReceiveOriginalData(int id, byte[] buffer, int offest, int count) {
            return false;
        }
        private void OnReceivePackage(int id, int signal, byte[] buffer) {

            /*shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = string.Format("信号值：{0}\n消息内容：{1}", signal, Encoding.ASCII.GetString(buffer));
            });*/
        }
        private void OnLeave(int id) {
            connectNum--;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            byte[] v = Encoding.ASCII.GetBytes(magEdit.Text);
            server.SendPackage(0, int.Parse(signalEdit.Text), v, 0, v.Length);
        }
    }
}
