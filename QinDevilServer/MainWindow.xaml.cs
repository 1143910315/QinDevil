using QinDevilCommon;
using QinDevilCommon.Keyboard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
        private SocketServer server;
        private readonly GameData gameData = new GameData();
        private KeyboardHook hook = new KeyboardHook();
        private bool ctrlState;
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
            GamePanel.DataContext = gameData;
            hook.KeyDownEvent += KeyDownCallbak;
            hook.KeyUpEvent += KeyUpCallbak;
            /*Regex regex = new Regex("^\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}:\\d+$");
            bool t = regex.IsMatch("192.168.1.1:12345");*/
        }
        private void KeyDownCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL:
                    ctrlState = true;
                    break;
                case KeyCode.Numeric7:
                    if (ctrlState) {
                        gameData.State = GameData.State_LeakHunting;
                    }
                    break;
                case KeyCode.Numeric8:
                    if (ctrlState) {
                        gameData.State = GameData.State_HitKey;
                    }
                    break;
                default:
                    break;
            }
        }
        private void KeyUpCallbak(KeyCode keyCode) {
            switch (keyCode) {
                case KeyCode.VK_LCONTROL:
                    ctrlState = false;
                    break;
                default:
                    break;
            }
        }
        private object OnAcceptSuccess(int id) {
            /*connectNum++;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });*/
            UserInfo userInfo = new UserInfo() {
                Id = id,
                LastReceiveTime = DateTime.Now
            };
            gameData.ClientInfo.InsertAfter(-1, userInfo);
            return userInfo;
        }
        private bool OnReceiveOriginalData(int id, byte[] buffer, int offest, int count, object userToken) {
            if (userToken is UserInfo userInfo) {
                if (userInfo.IpAndPort == null) {
                    try {
                        UTF8Encoding utf8 = new UTF8Encoding(false, true);
                        string s = utf8.GetString(buffer, offest, count);
                        Regex regex = new Regex("^\\d{1,3}.\\d{1,3}.\\d{1,3}.\\d{1,3}:\\d+$");
                        if (regex.IsMatch(s)) {
                            userInfo.IpAndPort = s;
                        }
                    } catch (Exception) {
                    }
                }
            }
            return false;
        }
        private void OnReceivePackage(int id, int signal, byte[] buffer, object userToken) {

            /*shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = string.Format("信号值：{0}\n消息内容：{1}", signal, Encoding.ASCII.GetString(buffer));
            });*/
        }
        private void OnLeave(int id, object userToken) {
            /*connectNum--;
            shouConnectNumber.Dispatcher.Invoke(() => {
                shouConnectNumber.Content = connectNum.ToString();
            });*/
            for (int i = 0; i < gameData.ClientInfo.GetSize(); i++) {
                if (gameData.ClientInfo.Get(i).Id == id) {
                    gameData.ClientInfo.Del(i);
                }
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            /*
            byte[] v = Encoding.ASCII.GetBytes(magEdit.Text);
            server.SendPackage(0, int.Parse(signalEdit.Text), v, 0, v.Length);*/
            gameData.ClientInfo.InsertAfter(-1, new UserInfo() {
                Id = int.Parse(signalEdit.Text),
                LastReceiveTime = DateTime.Now
            });
        }
    }
}
