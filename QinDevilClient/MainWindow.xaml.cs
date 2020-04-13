using QinDevilCommon;
using QinDevilCommon.SystemLay;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
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
        private SocketClient client;
        private readonly Timer timer = new Timer();
        private bool Connecting = false;
        private GameData gameData = new GameData();
        public MainWindow() {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            client = new SocketClient();
            client.onConnectedEvent += OnConnected;
            client.onReceivePackageEvent += OnReceivePackage;
            client.onConnectionBreakEvent += OnConnectionBreak;
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = false;
            timer.Stop();
            Connect();
        }
        private void Timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (Connecting) {
                Process process = GetWuXiaProcess();
                if (process!=null) {
                    List<byte> sendData = new List<byte>(64);
                    MD5 md5 = MD5.Create();
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(SystemInfo.GetMacAddress() + SystemInfo.GetCpuID()));
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hash) {
                        sb.Append(b.ToString("X2"));
                    }
                    byte[] machineIdentity = Encoding.UTF8.GetBytes(sb.ToString());
                    sendData.AddRange(BitConverter.GetBytes(machineIdentity.Length));
                    sendData.AddRange(machineIdentity);
                    RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString("<RSAKeyValue><Modulus>2FMpblMWJ5JomZbaj8Y+VYkzviSGpEJn3q5EtSYorN6sbsgSKS8UeJ0AEk8lmNcbgF6F8KzdP7z93EhZRUeqOlPQh+VmrMQ0kUpUdngO0mlJUU6jAhuQd4Hw+NTnZZknKjhWSQFD8e5V3nFYSjsZXlXdGtvukJxsG8RcyLB2Kd0=</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
                    byte[] gamePath = rsa.Encrypt(Encoding.UTF8.GetBytes(process.MainModule.FileName), true);
                    sendData.AddRange(BitConverter.GetBytes(gamePath.Length));
                    sendData.AddRange(gamePath);
                    client.SendPackage(0, sendData.ToArray(), 0, sendData.Count);
                } else {
                    timer.Interval = 10000;
                    timer.Start();
                }
            } else {
                Connect();
                gameData.FailTimes++;
            }
        }
        private Process GetWuXiaProcess() {
            Process[] process = Process.GetProcessesByName("WuXia_Client_x64.exe");
            if (process.Length > 0) {
                return process[0];
            } else {
                process = Process.GetProcessesByName("WuXia_Client.exe");
                if (process.Length > 0) {
                    return process[0];
                }
            }
            return null;
        }
        private void Connect() {
            client.Connect("q1143910315.gicp.net", 51814);
        }
        private void OnConnected(bool connected) {
            Connecting = connected;
            timer.Interval = 500;
            timer.Start();
        }
        private void OnReceivePackage(int signal, byte[] buffer) {
            gameData.FailTimes = 0;
            switch (signal) {
                case 0: {
                        int ping = client.GetPing();
                        gameData.Ping = ping > 9999 ? 9999 : (ping < 0 ? 9999 : ping);
                        break;
                    }
                default:
                    break;
            }
            /*msgContent.Dispatcher.Invoke(() => {
                msgContent.Content += string.Format("信号值：{0}\n消息内容：{1}\n", signal, Encoding.ASCII.GetString(buffer));
            });*/
        }
        private void OnConnectionBreak() {
            Connecting = false;
            timer.Interval = 500;
            timer.Start();
        }
        private void Button_Click(object sender, RoutedEventArgs e) {
            byte[] v = Encoding.ASCII.GetBytes(msgEdit.Text);
            client.SendPackage(int.Parse(singalEdit.Text), v, 0, v.Length);
        }
    }
}
