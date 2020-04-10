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
            client.Connect("170c8e7a.nat123.cc", 38836);
        }

    }
}
