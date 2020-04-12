using QinDevilCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QinDevilClient {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        SocketClient client;
        Timer timer = new Timer();
        public MainWindow() {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            client = new SocketClient();
            client.onConnectedEvent += OnConnected;
            client.onReceivePackageEvent += OnReceivePackage;
            client.onConnectionBreakEvent += OnConnectionBreak;
            timer.Elapsed += Timer_Elapsed;
            timer.Stop();
            Connect();
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            Connect();
        }
        private void Connect() {
            client.Connect("127.0.0.1", 13748);
        }
        private void OnConnected(bool connected) {
            if (connected) {
                timer.Interval = 2000;
                timer.Start();
            }
        }
        private void OnReceivePackage(int signal, byte[] buffer) {
            /*msgContent.Dispatcher.Invoke(() => {
                msgContent.Content += string.Format("信号值：{0}\n消息内容：{1}\n", signal, Encoding.ASCII.GetString(buffer));
            });*/
        }
        private void OnConnectionBreak() {
            throw new NotImplementedException();
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            byte[] v = Encoding.ASCII.GetBytes(msgEdit.Text);
            client.SendPackage(int.Parse(singalEdit.Text), v, 0, v.Length);
        }
    }
}
