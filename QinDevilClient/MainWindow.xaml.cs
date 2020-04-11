using QinDevilCommon;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace QinDevilClient {
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window {
        SocketClient client;
        public MainWindow() {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            client = new SocketClient();
            client.onReceivePackageEvent += OnReceivePackage;
            client.Connect("127.0.0.1", 13748);
        }
        private void OnReceivePackage(int signal, byte[] buffer) {
            msgContent.Dispatcher.Invoke(() => {
                msgContent.Content += string.Format("信号值：{0}\n消息内容：{1}\n", signal, Encoding.ASCII.GetString(buffer));
            });
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            byte[] v = Encoding.ASCII.GetBytes(msgEdit.Text);
            client.SendPackage(int.Parse(singalEdit.Text), v, 0, v.Length);
        }
    }
}
